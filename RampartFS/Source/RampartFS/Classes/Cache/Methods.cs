using System.Collections.Concurrent;
using System.Text;
using FuseDotNet;
using Lightning.Diagnostics.Logging;
using Mono.Unix;
using Mono.Unix.Native;

namespace RampartFS;

internal partial class Cache {
    public Cache (
        String RootPath,
        Controller<Int64> ParamController
    ) {
        BaseCache           = new ConcurrentDictionary<String, CacheEntry>();
        BaseRootPath        = RootPath;
        BaseParamController = ParamController;
        BaseCurrentCache    = 0;
        
        String? ListRaw = BaseParamController.GetRaw("CachedEntries");
        if (ListRaw != null) {
            String[] Paths = ListRaw.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (String Path in Paths) {
                if (File.Exists(Path)) {
                    CacheEntry Entry = new CacheEntry(Path);
                    Entry.Load();
                    BaseCurrentCache = BaseCurrentCache + Entry.Length;
                    BaseCache.TryAdd(Path, Entry);
                }
            }
        }
    }

    private void ConditionalTrim () {
        Monitor.Enter(BaseCache);
        Int64 MaximumCache = BaseParamController["MaximumCache", 0];
        Int64 TrimTarget   = BaseParamController["TrimTarget", 0];
        
        Int64 RelativeTrimTarget = Math.Max(1048576, MaximumCache - TrimTarget);
        Int64 CheckedRelativeTrimTarget = Math.Min(RelativeTrimTarget, MaximumCache);
        
        if (BaseCurrentCache >= MaximumCache) {
            Log.PrintAsync($"Cache trim required.", LogLevel.Debug);

            List<KeyValuePair<String, CacheEntry>> SortedEntries = BaseCache.ToList();
            SortedEntries.Sort(Comparison);
            
            Int64 TrimAmount = 0;
            Int32 Index = 0;
            
            List<KeyValuePair<String, CacheEntry>> SelectedEntries = new List<KeyValuePair<String, CacheEntry>>(SortedEntries.Count);
            
            while (TrimAmount < CheckedRelativeTrimTarget || SelectedEntries.Count < SortedEntries.Count) {
                KeyValuePair<String, CacheEntry> SelectedEntry = SortedEntries[Index];
                
                Log.PrintAsync($"Queuing '{SelectedEntry.Key}' for trimming - size: {SelectedEntry.Value.Length} bytes, age: {DateTime.UtcNow.Subtract(SelectedEntry.Value.AccessedOn)}", LogLevel.Debug);
                
                SelectedEntry.Value.Lock();
                SelectedEntries.Add(SelectedEntry);
                TrimAmount = TrimAmount + SelectedEntry.Value.Length;
                
                Log.PrintAsync($"Queued '{SelectedEntry.Key}' for trimming - size: {SelectedEntry.Value.Length} bytes, age: {DateTime.UtcNow.Subtract(SelectedEntry.Value.AccessedOn)}", LogLevel.Debug);
                
                Index = Index + 1;
            }
            
            Log.PrintAsync($"Queue complete. Trimming", LogLevel.Debug);

            Parallel.ForEach(SelectedEntries, Body); void Body (
                KeyValuePair<String, CacheEntry> Sorted
            ) {
                Log.PrintAsync($"Trimming '{Sorted.Key}' - size: {Sorted.Value.Length} bytes", LogLevel.Debug);
                if (Sorted.Value.Modified == true) {
                    Sorted.Value.Save();
                }
                Interlocked.Add(ref BaseCurrentCache, -Sorted.Value.Length);
                Sorted.Value.Dispose();
                BaseCache.TryRemove(Sorted.Key, out _);
                
                Log.PrintAsync($"Trimmed '{Sorted.Key}' ", LogLevel.Debug);
            }

            foreach (KeyValuePair<String, CacheEntry> Sorted in SelectedEntries) {
                Sorted.Value.Unlock();
                Log.PrintAsync($"Dequeued '{Sorted.Key}' ", LogLevel.Debug);
            }
            
            Log.PrintAsync($"Collecting garbage", LogLevel.Debug);

            GC.Collect(0, GCCollectionMode.Forced, true, true);
            GC.Collect( 1, GCCollectionMode.Forced, true, true);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }
        Monitor.Exit(BaseCache);
    }

    private static Int32 Comparison (
        KeyValuePair<String, CacheEntry> A,
        KeyValuePair<String, CacheEntry> B
    ) {
        return A.Value.AccessedOn.CompareTo(B.Value.AccessedOn);
    }

    public PosixResult ReadFile (
        in  String     AbsolutePath,
        in  Span<Byte> Buffer,
        in  Int64      Offset,
        out Int32      BytesRead
    ) {
        if (Syscall.lstat(AbsolutePath, out Stat Stats) == -1) {
            Errno       Error       = Stdlib.GetLastError();
            Int64       ErrorInt64  = (Int64)Error;
            PosixResult ErrorResult = (PosixResult)ErrorInt64;
            
            BytesRead = 0;
            
            Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
            
            return ErrorResult;
        }
        
        Int64 PreviousLength = Stats.st_size;
        
        Int64 MaximumCache = BaseParamController["MaximumCache", 0];
        Int64 TrimTarget   = BaseParamController["TrimTarget", 0];
        Int64 Threshold    = Math.Max(MaximumCache - TrimTarget, 8388608);

        if (PreviousLength > Threshold) {
            Log.PrintAsync($"File '{AbsolutePath}' was to big to cache ({PreviousLength} > {Threshold}). reading directly from disk", LogLevel.Warning);

            FileStream DiskStream = new FileStream(AbsolutePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            DiskStream.Position = Offset;
            BytesRead           = DiskStream.Read(Buffer);
            
            DiskStream.Flush();
            DiskStream.Close();
            DiskStream.DisposeAsync();
            
            return PosixResult.Success;
        }
        else {
            Monitor.Enter(BaseCache);
            if (BaseCache.TryGetValue(AbsolutePath, out CacheEntry? Value) == false) {
                Value = new CacheEntry(AbsolutePath);
                BaseCache.TryAdd(AbsolutePath, Value);
                
                Log.PrintAsync($"File '{AbsolutePath}' cache entry added for future modifications", LogLevel.Debug);
            }
            
            Value.Lock();
        
            Monitor.Exit(BaseCache);

            if (Value.Loaded == false) {
                Value.Load();
                Interlocked.Add(ref BaseCurrentCache, Value.Length);
                
                Log.PrintAsync($"File '{AbsolutePath}' cache entry loaded for future modifications", LogLevel.Debug);
            }

            PosixResult Result = Value.Read(Buffer, Offset, out BytesRead);
        
            Value.Unlock();
        
            ConditionalTrim();

            return Result;
        }
    }
    
    public PosixResult WriteFile (
        in  String             AbsolutePath,
        in  ReadOnlySpan<Byte> Buffer,
        in  Int64              Offset,
        out Int32              BytesWritten,
        out Int64              Difference,
        in  Int64              CurrentStorage,
        in  Int64              MaximumStorage
    ) {
        if (Syscall.lstat(AbsolutePath, out Stat Stats) == -1) {
            Errno       Error       = Stdlib.GetLastError();
            Int64       ErrorInt64  = (Int64)Error;
            PosixResult ErrorResult = (PosixResult)ErrorInt64;
            
            Difference   = 0;
            BytesWritten = 0;
            
            Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
            
            return ErrorResult;
        }
        
        Int64 PreviousLength = Stats.st_size;
        
        Int64 MaximumCache = BaseParamController["MaximumCache", 0];
        Int64 TrimTarget   = BaseParamController["TrimTarget", 0];
        Int64 Threshold    = Math.Max(MaximumCache - TrimTarget, 8388608);

        if (PreviousLength > Threshold) {
            Log.PrintAsync($"File '{AbsolutePath}' was to big to cache ({PreviousLength} > {Threshold}). writing directly to disk", LogLevel.Warning);
            
            CheckWrite(Buffer, Offset, PreviousLength, out Difference);
            if (CurrentStorage + Difference > MaximumStorage) {
                BytesWritten = 0;
                
                Log.PrintAsync($"File '{AbsolutePath}' would not fit within mount capacity after write. refusing with ENOSPC", LogLevel.Warning);

                return PosixResult.ENOSPC;
            }
            
            FileStream DiskStream = new FileStream(AbsolutePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            DiskStream.Position = Offset;
            DiskStream.Write(Buffer);
            BytesWritten = Buffer.Length;
            
            DiskStream.Flush();
            DiskStream.Close();
            DiskStream.DisposeAsync();
            
            return PosixResult.Success;
        }
        else {
            Monitor.Enter(BaseCache);
            if (BaseCache.TryGetValue(AbsolutePath, out CacheEntry? Value) == false) {
                Value = new CacheEntry(AbsolutePath);
                BaseCache.TryAdd(AbsolutePath, Value);
                
                Log.PrintAsync($"File '{AbsolutePath}' cache entry added for future modifications", LogLevel.Debug);
            }

            Value.Lock();
        
            Monitor.Exit(BaseCache);

            if (Value.Loaded == false) {
                Value.Load();
                Interlocked.Add(ref BaseCurrentCache, Value.Length);
                
                Log.PrintAsync($"File '{AbsolutePath}' cache entry loaded for future modifications", LogLevel.Debug);
            }

            Value.CheckWrite(Buffer, Offset, out Difference);
            if (CurrentStorage + Difference > MaximumStorage) {
                BytesWritten = 0;
                Value.Unlock();
                ConditionalTrim();
                
                Log.PrintAsync($"File '{AbsolutePath}' would not fit within mount capacity after write. refusing with ENOSPC", LogLevel.Warning);
                
                return PosixResult.ENOSPC;
            }
        
            PosixResult Result = Value.Write(Buffer, Offset, out BytesWritten);
            Interlocked.Add(ref BaseCurrentCache, Difference);
            Value.Unlock();
            ConditionalTrim();

            return Result;
        }
    }
    
    public PosixResult TruncateFile (
        in  String AbsolutePath,
        in  Int64  Length,
        out Int64  Difference,
        in  Int64  CurrentStorage,
        in  Int64  MaximumStorage
    ) {
        if (Syscall.lstat(AbsolutePath, out Stat Stats) == -1) {
            Errno       Error       = Stdlib.GetLastError();
            Int64       ErrorInt64  = (Int64)Error;
            PosixResult ErrorResult = (PosixResult)ErrorInt64;

            Difference = 0;
            
            Log.PrintAsync($"Syscall returned failure '{Error}' for path '{AbsolutePath}'", LogLevel.Warning);
            
            return ErrorResult;
        }
        Int64 PreviousLength = Stats.st_size;

        Int64 MaximumCache = BaseParamController["MaximumCache", 0];
        Int64 TrimTarget   = BaseParamController["TrimTarget", 0];
        Int64 Threshold    = Math.Max(MaximumCache - TrimTarget, 8388608);

        if (PreviousLength > Threshold) {
            Log.PrintAsync($"File '{AbsolutePath}' was to big to cache ({PreviousLength} > {Threshold}). truncating directly on disk", LogLevel.Warning);

            CheckTruncate(Length, PreviousLength, out Difference);
            if (CurrentStorage + Difference > MaximumStorage) {
                Log.PrintAsync($"File '{AbsolutePath}' would not fit within mount capacity after truncate. refusing with ENOSPC", LogLevel.Warning);

                return PosixResult.ENOSPC;
            }
            
            FileStream DiskStream = new FileStream(AbsolutePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            DiskStream.SetLength(Length);
            
            DiskStream.Flush();
            DiskStream.Close();
            DiskStream.DisposeAsync();
            
            return PosixResult.Success;
        }
        else {
            Monitor.Enter(BaseCache);
            if (BaseCache.TryGetValue(AbsolutePath, out CacheEntry? Value) == false) {
                Value = new CacheEntry(AbsolutePath);
                BaseCache.TryAdd(AbsolutePath, Value);
                
                Log.PrintAsync($"File '{AbsolutePath}' cache entry added for future modifications", LogLevel.Debug);
            }

            Value.Lock();
        
            Monitor.Exit(BaseCache);

            if (Value.Loaded == false) {
                Value.Load();
                Interlocked.Add(ref BaseCurrentCache, Value.Length);
                
                Log.PrintAsync($"File '{AbsolutePath}' cache entry loaded for future modifications", LogLevel.Debug);
            }
        
            Value.CheckTruncate(Length, out Difference);
            if (CurrentStorage + Difference > MaximumStorage) {
                Value.Unlock();
                ConditionalTrim();
                
                Log.PrintAsync($"File '{AbsolutePath}' would not fit within mount capacity after truncate. refusing with ENOSPC", LogLevel.Warning);

                return PosixResult.ENOSPC;
            }
        
            PosixResult Result = Value.Truncate(Length);
            Interlocked.Add(ref BaseCurrentCache, Difference);
        
            Value.Unlock();
            ConditionalTrim();

            return Result;
        }
    }

    public void ConditionalDrop (
        in  String AbsolutePath,
        out Int64  Difference
    ) {
        Monitor.Enter(BaseCache);
        if (BaseCache.TryRemove(AbsolutePath, out CacheEntry? Value) == false) {
            Monitor.Exit(BaseCache);
            UnixFileSystemInfo Info = UnixFileSystemInfo.GetFileSystemEntry(AbsolutePath);
            Difference = -Info.Length;
            
            Log.PrintAsync($"File '{AbsolutePath}' has no cache entry length due to no cache entry existing. returning file length instead", LogLevel.Debug);

            return;
        }

        Value.Lock();
        Difference = -Value.Length;
        Interlocked.Add(ref BaseCurrentCache, Difference);
        Value.Dispose();
        Value.Unlock();
        
        Monitor.Exit(BaseCache);
        
        Log.PrintAsync($"Dropped cache entry from memory for file '{AbsolutePath}' without saving to disk", LogLevel.Debug);

    }
    
    public void ConditionalSaveAll () {
        Log.PrintAsync($"Saving cache entries from memory to disk and populating cache preloader list", LogLevel.Debug);

        Monitor.Enter(BaseCache);
        
        Log.PrintAsync($"Populating cache preloader list", LogLevel.Debug);
        
        List<String>  PathList = BaseCache.Keys.ToList();
        StringBuilder Builder  = new StringBuilder(1048576);

        foreach (String Path in PathList) {
            Builder.AppendLine(Path);
        }
        
        String PathListString = Builder.ToString();
        BaseParamController.SetRaw("CachedEntries", PathListString);
        
        Log.PrintAsync($"Populated cache preloader list", LogLevel.Debug);

        Log.PrintAsync($"Saving cache entries from memory to disk", LogLevel.Debug);
        foreach (String ChildAbsolutePath in Directory.GetFiles(BaseRootPath, "*", SearchOption.AllDirectories)) {
            if (BaseCache.TryRemove(ChildAbsolutePath, out CacheEntry? Value) == false) {
                continue;
            }
            Value.Lock();
            Value.Save();
            Interlocked.Add(ref BaseCurrentCache, -Value.Length);
            Value.Dispose();
            Value.Unlock();
            
            Log.PrintAsync($"Saved cache entry from memory to disk for file '{ChildAbsolutePath}'", LogLevel.Debug);
        }
        
        Monitor.Exit(BaseCache);
        
        Log.PrintAsync($"Saved cache entries from memory to disk", LogLevel.Debug);

    }
    
    public void ConditionalSaveFolder (
        in String AbsolutePath
    ) {
        Log.PrintAsync($"Saving cache entries from memory to disk for path {AbsolutePath}", LogLevel.Debug);

        Monitor.Enter(BaseCache);

        foreach (String ChildAbsolutePath in Directory.GetFiles(AbsolutePath, "*", SearchOption.AllDirectories)) {
            if (BaseCache.TryRemove(ChildAbsolutePath, out CacheEntry? Value) == false) {
                continue;
            }

            Value.Lock();
            Value.Save();
            Interlocked.Add(ref BaseCurrentCache, -Value.Length);
            Value.Dispose();
            Value.Unlock();
            
            Log.PrintAsync($"Saved cache entry from memory to disk for file '{ChildAbsolutePath}'", LogLevel.Debug);
        }
        
        GC.Collect(0, GCCollectionMode.Forced, true, true);
        GC.Collect(1, GCCollectionMode.Forced, true, true);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        
        Monitor.Exit(BaseCache);
        
        Log.PrintAsync($"Saved cache entries from memory to disk for path {AbsolutePath}", LogLevel.Debug);
    }
    
    public void ConditionalSaveFile (
        in String AbsolutePath
    ) {
        Monitor.Enter(BaseCache);
        if (BaseCache.TryRemove(AbsolutePath, out CacheEntry? Value) == false) {
            Monitor.Exit(BaseCache);
            return;
        }

        Value.Lock();
        Monitor.Exit(BaseCache);
        
        Value.Save();
        Interlocked.Add(ref BaseCurrentCache, -Value.Length);
        Value.Dispose();
        Value.Unlock();
        
        Log.PrintAsync($"Saved cache entry from memory to disk for file '{AbsolutePath}'", LogLevel.Debug);
    }
    
    public void GetAttributes (
        in  String       AbsolutePath,
        in  FuseFileStat Input,
        out FuseFileStat Output
    ) {
        FuseFileStat Modified = Input;
        
        Monitor.Enter(BaseCache);
        if (BaseCache.TryGetValue(AbsolutePath, out CacheEntry? Value) == false) {
            Monitor.Exit(BaseCache);
            Output = Input;
            return;
        }

        Value.Lock();
        Monitor.Exit(BaseCache);
        Modified.st_size = Value.Length;
        Value.Unlock();
        
        Output = Modified;
    }
    
    private void CheckWrite (
        in  ReadOnlySpan<Byte> Buffer,
        in  Int64              Offset,
        in  Int64              PreviousLength,
        out Int64              Difference
    ) {
        Int64 Before = PreviousLength;
        Int64 After  = Offset + Buffer.Length;
        Difference = Math.Max(0, After - Before);
    }
    
    private void CheckTruncate (
        in  Int64    Length,
        in  Int64    PreviousLength,
        out Int64    Difference
    ) {
        Int64 Before = PreviousLength;
        Int64 After  = Length;
        
        Difference = After - Before;
    }
}