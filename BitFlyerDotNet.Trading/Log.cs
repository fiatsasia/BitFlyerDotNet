﻿//==============================================================================
// Copyright (c) 2017-2021 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

using System;
using System.Diagnostics;

namespace BitFlyerDotNet.Trading
{
    public class Log
    {
        public static string ModuleName { get; set; } = "BFTRD";
        public static string Now() => DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");
        public static Action<string> WriteLine { get; set; } = message => { Debug.WriteLine(message); };
        public static Action<string> Trace { get; set; } = message => { WriteLine($"{Now()} {ModuleName} TRACE {message}"); };
        public static Action<string> Info { get; set; } = message => { WriteLine($"{Now()} {ModuleName} INFO {message}"); };
        public static Action<string> Warn { get; set; } = message => { WriteLine($"{Now()} {ModuleName} WARN {message}"); };
        public static Action<string> Error { get; set; } = message => { WriteLine($"{Now()} {ModuleName} ERROR {message}"); };
    }
}
