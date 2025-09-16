using Mono.Unix.Native;

namespace RampartFS;

public readonly partial struct FolderChildEntry {
    private readonly String BaseName;
    private readonly Stat BaseAttributes;
}