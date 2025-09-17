using System.Collections.Concurrent;

namespace RampartFS;

public partial class Filesystem : IDisposable {
    private readonly String BaseStorageFolderPath;
    private readonly String BaseControlFolderPath;
    private readonly String BaseLogFolderPath;
    private readonly String BaseMountFolderPath;
    private readonly Boolean BaseKernelCaching;

    private readonly Int64 BaseDefaultMounted;
    private readonly Int64 BaseDefaultStorageMaximum;
    
    private readonly Surface<Int64> BaseMounted;
    private readonly Surface<Int64> BaseStorageMaximum;
    private readonly Surface<Int64> BaseStorageCurrent;
}