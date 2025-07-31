using FuseDotNet;

namespace RampartFS;

internal partial class Driver : IFuseOperations {
    private readonly Manager   BaseManager;
    private readonly PathCache BasePathCache;
}