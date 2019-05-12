//==============================================================================
// Copyright (c) 2017-2019 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfWithdrawRequest
    {
        [JsonProperty(PropertyName = "currency_code")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BfCurrencyCode CurrencyCode { get; set; }

        [JsonProperty(PropertyName = "bank_account_id")]
        public int BankAccountId { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public decimal Amount { get; set; }

        [JsonProperty(PropertyName = "code")]
        public string AuthenticationCode { get; set; }
        public bool ShouldSerializeAuthenticationCode() { return !string.IsNullOrEmpty(AuthenticationCode); }
    }

    public class BfWithdrawResponse
    {
        [JsonProperty(PropertyName = "message_id")]
        public string MessageId { get; private set; }
    }

    public partial class BitFlyerClient
    {
        public BitFlyerResponse<BfWithdrawResponse> Withdraw(BfWithdrawRequest request)
        {
            var jsonRequest = JsonConvert.SerializeObject(request, _jsonSettings);
            return PrivatePost<BfWithdrawResponse>(nameof(Withdraw), jsonRequest);
        }

        public BitFlyerResponse<BfWithdrawResponse> Withdraw(BfCurrencyCode currencyCode, int bankAccountId, decimal amount, string authenticationCode)
        {
            return Withdraw(new BfWithdrawRequest
            {
                CurrencyCode = currencyCode,
                BankAccountId = bankAccountId,
                Amount = amount,
                AuthenticationCode = authenticationCode,
            });
        }
    }
}
