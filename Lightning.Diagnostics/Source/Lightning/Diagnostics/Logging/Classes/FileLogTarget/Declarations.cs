using System;
using System.Collections.Concurrent;
using System.IO;

namespace Lightning.Diagnostics.Logging;

public partial class FileLogTarget : ILogTarget {
    private readonly ConcurrentQueue<ValueTuple<LogLevel, String>> PrintQueue;

    private Boolean Running;

    private readonly FileStream   OutputStream;
    private readonly StreamWriter OutputWriter;
}