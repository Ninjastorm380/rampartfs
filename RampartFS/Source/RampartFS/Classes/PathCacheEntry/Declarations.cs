using Mono.Unix;

namespace RampartFS;

public partial class PathCacheEntry {
    private          DateTime  BaseAccessedOn;
    private readonly String    BaseAbsolutePath; 
}