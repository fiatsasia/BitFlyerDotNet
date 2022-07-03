//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class LogAdapter
{
    public static string Now() => DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");

    public virtual void WriteLine(string message) => System.Diagnostics.Trace.WriteLine(message);
    public virtual void Trace(string message) => WriteLine($"{Now()} TRACE {message}");
    public virtual void Info(string message) => WriteLine($"{Now()} INFO {message}");
    public virtual void Warn(string message) => WriteLine($"{Now()} WARN {message}");
    public virtual void Error(string message) => WriteLine($"{Now()} ERROR {message}");
    public virtual void Error(Exception ex) => WriteLine($"{Now()} ERROR {ex.Message}");
    public virtual void Error(string message, Exception ex) => WriteLine($"{Now()} ERROR {message} {ex.Message}");
    public virtual void Fatal(string message) => WriteLine($"{Now()} FATAL {message}");
    public virtual void Debug(string message) => WriteLine($"{Now()} DEBUG {message}");
}

public static class Log
{
    public static LogAdapter Instance { get; set; } = new LogAdapter();

    public static void Trace(string message) => Instance?.Trace(message);
    public static void TraceJson(string message, string json) => Instance?.Trace($"{message} {json}");
    public static void Debug(string message) => Instance?.Debug(message);
    public static void Info(string message) => Instance?.Info(message);
    public static void Warn(string message) => Instance?.Warn(message);
    public static void Error(string message) => Instance?.Error(message);
    public static void Error(Exception ex) => Instance?.Error(ex);
    public static void Error(string message, Exception ex) => Instance?.Error(message, ex);
    public static void Fatal(string message) => Instance?.Fatal(message);

    [Conditional("DEBUG")]
    public static void Enter()
    {
        var method = new StackFrame(1, true).GetMethod();
        var methodname = method.DeclaringType + "." + method.Name;
        method = null;
        Instance?.Trace(methodname);
    }

    [Conditional("DEBUG")]
    public static void Enter(string message)
    {
        var method = new StackFrame(1, true).GetMethod();
        var methodname = method.DeclaringType + "." + method.Name;
        method = null;
        Instance?.Trace($"{methodname} : {message}");
    }
}
