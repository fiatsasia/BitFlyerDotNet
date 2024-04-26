//==============================================================================
// Copyright (c) 2017-2022 Fiats Inc. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the solution folder for
// full license information.
// https://www.fiats.jp/
// Fiats Inc. Nakano, Tokyo, Japan
//

namespace BitFlyerDotNet.LightningApi;

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

public abstract class BitFlyerResponse
{
    static readonly JsonSerializerSettings _jsonDeserializeSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    public Exception Exception { get; internal set; }
    public HttpStatusCode StatusCode { get; internal set; } = HttpStatusCode.OK;
    public BfErrorResponse ErrorResponse { get; internal set; } = BfErrorResponse.Default;
    public abstract bool IsEmpty { get; }

    public bool IsError => StatusCode != HttpStatusCode.OK || ErrorResponse != BfErrorResponse.Default;
    public bool IsErrorOrEmpty => IsError || IsEmpty;
    public bool IsOk => StatusCode == HttpStatusCode.OK;
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

    internal void Set(HttpStatusCode statusCode, string json)
    {
        StatusCode = statusCode;
        Json = json;
    }

    protected BitFlyerResponse()
    {
    }

    public T Deserialize<T>()
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
        return JsonConvert.DeserializeObject<T>(_json, _jsonDeserializeSettings);
    }
}

public class BitFlyerResponse<T> : BitFlyerResponse
{
    public override bool IsEmpty => string.IsNullOrEmpty(Json) || Json == JsonEmpty;
    string JsonEmpty => (typeof(T) != typeof(string) && typeof(T).IsArray) ? "[]" : "{}";

    T _response = default(T);

    public T Deserialize()
    {
        if (object.Equals(_response, default(T)))
        {
            _response = Deserialize<T>();
        }
        return _response;
    }

    public BitFlyerResponse()
        : base()
    {
        Json = JsonEmpty;
    }
}

public partial class BitFlyerClient : IDisposable
{
    public static readonly JsonSerializerSettings JsonSerializeSettings = new ()
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
    const int ReadCountMax = 500;
    static readonly TimeSpan ApiLimitInterval = TimeSpan.FromMinutes(5);
    const int ApiLimitCount = 500;
    const int ApiLimitterPenaltyMs = 600; // 5min / 500times
    const int OrderApiLimitCount = 300;
    const int OrderApiLimitPenaltyMs = 1000; // 5min / 300times

    HttpClient _client;
    string _apiKey;
    HMACSHA256 _hash;

    public bool IsAuthenticated => _hash != null;
    public long TotalReceivedMessageChars { get; private set; }
    public Func<string, string, bool> ConfirmCallback { get; set; } = (apiName, json) => true;

    CountTimerLimitter _apiLimitter = new (ApiLimitInterval, ApiLimitCount);
    public bool IsApiLimitReached => _apiLimitter.IsLimitReached;
    CountTimerLimitter _orderApiLimitter = new (ApiLimitInterval, OrderApiLimitCount);
    public bool IsOrderLimitReached => _orderApiLimitter.IsLimitReached;

    public BitFlyerClient(TimeSpan? httpTimeout = default)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        _client = new ();
        _client.BaseAddress = new (BaseUri);
        if (httpTimeout.HasValue)
        {
            _client.Timeout = httpTimeout.Value;
        }
    }

    public BitFlyerClient(string apiKey, string apiSecret, TimeSpan? httpTimeout = default)
        : this(httpTimeout)
    {
        _apiKey = apiKey;
        _hash = new (Encoding.UTF8.GetBytes(apiSecret));
    }

    public void Dispose()
    {
        Log.Debug($"{nameof(BitFlyerClient)} disposing...");
        _client?.Dispose();
        _hash?.Dispose();
        Log.Debug($"{nameof(BitFlyerClient)} disposed.");
    }

    public void Authenticate(string apiKey, string apiSecret)
    {
        _apiKey = apiKey;
        _hash = new(Encoding.UTF8.GetBytes(apiSecret));
    }

    internal async Task<BitFlyerResponse<T>> GetAsync<T>(string callerName, string queryParameters, CancellationToken ct)
    {
        var apiName = callerName.Replace("Async", "").ToLower();
        var path = PublicBasePath + apiName;
        if (!string.IsNullOrEmpty(queryParameters))
        {
            path += "?" + queryParameters;
        }
        Log.Trace($"HTTP get {path}");

        using (var request = new HttpRequestMessage(HttpMethod.Get, path))
        {
            var responseObject = new BitFlyerResponse<T>();
            try
            {
                var response = await _client.SendAsync(request, ct);
                responseObject.Set(response.StatusCode, await response.Content.ReadAsStringAsync());
                TotalReceivedMessageChars += responseObject.Json.Length;
                switch (responseObject.StatusCode)
                {
                    case HttpStatusCode.OK:
                        Log.Trace($"HTTP response {responseObject.Json}");
                        break;

                    case (HttpStatusCode)429:
                        throw new BitFlyerApiLimitException($"{apiName}: Too many requests (HTTP response Status=429).");

                    case HttpStatusCode.InternalServerError:
                        // Internal server error causes in mainly reasons are server too busy or in maintanance.
                        // Application will decide terminate, wait or confirm to user.

                    default:
                        Log.Warn($"{apiName} returns {response.StatusCode}");
                        break;
                }
                if (_apiLimitter.CheckLimitReached())
                {
                    Log.Warn($"API limit reached. Inserting {ApiLimitterPenaltyMs}ms delay.");
                    await Task.Delay(ApiLimitterPenaltyMs);
                }
                return responseObject;
            }
            catch (AggregateException aex)
            {
                var ex = aex.InnerException;
                responseObject.Exception = ex;
                if (ex is TaskCanceledException) // Caused timedout
                {
                    responseObject.StatusCode = HttpStatusCode.RequestTimeout;
                }
                else if (ex is HttpRequestException)
                {
                    if (ex.InnerException is WebException)
                    {
                        responseObject.ErrorMessage = ((WebException)ex.InnerException).Status.ToString();
                        responseObject.StatusCode = HttpStatusCode.InternalServerError;
                    }
                }
                else if (ex is WebException)
                {
                    var we = ex.InnerException as WebException;
                    var resp = we.Response as HttpWebResponse;
                    if (resp != null)
                    {
                        responseObject.StatusCode = resp.StatusCode;
                    }
                    else
                    {
                        responseObject.StatusCode = HttpStatusCode.NoContent;
                    }
                }
                else
                {
                    throw ex;
                }
                return responseObject;
            }
        }
    }

    internal async Task<BitFlyerResponse<T>> GetPrivateAsync<T>(string callerName, string queryParameters, CancellationToken ct)
    {
        if (!IsAuthenticated)
        {
            throw new BitFlyerUnauthorizedException("Access key and secret required.");
        }

        var apiName = callerName.Replace("Async", "").ToLower();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ff");
        var path = PrivateBasePath + apiName;
        if (!string.IsNullOrEmpty(queryParameters))
        {
            path += "?" + queryParameters;
        }
        Log.Trace($"HTTP get {path}");

        var text = timestamp + "GET" + path;
        var sign = BitConverter.ToString(_hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();
        using (var request = new HttpRequestMessage(HttpMethod.Get, path))
        {
            request.Headers.Clear();
            request.Headers.Add("ACCESS-KEY", _apiKey);
            request.Headers.Add("ACCESS-TIMESTAMP", timestamp);
            request.Headers.Add("ACCESS-SIGN", sign);

            var responseObject = new BitFlyerResponse<T>();
            try
            {
                var response = await _client.SendAsync(request, ct);
                responseObject.Set(response.StatusCode, await response.Content.ReadAsStringAsync());
                TotalReceivedMessageChars += responseObject.Json.Length;
                switch (responseObject.StatusCode)
                {
                    case HttpStatusCode.OK:
                        Log.TraceJson("HTTP get response", responseObject.Json);
                        break;

                    case HttpStatusCode.Unauthorized:
                        throw new BitFlyerUnauthorizedException($"{apiName}: Permission denied.");

                    case (HttpStatusCode)429:
                        throw new BitFlyerApiLimitException($"{apiName}: Too many requests (HTTP response Status=429).");

                    case HttpStatusCode.InternalServerError:
                        // Internal server error causes in mainly reasons are server too busy or in maintanance.
                        // Application will decide terminate, wait or confirm to user.

                    default:
                        Log.Warn($"{apiName} returns {response.StatusCode}");
                        break;
                }
                if (_apiLimitter.CheckLimitReached())
                {
                    Log.Warn($"API limit reached. Inserting {ApiLimitterPenaltyMs}ms delay.");
                    await Task.Delay(ApiLimitterPenaltyMs);
                }
                return responseObject;
            }
            catch (AggregateException aex)
            {
                var ex = aex.InnerException;
                responseObject.Exception = ex;
                if (ex is TaskCanceledException) // Caused timedout
                {
                    responseObject.StatusCode = HttpStatusCode.RequestTimeout;
                }
                else if (ex is HttpRequestException)
                {
                    if (ex.InnerException is WebException)
                    {
                        responseObject.ErrorMessage = ((WebException)ex.InnerException).Status.ToString();
                        responseObject.StatusCode = HttpStatusCode.InternalServerError;
                    }
                }
                else if (ex is WebException)
                {
                    var we = ex.InnerException as WebException;
                    var resp = we.Response as HttpWebResponse;
                    if (resp != null)
                    {
                        responseObject.StatusCode = resp.StatusCode;
                    }
                    else
                    {
                        responseObject.StatusCode = HttpStatusCode.NoContent;
                    }
                }
                else
                {
                    throw ex;
                }
                return responseObject;
            }
        }
    }

    internal async Task<BitFlyerResponse<T>> PostPrivateAsync<T>(string callerName, object requestObject, CancellationToken ct)
    {
        if (!IsAuthenticated)
        {
            throw new NotSupportedException("Access key and secret required.");
        }

        var apiName = callerName.Replace("Async", "").ToLower();
        var body = JsonConvert.SerializeObject(requestObject, JsonSerializeSettings);
        Log.Trace($"HTTP post {apiName}:{body}");
        if (!ConfirmCallback(apiName, body))
        {
            return new BitFlyerResponse<T>();
        }

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ff");
        var path = PrivateBasePath + apiName;

        var text = timestamp + "POST" + path + body;
        var sign = BitConverter.ToString(_hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();
        using (var request = new HttpRequestMessage(HttpMethod.Post, path))
        using (var content = new StringContent(body))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;
            request.Headers.Clear();
            request.Headers.Add("ACCESS-KEY", _apiKey);
            request.Headers.Add("ACCESS-TIMESTAMP", timestamp);
            request.Headers.Add("ACCESS-SIGN", sign);

            var responseObject = new BitFlyerResponse<T>();
            try
            {
                var response = await _client.SendAsync(request, ct);
                responseObject.Set(response.StatusCode, await response.Content.ReadAsStringAsync());
                TotalReceivedMessageChars += responseObject.Json.Length;
                switch (callerName)
                {
                    case nameof(SendChildOrderAsync):
                    case nameof(SendParentOrderAsync):
                    case nameof(CancelAllChildOrdersAsync):
                        if (_orderApiLimitter.CheckLimitReached())
                        {
                            Log.Warn("Order API limit reached.");
                            await Task.Delay(OrderApiLimitPenaltyMs);
                        }
                        break;
                }
                return responseObject;
            }
            catch (AggregateException aex)
            {
                var ex = aex.InnerException;
                responseObject.Exception = ex;
                if (ex is TaskCanceledException) // Caused timedout
                {
                    responseObject.StatusCode = HttpStatusCode.RequestTimeout;
                    Log.Warn("BitFlyerlient: Request timedout");
                }
                else if (ex is HttpRequestException)
                {
                    if (ex.InnerException is WebException)
                    {
                        responseObject.ErrorMessage = ((WebException)ex.InnerException).Status.ToString();
                        responseObject.StatusCode = HttpStatusCode.InternalServerError;
                        Log.Error($"BitFlyerlient: Internal Server Error {responseObject.ErrorMessage}", ex.InnerException);
                    }
                }
                else if (ex is WebException)
                {
                    var we = ex.InnerException as WebException;
                    var resp = we.Response as HttpWebResponse;
                    if (resp != null)
                    {
                        responseObject.StatusCode = resp.StatusCode;
                    }
                    else
                    {
                        responseObject.StatusCode = HttpStatusCode.NoContent;
                    }
                    Log.Error($"BitFlyerlient: WebException {responseObject.StatusCode}", we);
                }
                else
                {
                    Log.Error($"BitFlyerlient: Unexpected exception", ex);
                    throw ex;
                }
                return responseObject;
            }
        }
    }
}
