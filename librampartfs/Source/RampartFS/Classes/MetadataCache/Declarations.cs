namespace RampartFS;

public partial class MetadataCache {
    private readonly Lock BaseLock;
    private readonly Dictionary<String, MetadataEntry> BaseCache;
}