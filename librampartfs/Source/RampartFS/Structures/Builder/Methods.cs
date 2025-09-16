namespace RampartFS;

internal ref partial struct Builder {
    public Builder(Span<Char> InitialBuffer) {
        BaseBuffer = InitialBuffer;
        BaseLength = 0;
    }

    public void Append(Char Value) {
        BaseBuffer[BaseLength++] = Value;
    }

    public void Append(ReadOnlySpan<Char> Value) {
        Value.CopyTo(BaseBuffer[BaseLength..]);
        BaseLength += Value.Length;
    }

    public override String ToString() {
        return new String(BaseBuffer[..BaseLength]);
    }

    public ReadOnlySpan<Char> AsSpan() {
        return BaseBuffer[..BaseLength];
    }
}