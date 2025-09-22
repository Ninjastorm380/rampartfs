using System.Numerics;

namespace RampartFS;

internal partial class Surface<T> : IDisposable, IEquatable<T> where T : IParsable<T>, IEquatable<T>, INumber<T> {
    private readonly String            BaseAbsolutePath;
    private readonly FileSystemWatcher BaseWatcher;
    private          T                 BaseCachedValue;
    private          String            BaseCachedValueRaw;
    private readonly T                 BaseDefaults;
}