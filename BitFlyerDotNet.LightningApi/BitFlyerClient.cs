//==============================================================================
// Copyright (c) 2017-2020 Fiats Inc. All rights reserved.
// https://www.fiats.asia/
//

using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading;

namespace BitFlyerDotNet.LightningApi
{
    public class BfErrorResponse
    {
        [JsonProperty(PropertyName = "status")]
        public int Status { get; private set; }

        [JsonProperty(PropertyName = "error_message")]
        public string ErrorMessage { get; private set; }

        [JsonProperty(PropertyName = "data")]
        public string Data { get; private set; }

        public static readonly BfErrorResponse Default = default(BfErrorResponse);
    }

    public interface IBitFlyerResponse
    {
        string Json { get; }
        bool IsError { get; }
        bool IsNetworkError { get; }
        bool IsApplicationError { get; }
        string ErrorMessage { get; }
        bool IsUnauthorized { get; }
    }

    public class BitFlyerResponse : IBitFlyerResponse
    {
        public static readonly IBitFlyerResponse Success = new BitFlyerResponse(false, false, "Success");

        public string Json { get; }
        public bool IsError { get { return IsNetworkError || IsApplicationError; } }
        public bool IsNetworkError { get; private set; }
        public bool IsApplicationError { get; private set; }
        public bool IsUnauthorized { get { return false; } }
        public string ErrorMessage { get; private set; }

        public BitFlyerResponse(bool isNetworkError, bool isApplicationError, string errorMessage)
        {
            IsNetworkError = isNetworkError;
            IsApplicationError = isApplicationError;
            ErrorMessage = errorMessage;
        }
    }

    public class BitFlyerResponse<T> : IBitFlyerResponse
    {
        static readonly JsonSerializerSettings _jsonDeserializeSettings = new JsonSerializerSettings
        {
            // To enable after develop/find PascalCaseNamingStrategy()
            //ContractResolver = new DefaultContractResolver { NamingStrategy = new PascalCaseNamingStrategy() },
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        string JsonEmpty => (typeof(T) != typeof(string) && typeof(T).IsArray) ? "[]" : "{}";

        internal void ParseResponseMessage(string request, HttpResponseMessage message)
        {
            StatusCode = message.StatusCode;
            Json = message.Content.ReadAsStringAsync().Result;
        }

        public HttpStatusCode StatusCode { get; internal set; } = HttpStatusCode.OK;
        public BfErrorResponse ErrorResponse { get; internal set; } = BfErrorResponse.Default;
        public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;

        string _errorMessage;
        public string ErrorMessage
        {
            get
            {
                if (!string.IsNullOrEmpty(_errorMessage))
                {
                    return _errorMessage;
                }
                else if (StatusCode != HttpStatusCode.OK)
                {
                    return StatusCode.ToString();
                }
                else
                {
                    return ErrorResponse != BfErrorResponse.Default ? ErrorResponse.ErrorMessage : "Success";
                }
            }
            set { _errorMessage = value; }
        }

        public bool IsNetworkError => StatusCode != HttpStatusCode.OK;
        public bool IsApplicationError => ErrorResponse != BfErrorResponse.Default;
        public bool IsError => StatusCode != HttpStatusCode.OK || ErrorResponse != BfErrorResponse.Default;
        public bool IsEmpty => string.IsNullOrEmpty(_json) || _json == JsonEmpty;
        public bool IsErrorOrEmpty => IsError || IsEmpty;
        public bool IsOk => StatusCode == HttpStatusCode.OK;
        public Exception Exception { get; internal set; }

        T _response = default(T);
        [Obsolete("This method is obsolete. Use Message property instead.", false)]
        public T GetResponse()
        {
            if (IsError)
            {
                if (Exception != null)
                {
                    throw Exception;
                }
                else
                {
                    throw new ApplicationException(ErrorMessage);
                }
            }

            if (object.Equals(_response, default(T)))
            {
                _response = JsonConvert.DeserializeObject<T>(_json, _jsonDeserializeSettings);
            }
            return _response;
        }

        public T GetMessage()
        {
            if (IsError)
            {
                if (Exception != null)
                {
                    throw Exception;
                }
                else
                {
                    throw new ApplicationException(ErrorMessage);
                }
            }

            if (object.Equals(_response, default(T)))
            {
                _response = JsonConvert.DeserializeObject<T>(_json, _jsonDeserializeSettings);
            }
            return _response;
        }

        string _json;
        public string Json
        {
            get { return _json; }
            set
            {
                if (value.Contains("error_message"))
                {
                    ErrorResponse = JsonConvert.DeserializeObject<BfErrorResponse>(value, _jsonDeserializeSettings);
                }
                else
                {
                    _json = value;
                }
            }
        }

        public BitFlyerResponse()
        {
            _json = JsonEmpty;
        }
    }

    public partial class BitFlyerClient : IDisposable
    {
        public static readonly JsonSerializerSettings JsonSerializeSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        const string BaseUri = "https://api.bitflyer.jp";
        const string PublicBasePath = "/v1/";
        const string PrivateBasePath = "/v1/me/";
        const string UsaMarket = "/usa";
        const string EuMarket = "/eu";
        static readonly TimeSpan ApiLimitInterval = TimeSpan.FromMinutes(5);
        const int ApiLimitCount = 500;
        const int ApiLimitterPenaltyMs = 600; // 5min / 500times
        const int OrderApiLimitCount = 300;
        const int OrderApiLimitPenaltyMs = 1000; // 5min / 300times

        HttpClient _client;
        string _apiKey;
        HMACSHA256 _hash;

        public bool IsAuthenticated => _hash != null;
        public BitFlyerClientConfig Config { get; } = new BitFlyerClientConfig();
        public long TotalReceivedMessageChars { get; private set; }
        public Func<string, string, bool> ConfirmCallback { get; set; } = (apiName, json) => true;

        CountTimerLimitter _apiLimitter = new CountTimerLimitter(ApiLimitInterval, ApiLimitCount);
        CountTimerLimitter _orderApiLimitter = new CountTimerLimitter(ApiLimitInterval, OrderApiLimitCount);

        public BitFlyerClient(BitFlyerClientConfig config = null)
        {
            if (config != null)
            {
                Config = config;
            }
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(BaseUri);
        }

        public BitFlyerClient(string apiKey, string apiSecret, BitFlyerClientConfig config = null)
            : this(config)
        {
            _apiKey = apiKey;
            _hash = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        }

        public void Dispose()
        {
            Debug.Print($"{nameof(BitFlyerClient)}.Dispose");
            _client?.Dispose();
            _hash?.Dispose();
            Debug.Print($"{nameof(BitFlyerClient)}.Dispose exit");
        }

        internal BitFlyerResponse<T> Get<T>(string apiName, string queryParameters = "")
        {
            var path = PublicBasePath + apiName.ToLower();
            if (!string.IsNullOrEmpty(queryParameters))
            {
                path += "?" + queryParameters;
            }

            using (var request = new HttpRequestMessage(HttpMethod.Get, path))
            {
                var response = new BitFlyerResponse<T>();
                try
                {
                    response.ParseResponseMessage(path, _client.SendAsync(request).Result);
                    TotalReceivedMessageChars += response.Json.Length;
                    if (_apiLimitter.CheckLimitReached())
                    {
                        Debug.Print("API limit reached.");
                        Thread.Sleep(ApiLimitterPenaltyMs);
                    }
                    return response;
                }
                catch (AggregateException aex)
                {
                    var ex = aex.InnerException;
                    response.Exception = ex;
                    if (ex is TaskCanceledException) // Caused timedout
                    {
                        response.StatusCode = HttpStatusCode.RequestTimeout;
                    }
                    else if (ex is HttpRequestException)
                    {
                        if (ex.InnerException is WebException)
                        {
                            response.ErrorMessage = ((WebException)ex.InnerException).Status.ToString();
                            response.StatusCode = HttpStatusCode.InternalServerError;
                        }
                    }
                    else if (ex is WebException)
                    {
                        var we = ex.InnerException as WebException;
                        var resp = we.Response as HttpWebResponse;
                        if (resp != null)
                        {
                            response.StatusCode = resp.StatusCode;
                        }
                        else
                        {
                            response.StatusCode = HttpStatusCode.NoContent;
                        }
                    }
                    else
                    {
                        throw ex;
                    }
                    return response;
                }
            }
        }

        internal BitFlyerResponse<T> PrivateGet<T>(string apiName, string queryParameters = "")
        {
            if (!IsAuthenticated)
            {
                throw new BitFlyerUnauthorizedException("Access key and secret required.");
            }

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ff");
            var path = PrivateBasePath + apiName.ToLower();
            if (!string.IsNullOrEmpty(queryParameters))
            {
                path += "?" + queryParameters;
            }

            var text = timestamp + "GET" + path;
            var sign = BitConverter.ToString(_hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();
            using (var request = new HttpRequestMessage(HttpMethod.Get, path))
            {
                request.Headers.Clear();
                request.Headers.Add("ACCESS-KEY", _apiKey);
                request.Headers.Add("ACCESS-TIMESTAMP", timestamp);
                request.Headers.Add("ACCESS-SIGN", sign);

                var response = new BitFlyerResponse<T>();
                try
                {
                    response.ParseResponseMessage(text, _client.SendAsync(request).Result);
                    TotalReceivedMessageChars += response.Json.Length;
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new BitFlyerUnauthorizedException($"{apiName}: Permission denied.");
                    }
                    if (_apiLimitter.CheckLimitReached())
                    {
                        Debug.Print("API limit reached.");
                        Thread.Sleep(ApiLimitterPenaltyMs);
                    }
                    return response;
                }
                catch (BitFlyerUnauthorizedException)
                {
                    throw;
                }
                catch (AggregateException aex)
                {
                    var ex = aex.InnerException;
                    response.Exception = ex;
                    if (ex is TaskCanceledException) // Caused timedout
                    {
                        response.StatusCode = HttpStatusCode.RequestTimeout;
                    }
                    else if (ex is HttpRequestException)
                    {
                        if (ex.InnerException is WebException)
                        {
                            response.ErrorMessage = ((WebException)ex.InnerException).Status.ToString();
                            response.StatusCode = HttpStatusCode.InternalServerError;
                        }
                    }
                    else if (ex is WebException)
                    {
                        var we = ex.InnerException as WebException;
                        var resp = we.Response as HttpWebResponse;
                        if (resp != null)
                        {
                            response.StatusCode = resp.StatusCode;
                        }
                        else
                        {
                            response.StatusCode = HttpStatusCode.NoContent;
                        }
                    }
                    else
                    {
                        throw ex;
                    }
                    return response;
                }
            }
        }

        internal BitFlyerResponse<T> PrivatePost<T>(string apiName, object request)
        {
            if (!IsAuthenticated)
            {
                throw new NotSupportedException("Access key and secret required.");
            }

            var body = JsonConvert.SerializeObject(request, JsonSerializeSettings);
            if (!ConfirmCallback(apiName, body))
            {
                return new BitFlyerResponse<T>();
            }

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ff");
            var path = PrivateBasePath + apiName.ToLower();

            var text = timestamp + "POST" + path + body;
            var sign = BitConverter.ToString(_hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();
            using (var message = new HttpRequestMessage(HttpMethod.Post, path))
            using (var content = new StringContent(body))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                message.Content = content;
                message.Headers.Clear();
                message.Headers.Add("ACCESS-KEY", _apiKey);
                message.Headers.Add("ACCESS-TIMESTAMP", timestamp);
                message.Headers.Add("ACCESS-SIGN", sign);

                var response = new BitFlyerResponse<T>();
                try
                {
                    response.ParseResponseMessage(text, _client.SendAsync(message).Result);
                    TotalReceivedMessageChars += response.Json.Length;
                    switch (apiName)
                    {
                        case nameof(SendChildOrder):
                        case nameof(SendParentOrder):
                        case nameof(CancelAllChildOrders):
                            if (_orderApiLimitter.CheckLimitReached())
                            {
                                Debug.Print("Order API limit reached.");
                                Thread.Sleep(OrderApiLimitPenaltyMs);
                            }
                            break;
                    }
                    return response;
                }
                catch (AggregateException aex)
                {
                    var ex = aex.InnerException;
                    response.Exception = ex;
                    if (ex is TaskCanceledException) // Caused timedout
                    {
                        response.StatusCode = HttpStatusCode.RequestTimeout;
                    }
                    else if (ex is HttpRequestException)
                    {
                        if (ex.InnerException is WebException)
                        {
                            response.ErrorMessage = ((WebException)ex.InnerException).Status.ToString();
                            response.StatusCode = HttpStatusCode.InternalServerError;
                        }
                    }
                    else if (ex is WebException)
                    {
                        var we = ex.InnerException as WebException;
                        var resp = we.Response as HttpWebResponse;
                        if (resp != null)
                        {
                            response.StatusCode = resp.StatusCode;
                        }
                        else
                        {
                            response.StatusCode = HttpStatusCode.NoContent;
                        }
                    }
                    else
                    {
                        throw ex;
                    }
                    return response;
                }
            }
        }
    }
}
