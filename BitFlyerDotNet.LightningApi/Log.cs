//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace BitFlyerDotNet.LightningApi
{
    public class Log
    {
        public static string ModuleName { get; set; } = "BFAPI";
        public static Action<string> WriteLine { get; set; } = message => { Debug.WriteLine(message); };
        public static Action<string> Trace { get; set; } = message => { WriteLine($"{DateTime.Now} {ModuleName} TRACE {message}"); };
        public static Action<string> Info { get; set; } = message => { WriteLine($"{DateTime.Now} {ModuleName} INFO {message}"); };
        public static Action<string> Warn { get; set; } = message => { WriteLine($"{DateTime.Now} {ModuleName} WARN {message}"); };
        public static Action<string> Error { get; set; } = message => { WriteLine($"{DateTime.Now} {ModuleName} ERROR {message}"); };
    }

#if false
    public static class DebugEx
    {
        [Conditional("DEBUG")]
        public static void Trace()
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)));
        }

        [Conditional("DEBUG")]
        public static void Trace(string format, params object[] args)
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)) + format, args);
        }

        [Conditional("DEBUG")]
        public static void EnterMethod()
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)) + "EnterMethod");
        }

        [Conditional("DEBUG")]
        public static void ExitMethod()
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)) + "ExitMethod");
        }

        static string CreatePrifix(StackFrame sf)
        {
            var method = sf.GetMethod();
            var methodname = method.DeclaringType + "." + method.Name;
            var fileName = Path.GetFileName(sf.GetFileName());
            var lineNum = sf.GetFileLineNumber();
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return $"{fileName}({lineNum}):{methodname}({threadId}) ";
        }
    }
#endif
}
