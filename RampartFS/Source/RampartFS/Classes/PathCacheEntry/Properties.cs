using Mono.Unix;

namespace RampartFS;

public partial class PathCacheEntry {
    public String AbsolutePath {
        get {
            BaseAccessedOn = DateTime.UtcNow;
            return BaseAbsolutePath;
        }
    }
    
    public DateTime AccessedOn {
        get {
            return BaseAccessedOn;
        }
    }
}