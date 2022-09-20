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
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Infrastructure;

global using BitFlyerDotNet.LightningApi;
