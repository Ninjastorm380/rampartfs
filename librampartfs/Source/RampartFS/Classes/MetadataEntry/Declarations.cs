using Mono.Unix.Native;

namespace RampartFS;

public partial class MetadataEntry {
    private static readonly MetadataEntry BaseDefault;
    private readonly Lock BaseLock;
    private Int64 BaseLength;
}