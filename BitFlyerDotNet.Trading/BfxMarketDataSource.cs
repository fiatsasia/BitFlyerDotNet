//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.asia/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.Trading;

#pragma warning disable CS8618

public class BfxMarketDataSource : IDisposable
{
    public BfTicker Ticker { get; private set; }

    string _productCode;
    BitFlyerClient _client;
    RealtimeSourceFactory _rts;
    CompositeDisposable _disposables = new();

    public BfxMarketDataSource(string productCode, BitFlyerClient client, RealtimeSourceFactory rts)
    {
        _productCode = productCode;
        _client = client;
        _rts = rts;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public async Task InitializeAsync()
    {
        Ticker = await _client.GetTickerAsync(_productCode);
        _rts.GetTickerSource(_productCode).Subscribe(ticker => { Ticker = ticker; }).AddTo(_disposables);
    }
}
