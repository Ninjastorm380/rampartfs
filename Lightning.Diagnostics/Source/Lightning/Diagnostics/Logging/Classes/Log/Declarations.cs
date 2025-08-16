using System;
using System.Collections.Generic;

namespace Lightning.Diagnostics.Logging;

public partial class Log
{
    private const String Warning  = "[!|";
    private const String Critical = "[#|";
    private const String Debug    = "[?|";
    private const String Info     = "[•|";

    private const String TimeSplit = " on ";

    private const String DateTimeSplit = "@";
    private const String LineSplit = ", Line ";
    private const String EndCap    = "]: ";
    
    private static readonly List<ILogTarget> OutputTargets = new List<ILogTarget>();
}