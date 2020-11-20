//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BitFlyerDotNet.LightningApi
{
    public class BfWithdrawRequest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BfCurrencyCode CurrencyCode { get; set; }

        public int BankAccountId { get; set; }

        [JsonConverter(typeof(DecimalJsonConverter))]
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
        /// <summary>
        /// Withdrawing Funds
        /// <see href="https://scrapbox.io/BitFlyerDotNet/Withdraw">Online help</see>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfWithdrawResponse> Withdraw(BfWithdrawRequest request)
        {
            return PrivatePostAsync<BfWithdrawResponse>(nameof(Withdraw), request, CancellationToken.None).Result;
        }

        /// <summary>
        /// Withdrawing Funds
        /// <see href="https://scrapbox.io/BitFlyerDotNet/Withdraw">Online help</see>
        /// </summary>
        /// <param name="currencyCode"></param>
        /// <param name="bankAccountId"></param>
        /// <param name="amount"></param>
        /// <param name="authenticationCode"></param>
        /// <returns></returns>
        public BitFlyerResponse<BfWithdrawResponse> Withdraw(BfCurrencyCode currencyCode, int bankAccountId, decimal amount, string authenticationCode)
        {
            return Withdraw(new ()
            {
                CurrencyCode = currencyCode,
                BankAccountId = bankAccountId,
                Amount = amount,
                AuthenticationCode = authenticationCode,
            });
        }
    }
}
