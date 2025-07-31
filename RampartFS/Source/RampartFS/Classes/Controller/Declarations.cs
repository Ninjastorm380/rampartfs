using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;

namespace RampartFS;

internal partial class Controller<T> : IDisposable where T : IParsable<T> {
    private readonly ConcurrentDictionary<String, ControllerSurface<T>> BaseController;
    private readonly String                                          BaseRootPath;
    private readonly PathCache                                       BasePathTranslator;
}