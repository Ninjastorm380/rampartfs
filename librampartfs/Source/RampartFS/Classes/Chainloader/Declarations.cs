using Lightning.Diagnostics.Logging;

namespace RampartFS;

public abstract partial class Chainloader {
    const Int64 DefaultStorageMaximum = 107374182400L;
    const Boolean DefaultLogToConsole = false;
    const Boolean DefaultLogToDisk = true;
    const LogLevel DefaultVerbosity = LogLevel.Warning;
    const Boolean DefaultAsync = true;
}