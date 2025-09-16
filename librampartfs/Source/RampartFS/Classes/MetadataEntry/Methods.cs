using Mono.Unix.Native;

namespace RampartFS;

public partial class MetadataEntry {
    static MetadataEntry() {
        BaseDefault = new MetadataEntry(new Stat());
    }
    
    public MetadataEntry(Stat Attributes) {
        BaseLength = Attributes.st_size;
        BaseLock = new Lock();
    }

    public void SetSize(Int64 Length) {
        //BaseLock.Enter();
        BaseLength = Length;
        //BaseLock.Exit();
    }
}