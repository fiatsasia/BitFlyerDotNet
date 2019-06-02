//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;

namespace BitFlyerDotNet.LightningApi
{
    public class BitFlyerDotNetException : ApplicationException
    {
        public BitFlyerDotNetException(string message)
            : base(message)
        {
        }
    }
}
