namespace RampartFS;

internal ref partial struct Builder {
    public Int32 Length {
        get {
            return BaseLength;
        }
        set {
            BaseLength = value;
        }
    }
    
    public Char this[Int32 Index] {
        get {
            return BaseBuffer[Index];
        }
    }
}