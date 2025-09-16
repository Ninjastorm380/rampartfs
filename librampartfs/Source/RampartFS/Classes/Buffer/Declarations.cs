namespace RampartFS;

public partial class Buffer : Stream {
    private readonly Lock BaseLock;
    private readonly Boolean BaseCanRead;
    private readonly Boolean BaseCanSeek;
    private readonly Boolean BaseCanWrite;
    
    private Byte[] BaseContent;
    private Int32 BasePosition;
    private Int32 BaseLength;
}