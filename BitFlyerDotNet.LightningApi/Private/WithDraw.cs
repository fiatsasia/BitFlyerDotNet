//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    class BfWithdrawRequest
    {
        [JsonProperty(PropertyName = "currency_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfCurrencyCode CurrencyCode { get; internal set; }

        [JsonProperty(PropertyName = "bank_account_id")]
        public int BankAccountId { get; internal set; }

        [JsonProperty(PropertyName = "amount")]
        public double Amount { get; internal set; }

        [JsonProperty(PropertyName = "code")]
        public string AuthenticationCode { get; internal set; }
        public bool ShouldSerializeAuthenticationCode() { return !string.IsNullOrEmpty(AuthenticationCode); }
    }

    public class BfWithdrawResponse
    {
        [JsonProperty(PropertyName = "message_id")]
        public string MessageId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfWithdrawResponse> Withdraw(BfCurrencyCode currencyCode, int bankAccountId, double amount, string authenticationCode)
        {
            var request = new BfWithdrawRequest
            {
                CurrencyCode = currencyCode,
                BankAccountId = bankAccountId,
                Amount = amount,
                AuthenticationCode = authenticationCode,
            };
            var jsonRequest = JsonConvert.SerializeObject(request, _jsonSettings);
            return PrivatePost<BfWithdrawResponse>(nameof(Withdraw), jsonRequest);
        }
    }
}
