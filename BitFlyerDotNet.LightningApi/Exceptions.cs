//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

public class BitFlyerDotNetException : ApplicationException
{
    public BitFlyerDotNetException() : base() { }
    public BitFlyerDotNetException(string message) : base(message) { }
}

public class BitFlyerUnauthorizedException : BitFlyerDotNetException
{
    public BitFlyerUnauthorizedException() : base() { }
    public BitFlyerUnauthorizedException(string message) : base(message) { }
}

public class BitFlyerApiLimitException : BitFlyerDotNetException
{
    public BitFlyerApiLimitException() : base() { }
    public BitFlyerApiLimitException(string message) : base(message) { }
}
