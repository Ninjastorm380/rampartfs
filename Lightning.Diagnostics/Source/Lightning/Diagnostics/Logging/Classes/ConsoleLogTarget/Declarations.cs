using System;
using System.Collections.Concurrent;

namespace Lightning.Diagnostics.Logging;

public partial class ConsoleLogTarget : ILogTarget {
    private static readonly ConcurrentQueue<ValueTuple<LogLevel, String>> PrintQueue;

    private static Boolean Running;
}