using System.Runtime.CompilerServices;

namespace RampartFS;

public partial class Buffer : Stream {
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] private void Increase(Int32 Value) {
        if (Value > BaseContent.Length) {
            Array.Resize(ref BaseContent, Value);
        }

        if (Value > BaseLength) {
            BaseLength = Value;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] private void Increase(Int64 Value) {
        if (Value > BaseContent.Length) {
            Array.Resize(ref BaseContent, (Int32)Value);
        }

        if (Value > BaseLength) {
            BaseLength = (Int32)Value;
        }
    }
    
    public Buffer() {
        BaseLock = new Lock();
        Array.Resize(ref BaseContent, 1024);
        BasePosition = 0;
        BaseLength = 0;
        BaseCanRead = true;
        BaseCanSeek = true;
        BaseCanWrite = true;
    }

    public override void Flush() {
        BaseLock.Enter();
        
        BasePosition = 0;
        BaseLength = 0;
        
        BaseLock.Exit();
    }

    public override Int32 Read(Byte[] Buffer, Int32 Offset, Int32 Count) {
        return Read32(Buffer, Offset, Count);
    }

    public override Int64 Seek(Int64 Offset, SeekOrigin Origin) {
        return Seek32((Int32)Offset, Origin);
    }

    public override void SetLength(Int64 Value) {
        SetLength32((Int32)Value);
    }
    
    public override void Write(Byte[] Buffer, Int32 Offset, Int32 Count) {
        Write32(Buffer, Offset, Count);
    }
    
    public Int32 Read32(Byte[] Buffer, Int32 Offset, Int32 Count) {
        BaseLock.Enter();
        if (BasePosition >= BaseLength) {
            BaseLock.Exit();
            return 0;
        }
        
        if (BasePosition + Count > BaseLength) {
            Int32 Remaining = BaseLength - BasePosition;
            Array.Copy(BaseContent, BasePosition, Buffer, Offset, Remaining);
            BasePosition += Remaining;
            
            BaseLock.Exit();

            return Remaining;
        }
        
        Array.Copy(BaseContent, BasePosition, Buffer, Offset, Count);
        BasePosition += Count;

        BaseLock.Exit();
        return Count;
    }

    public Int64 Seek32(Int32 Offset, SeekOrigin Origin) {
        BaseLock.Enter();
        
        BasePosition = Origin switch {
            SeekOrigin.Begin   => Offset,
            SeekOrigin.Current => BasePosition + Offset,
            SeekOrigin.End     => BaseLength + Offset,
            _                  => BasePosition
        }; Increase(BasePosition);
        
        BaseLock.Exit();
        
        return BasePosition;
    }

    public void SetLength32(Int32 Value) {
        BaseLock.Enter();
        
        if (Value > BaseContent.Length) {
            Array.Resize(ref BaseContent, Value);
        } BaseLength = Value;
        
        if (BasePosition > BaseLength) {
            BasePosition = BaseLength;
        }
        
        BaseLock.Exit();
    }
    
    public void Write32(Byte[] Buffer, Int32 Offset, Int32 Count) {
        BaseLock.Enter();
        
        Increase(BasePosition + Count);
        Array.Copy(Buffer, Offset, BaseContent, BasePosition, Count);
        BasePosition += Count;
        
        BaseLock.Exit();
    }

    public override Boolean CanRead {
        get {
            return BaseCanRead;
        }
    }

    public override Boolean CanSeek {
        get {
            return BaseCanSeek;
        }
    }

    public override Boolean CanWrite {
        get {
            return BaseCanWrite;
        }
    }

    public override Int64 Length {
        get {
            BaseLock.Enter();
            
            Int64 LocalLength = BaseLength;
            
            BaseLock.Exit();
            
            return LocalLength;
        }
    }

    public override Int64 Position {
        get {
            BaseLock.Enter();
            
            Int64 LocalPosition = BasePosition;
            
            BaseLock.Exit();
            
            return LocalPosition;
        }
        set {
            BaseLock.Enter();
            
            Increase(value);
            BasePosition = (Int32)value;
            
            BaseLock.Exit();
        }
    }
}