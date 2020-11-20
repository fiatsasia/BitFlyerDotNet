//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

namespace BitFlyerDotNet.LightningApi
{
    public interface IBitFlyerResponse
    {
        string Json { get; }
        bool IsError { get; }
        bool IsNetworkError { get; }
        bool IsApplicationError { get; }
        string ErrorMessage { get; }
        bool IsUnauthorized { get; }
    }
}
