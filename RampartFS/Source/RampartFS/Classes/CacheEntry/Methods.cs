using FuseDotNet;

namespace RampartFS;

internal partial class CacheEntry : IDisposable {
    public CacheEntry (
        String AbsolutePath
    ) {
        BaseLockObject   = new Object();
        BaseStream       = new MemoryStream(16777216);
        BaseAbsolutePath = AbsolutePath;
        BaseAccessedOn   = DateTime.UtcNow;
        BaseLoaded       = false;
        BaseModified     = false;
    }
    
    public void Lock () {
        Monitor.Enter(BaseLockObject);
    }

    public void Unlock () {
        Monitor.Exit(BaseLockObject);
    }

    public void Load () {
        BaseStream.Position = 0;
        BaseStream.SetLength(0);
        
        FileStream SourceStream = new FileStream(BaseAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        SourceStream.Position = 0;
        SourceStream.CopyTo(BaseStream);
        SourceStream.Flush();
        SourceStream.Close();
        SourceStream.DisposeAsync();
        
        BaseAccessedOn = DateTime.UtcNow;
        BaseLoaded     = true;
    }

    public void Save () {
        BaseStream.Position = 0;

        FileStream TargetStream = new FileStream(BaseAbsolutePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        TargetStream.Position = 0;
        BaseStream.CopyTo(TargetStream);
        TargetStream.Flush();
        TargetStream.Close();
        TargetStream.DisposeAsync();
        
        BaseAccessedOn = DateTime.MinValue;
    }
    
    public PosixResult Read (
        in  Span<Byte> Buffer,
        in  Int64      Offset,
        out Int32      BytesRead
    ) {
        BaseStream.Position = Offset;
        BytesRead           = BaseStream.Read(Buffer);
        
        BaseAccessedOn = DateTime.UtcNow;
        
        return PosixResult.Success;
    }
    
    public PosixResult Write (
        in  ReadOnlySpan<Byte> Buffer,
        in  Int64              Offset,
        out Int32              BytesWritten
    ) {
        BaseStream.Position = Offset;
        BaseStream.Write(Buffer);
        BytesWritten = Buffer.Length;
        
        BaseAccessedOn = DateTime.UtcNow;

        BaseModified = true;
        
        return PosixResult.Success;
    }
    
    public PosixResult CheckWrite (
        in  ReadOnlySpan<Byte> Buffer,
        in  Int64              Offset,
        out Int64              Difference
    ) {
        Int64 Before = BaseStream.Length;
        Int64 After  = Offset + Buffer.Length;
        Difference = Math.Max(0, After - Before);
        
        return PosixResult.Success;
    }
    
    public PosixResult Truncate (
        in  Int64 Length
    ) {
        BaseStream.Position = 0;
        BaseStream.SetLength(Length);
        
        BaseAccessedOn = DateTime.UtcNow;

        BaseModified = true;
        
        return PosixResult.Success;
    }
    
    public PosixResult CheckTruncate (
        in  Int64 Length,
        out Int64 Difference
    ) {
        Int64 Before = BaseStream.Length;
        Int64 After  = Length;
        
        Difference = After - Before;
        
        return PosixResult.Success;
    }
    
    public void Dispose () {
        BaseStream.Position = 0;
        BaseStream.SetLength(0);
        BaseStream.Flush();
        BaseStream.Close();
        BaseStream.Dispose();
        BaseLoaded   = false;
        BaseModified = false;
    }
}