using System.Buffers;
using Mono.Unix.Native;

namespace RampartFS;

public partial class MetadataCache {
    public MetadataCache() {
        BaseLock = new Lock();
        BaseCache = new Dictionary<String, MetadataEntry>(252144);
    }
    
    public Errno SetLength(String AbsolutePath, Int32 Handle, Int64 Offset, Int64 Written) {
        //BaseLock.Enter();
        if (BaseCache.TryGetValue(AbsolutePath, out MetadataEntry? Selected) == false) {
            if (Syscall.fstat(Handle, out Stat Attributes) == -1) {
                Errno Result = Stdlib.GetLastError();
                //BaseLock.Exit();
                return Result;
            }
            
            Selected = new MetadataEntry(Attributes);
            BaseCache.TryAdd(AbsolutePath, Selected);
            //BaseLock.Exit();
            return 0;
        }
        //BaseLock.Exit();
        
        Selected.SetSize(Math.Max(Selected.Length, Offset + Written));

        return 0;
    }
    
    public Errno SetLength(String AbsolutePath, Int32 Handle, Int64 Length) {
        //BaseLock.Enter();
        if (BaseCache.TryGetValue(AbsolutePath, out MetadataEntry? Selected) == false) {
            if (Syscall.fstat(Handle, out Stat Attributes) == -1) {
                Errno Result = Stdlib.GetLastError();
                //BaseLock.Exit();
                return Result;
            }
            
            Selected = new MetadataEntry(Attributes);
            BaseCache.TryAdd(AbsolutePath, Selected);
            //BaseLock.Exit();
            return 0;
        }
        //BaseLock.Exit();
        
        Selected.SetSize(Length);

        return 0;
    }
    
    public Errno SetLength(String AbsolutePath, Int64 Length) {
        //BaseLock.Enter();
        if (BaseCache.TryGetValue(AbsolutePath, out MetadataEntry? Selected) == false) {
            if (Syscall.lstat(AbsolutePath, out Stat Attributes) == -1) {
                Errno Result = Stdlib.GetLastError();
                //BaseLock.Exit();
                return Result;
            }
            
            Selected = new MetadataEntry(Attributes);
            BaseCache.TryAdd(AbsolutePath, Selected);
            //BaseLock.Exit();
            return 0;
        }
        //BaseLock.Exit();
        
        Selected.SetSize(Length);
        return 0;
    }
    
    public Errno GetLength(String AbsolutePath, Int32 Handle, out Int64 Length) {
        //BaseLock.Enter();
        if (BaseCache.TryGetValue(AbsolutePath, out MetadataEntry? Selected) == false) {
            if (Syscall.fstat(Handle, out Stat Attributes) == -1) {
                Errno Result = Stdlib.GetLastError();
                //BaseLock.Exit();
                Length = 0;
                return Result;
            }
            
            Selected = new MetadataEntry(Attributes);
            BaseCache.TryAdd(AbsolutePath, Selected);
        }
        //BaseLock.Exit();
        
        Length = Selected.Length;

        return 0;
    }
    
    public Errno GetLength(String AbsolutePath, out Int64 Length) {
        //BaseLock.Enter();
        if (BaseCache.TryGetValue(AbsolutePath, out MetadataEntry? Selected) == false) {
            if (Syscall.lstat(AbsolutePath, out Stat Attributes) == -1) {
                Errno Result = Stdlib.GetLastError();
                //BaseLock.Exit();
                Length = 0;
                return Result;
            }
            
            Selected = new MetadataEntry(Attributes);
            BaseCache.TryAdd(AbsolutePath, Selected);
        }
        //BaseLock.Exit();
        
        Length = Selected.Length;
        
        return 0;
    }
    
    public Errno Drop(String OldAbsolutePath, String NewAbsolutePath) {
        //BaseLock.Enter();
        
        List<KeyValuePair<String, MetadataEntry>>[] Reference = ArrayPool<List<KeyValuePair<String, MetadataEntry>>>.Shared.Rent(2);

        if (Reference[0] == null!) {
            Reference[0] = [];
        }
            
        if (Reference[1] == null!) {
            Reference[1] = [];
        }
            
        List<KeyValuePair<String, MetadataEntry>> OldEntries = Reference[0];
        List<KeyValuePair<String, MetadataEntry>> NewEntries = Reference[1];
            
        OldEntries.Clear();
        NewEntries.Clear();
            
        foreach (KeyValuePair<String, MetadataEntry> Entry in BaseCache) {
            if (Entry.Key.StartsWith(OldAbsolutePath)) {
                OldEntries.Add(Entry);
            }
            if (Entry.Key.StartsWith(NewAbsolutePath)) {
                NewEntries.Add(Entry);
            }
        }
            
        foreach (KeyValuePair<String, MetadataEntry> Entry in NewEntries) {
            BaseCache.Remove(Entry.Key, out _);
        }

        foreach (KeyValuePair<String, MetadataEntry> Entry in OldEntries) {
            BaseCache.Remove(Entry.Key, out _);
        }
            
        ArrayPool<List<KeyValuePair<String, MetadataEntry>>>.Shared.Return(Reference);
            
        BaseCache.Remove(OldAbsolutePath, out _);
        BaseCache.Remove(NewAbsolutePath, out _);
        
        //BaseLock.Exit();
        return 0;
    }
    
    public Errno Drop(String AbsolutePath) {
        //BaseLock.Enter();
        
        List<KeyValuePair<String, MetadataEntry>>[] Reference = ArrayPool<List<KeyValuePair<String, MetadataEntry>>>.Shared.Rent(2);

        if (Reference[0] == null!) {
            Reference[0] = [];
        }
            
        if (Reference[1] == null!) {
            Reference[1] = [];
        }
            
        List<KeyValuePair<String, MetadataEntry>> OldEntries = Reference[0];
            
        OldEntries.Clear();
        
        foreach (KeyValuePair<String, MetadataEntry> Entry in BaseCache) {
            if (Entry.Key.StartsWith(AbsolutePath)) {
                OldEntries.Add(Entry);
            }
        }

        foreach (KeyValuePair<String, MetadataEntry> Entry in OldEntries) {
            BaseCache.Remove(Entry.Key, out _);
        }
            
        ArrayPool<List<KeyValuePair<String, MetadataEntry>>>.Shared.Return(Reference);
            
        BaseCache.Remove(AbsolutePath, out _);
        
        //BaseLock.Exit();
        return 0;
    }
}