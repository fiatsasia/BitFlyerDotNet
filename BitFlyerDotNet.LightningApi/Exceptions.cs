//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.LightningApi
{
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
}
