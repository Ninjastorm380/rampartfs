namespace RampartFS;

internal partial class CacheEntry {
    private readonly Object       BaseLockObject;
    private readonly String       BaseAbsolutePath;
    private          DateTime     BaseAccessedOn;
    private readonly MemoryStream BaseStream;
    private          Boolean      BaseLoaded;
    private          Boolean      BaseModified;
}