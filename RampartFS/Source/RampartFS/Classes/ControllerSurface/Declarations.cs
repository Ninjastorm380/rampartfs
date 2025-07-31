using System.IO.MemoryMappedFiles;

namespace RampartFS;

internal partial class ControllerSurface<T> : IDisposable where T : IParsable<T> {

    private readonly String            BaseAbsolutePath;
    private readonly FileSystemWatcher BaseWatcher;
    private          T?                BaseCachedValue;
    private          String            BaseCachedValueRaw;
    private readonly T?                BaseDefaults;
}