using Mono.Unix.Native;

namespace RampartFS;

public readonly partial struct FolderChildEntry {
    public String Name {
        get {
            return BaseName;
        }
    }

    public Stat Attributes {
        get {
            return BaseAttributes;
        }
    }
}