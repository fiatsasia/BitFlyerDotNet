//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using BitFlyerDotNet.LightningApi;

namespace BitFlyerDotNet.Trading
{
    public partial class BfTradingAccount
    {
        public BitFlyerClient Client { get; private set; }
        public RealtimeSourceFactory RealtimeSource { get; private set; }

        Dictionary<BfProductCode, BfTradingMarket> _markets = new Dictionary<BfProductCode, BfTradingMarket>();
        public bool IsMarketAvailable(BfProductCode code) => _markets.ContainsKey(code);

        public BfTradingAccount()
        {
        }

        public BfTradingAccount(BitFlyerClient client, RealtimeSourceFactory realtimeSource)
        {
            Client = client;
            RealtimeSource = realtimeSource;
        }

        public void Initialize()
        {
            if (Client == null)
            {
                Client = new BitFlyerClient();
            }
            if (RealtimeSource == null)
            {
                RealtimeSource = new RealtimeSourceFactory(Client);
            }
            RealtimeSource.AvailableMarkets.ForEach(productCode =>
            {
                _markets.Add(productCode, new BfTradingMarket(this, productCode));
            });
        }

        public void Login(string apiKey, string apiSecret)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentException("Invalid API key or secret.");
            }

            if (Client == null)
            {
                Client = new BitFlyerClient(apiKey, apiSecret);
            }
            else
            {
                Client.ApplyApiKeyAndSecrets(apiKey, apiSecret);
            }

            // Check API permissions
            var permissions = Client.GetPermissions().GetResult();
            if (!permissions.Where(e => e.Contains("v1/me/")).Any())
            {
                throw new BitFlyerDotNetException("Any of enabled private API permission is not found.");
            }

            Initialize();
        }

        public void Logout()
        {
            Client?.Dispose();
            Client = null;
        }

        public BfTradingMarket GetMarket(BfProductCode productCode)
        {
            return _markets[productCode];
        }
    }
}
