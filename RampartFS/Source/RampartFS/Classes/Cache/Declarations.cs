using System.Collections.Concurrent;

namespace RampartFS;

internal partial class Cache {
    private ConcurrentDictionary<String, CacheEntry> BaseCache;
    private Controller<Int64>                        BaseParamController;
    private String                                   BaseRootPath;
    private Int64                                    BaseCurrentCache;
}