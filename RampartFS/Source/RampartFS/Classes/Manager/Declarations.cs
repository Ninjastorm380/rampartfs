namespace RampartFS;

internal partial class Manager {
    private const Int32 AT_FDCWD = -100;
    
    private readonly Cache      BaseCache;
    private readonly Controller<Int64> BaseController;
    private          Int64  BaseCurrentStorage;
}