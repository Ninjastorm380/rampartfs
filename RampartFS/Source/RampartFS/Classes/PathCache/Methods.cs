using Mono.Unix;

namespace RampartFS;

public partial class PathCache {
    public PathCache 
    (
        String RootPath
    ) {
        BaseTranslationTable = new Dictionary<String, PathCacheEntry>(TranslationTableMaximum);
        BaseSortingList      = new List<KeyValuePair<String, PathCacheEntry>>(TranslationTableMaximum);
        BaseRootPath         = RootPath;
    }

    public void GetAbsolutePath (
        String RelativePath,
        out String AbsolutePath
    ) {
        lock (BaseTranslationTable) {
            if (BaseTranslationTable.TryGetValue(RelativePath, out PathCacheEntry? Value) == true) {
                AbsolutePath = Value.AbsolutePath;
            }
            else {
                AbsolutePath = Path.Combine(Path.GetFullPath(BaseRootPath), RelativePath.StartsWith(Path.DirectorySeparatorChar) == true ? RelativePath.TrimStart(Path.DirectorySeparatorChar) : RelativePath);
                PathCacheEntry Entry = new PathCacheEntry(AbsolutePath);
                BaseTranslationTable.Add(RelativePath, Entry);

                if (BaseTranslationTable.Count > TranslationTableThreshold) {
                    BaseSortingList.Clear(); foreach (KeyValuePair<String, PathCacheEntry> TableEntry in BaseTranslationTable) {
                        BaseSortingList.Add(TableEntry);
                    } BaseSortingList.Sort(CompareAccessTimes);
                    
                    while (BaseSortingList.Count > TranslationTableTarget) {
                        Int32  Index  = BaseSortingList.Count - 1;
                        String Target = BaseSortingList[Index].Key;
                        BaseTranslationTable.Remove(Target);
                        BaseSortingList.RemoveAt(Index);
                    }
                }
            }
        }
    }
    
    private static Int32 CompareAccessTimes (
        KeyValuePair<String, PathCacheEntry> X,
        KeyValuePair<String, PathCacheEntry> Y
    ) {
        return Y.Value.AccessedOn.CompareTo(X.Value.AccessedOn);
    }
}