namespace RampartFS;

public partial class Buffer : Stream {
    public Byte[] Content {
        get {
            return BaseContent;
        }
    }
}