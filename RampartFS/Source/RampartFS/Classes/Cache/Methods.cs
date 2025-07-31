using System.Collections.Concurrent;
using System.Text;
using FuseDotNet;
using Mono.Unix;

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
        Int64 MinimumTrim  = BaseParamController["MinimumTrim", 0];
        
        if (BaseCurrentCache >= MaximumCache) {
            List<KeyValuePair<String, CacheEntry>> SortedEntries = BaseCache.ToList();
            SortedEntries.Sort(Comparison);

            Int64 TrimAmount = 0;
            List<KeyValuePair<String, CacheEntry>> SelectedEntries = new List<KeyValuePair<String, CacheEntry>>(SortedEntries.Count);
            for (Int32 Index = 0; Index < SortedEntries.Count; Index++) {
                KeyValuePair<String, CacheEntry> SelectedEntry = SortedEntries[Index];
                SelectedEntry.Value.Lock();
                SelectedEntries.Add(SelectedEntry);
                TrimAmount = TrimAmount + SelectedEntry.Value.Length;
                
                if (TrimAmount >= Math.Min(Math.Max(1048576, MinimumTrim), MaximumCache)) {
                    break;
                }
            }
            
            foreach (KeyValuePair<String, CacheEntry> Sorted in SelectedEntries) {
                Sorted.Value.Save();
                Interlocked.Add(ref BaseCurrentCache, -Sorted.Value.Length);
                Sorted.Value.Dispose();
                BaseCache.TryRemove(Sorted.Key, out _);
                Sorted.Value.Unlock();
            }
            
            GC.Collect(0, GCCollectionMode.Forced, true, true);
            GC.Collect(1, GCCollectionMode.Forced, true, true);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.Collect(3, GCCollectionMode.Forced, true, true);
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
        Monitor.Enter(BaseCache);
        if (BaseCache.TryGetValue(AbsolutePath, out CacheEntry? Value) == false) {
            Value = new CacheEntry(AbsolutePath);
            BaseCache.TryAdd(AbsolutePath, Value);
        }

        Value.Lock();
        
        Monitor.Exit(BaseCache);

        if (Value.Loaded == false) {
            Value.Load();
            Interlocked.Add(ref BaseCurrentCache, Value.Length);
        }

        PosixResult Result = Value.Read(Buffer, Offset, out BytesRead);
        
        Value.Unlock();
        
        ConditionalTrim();

        return Result;
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
        Monitor.Enter(BaseCache);
        if (BaseCache.TryGetValue(AbsolutePath, out CacheEntry? Value) == false) {
            Value = new CacheEntry(AbsolutePath);
            BaseCache.TryAdd(AbsolutePath, Value);
        }

        Value.Lock();
        
        Monitor.Exit(BaseCache);

        if (Value.Loaded == false) {
            Value.Load();
            Interlocked.Add(ref BaseCurrentCache, Value.Length);
        }

        Value.CheckWrite(Buffer, Offset, out Difference);
        if (CurrentStorage + Difference > MaximumStorage) {
            BytesWritten = 0;
            Value.Unlock();
            ConditionalTrim();
            return PosixResult.ENOSPC;
        }
        
        PosixResult Result = Value.Write(Buffer, Offset, out BytesWritten);
        Interlocked.Add(ref BaseCurrentCache, Difference);
        Value.Unlock();
        ConditionalTrim();

        return Result;
    }
    
    public PosixResult TruncateFile (
        in  String AbsolutePath,
        in  Int64  Length,
        out Int64  Difference,
        in  Int64  CurrentStorage,
        in  Int64  MaximumStorage
    ) {
        Monitor.Enter(BaseCache);
        if (BaseCache.TryGetValue(AbsolutePath, out CacheEntry? Value) == false) {
            Value = new CacheEntry(AbsolutePath);
            BaseCache.TryAdd(AbsolutePath, Value);
        }

        Value.Lock();
        
        Monitor.Exit(BaseCache);

        if (Value.Loaded == false) {
            Value.Load();
            Interlocked.Add(ref BaseCurrentCache, Value.Length);
        }
        
        Value.CheckTruncate(Length, out Difference);
        if (CurrentStorage + Difference > MaximumStorage) {
            Value.Unlock();
            ConditionalTrim();
            return PosixResult.ENOSPC;
        }
        
        PosixResult Result = Value.Truncate(Length);
        Interlocked.Add(ref BaseCurrentCache, Difference);
        
        Value.Unlock();
        ConditionalTrim();

        return Result;
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
            return;
        }

        Value.Lock();
        Difference = -Value.Length;
        Interlocked.Add(ref BaseCurrentCache, Difference);
        Value.Dispose();
        Value.Unlock();
        
        Monitor.Exit(BaseCache);
    }
    
    public void ConditionalSaveAll () {
        Monitor.Enter(BaseCache);

        List<String>  PathList = BaseCache.Keys.ToList();
        StringBuilder Builder  = new StringBuilder(1048576);

        foreach (String Path in PathList) {
            Builder.AppendLine(Path);
        }
        
        String PathListString = Builder.ToString();
        BaseParamController.SetRaw("CachedEntries", PathListString);
        

        foreach (String ChildAbsolutePath in Directory.GetFiles(BaseRootPath, "*", SearchOption.AllDirectories)) {
            if (BaseCache.TryRemove(ChildAbsolutePath, out CacheEntry? Value) == false) {
                continue;
            }
            Value.Lock();
            Value.Save();
            Interlocked.Add(ref BaseCurrentCache, -Value.Length);
            Value.Dispose();
            Value.Unlock();
        }
        
        Monitor.Exit(BaseCache);
    }
    
    public void ConditionalSaveFolder (
        in String AbsolutePath
    ) {
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
        }
        
        GC.Collect(0, GCCollectionMode.Forced, true, true);
        GC.Collect(1, GCCollectionMode.Forced, true, true);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.Collect(3, GCCollectionMode.Forced, true, true);
        
        Monitor.Exit(BaseCache);
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
}