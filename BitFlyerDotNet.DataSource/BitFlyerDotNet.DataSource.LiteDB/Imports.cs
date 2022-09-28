//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

global using System;
global using System.Linq;
global using System.IO;
global using System.Collections.Generic;
global using System.Collections.Concurrent;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Runtime.Serialization;
global using System.Reactive.Linq;
global using System.Reactive.Threading.Tasks;
global using System.Reactive.Disposables;

global using LiteDB;

global using BitFlyerDotNet.LightningApi;

#if !NET5_0_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit { }
}
#endif
