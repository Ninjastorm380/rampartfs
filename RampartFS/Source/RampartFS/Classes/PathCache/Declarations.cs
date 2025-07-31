namespace RampartFS;

public partial class PathCache {
    private const Int32 TranslationTableMaximum   = 124288;
    private const Int32 TranslationTableThreshold = 124287;
    private const Int32 TranslationTableTarget    = 65536;
    
    private readonly Dictionary<String, PathCacheEntry> BaseTranslationTable;
    private readonly List<KeyValuePair<String, PathCacheEntry>> BaseSortingList;
    private readonly String BaseRootPath;
}