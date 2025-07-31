using Mono.Unix;

namespace RampartFS;

public partial class PathCacheEntry {
    public PathCacheEntry (
        String AbsolutePath
    ) {
        BaseAbsolutePath        = AbsolutePath;
        BaseAccessedOn          = DateTime.UtcNow;
    }
}