using Mono.Unix.Native;

namespace RampartFS;

public partial class MetadataEntry {
    public Int64 Length {
        get {
            //BaseLock.Enter();
            Int64 Result = BaseLength;
            //BaseLock.Exit();
            
            return Result;
        }
    }
    
    public static MetadataEntry Default {
        get {
            return BaseDefault;
        }
    }
}