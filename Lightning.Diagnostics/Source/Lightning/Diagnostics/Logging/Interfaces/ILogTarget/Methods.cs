using System;

namespace Lightning.Diagnostics.Logging;

public interface ILogTarget {
    void PrintAsync(LogLevel Severity, String Message);

    void Close ();
}