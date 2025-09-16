using Mono.Unix.Native;

namespace RampartFS;

public readonly partial struct FolderChildEntry {
    public FolderChildEntry(String Name, Stat Attributes) {
        BaseName = Name;
        BaseAttributes = Attributes;
    }
}