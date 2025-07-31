namespace RampartFS;

internal partial class CacheEntry {
    public Int64 Length {
        get {
            return BaseStream.Length;
        }
    }

    public Boolean Loaded {
        get {
            return BaseLoaded;
        }
    }

    public DateTime AccessedOn {
        get {
            return BaseAccessedOn;
        }
    }
}