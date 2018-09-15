//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;

using Newtonsoft.Json;

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
        bool IsError { get; }
        bool IsNetworkError { get; }
        bool IsApplicationError { get; }
        string ErrorMessage { get; }
        bool IsUnauthorized { get; }
    }

    public class BitFlyerResponse : IBitFlyerResponse
    {
        public static readonly IBitFlyerResponse Success = new BitFlyerResponse(false, false, "Success");

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
        const string JsonArrayEmpty = "[]";

        static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        internal void ParseResponseMessage(string request, HttpResponseMessage message)
        {
            StatusCode = message.StatusCode;
            Json = message.Content.ReadAsStringAsync().Result;
            if (StatusCode == HttpStatusCode.BadRequest)
            {
                throw new ArgumentException(request); // Requested prameters are illegal
            }
        }

        public HttpStatusCode StatusCode { get; internal set; }
        public BfErrorResponse ErrorResponse { get; internal set; } = BfErrorResponse.Default;
        public bool IsUnauthorized { get { return StatusCode == HttpStatusCode.Unauthorized; } }

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

        public bool IsNetworkError { get { return StatusCode != HttpStatusCode.OK; } }
        public bool IsApplicationError { get { return ErrorResponse != BfErrorResponse.Default; } }
        public bool IsError { get { return StatusCode != HttpStatusCode.OK || ErrorResponse != BfErrorResponse.Default; } }
        public bool IsEmpty { get { return string.IsNullOrEmpty(_json) || _json == JsonArrayEmpty; } }
        public bool IsErrorOrEmpty { get { return IsError || IsEmpty; } }
        public Exception Exception { get; internal set; }

        T _result = default(T);
        public T GetResult()
        {
            if (object.Equals(_result, default(T)))
            {
                _result = JsonConvert.DeserializeObject<T>(_json, _jsonSettings);
            }
            return _result;
        }

        string _json = string.Empty;
        public string Json
        {
            get { return _json; }
            set
            {
                if (value.Contains("error_message"))
                {
                    ErrorResponse = JsonConvert.DeserializeObject<BfErrorResponse>(value, _jsonSettings);
                    _json = string.Empty;
                }
                else
                {
                    _json = value;
                }
            }
        }

        public BitFlyerResponse()
        {
            if (typeof(T) != typeof(string) && typeof(T).IsArray)
            {
                _json = JsonArrayEmpty;
            }
        }
    }

    public partial class BitFlyerClient
    {
        static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        const string _baseUri = "https://api.bitflyer.jp";
        const string _publicBasePath = "/v1/";
        const string _privateBasePath = "/v1/me/";

        HttpClient _client;
        string _key;
        HMACSHA256 _hmac;

        public bool IsPrivateApiEnabled { get { return _hmac != null; } }

        public BitFlyerClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_baseUri);
        }

        public BitFlyerClient(string key, string secret)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            _key = key;
            _hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_baseUri);
        }

        internal BitFlyerResponse<T> Get<T>(string apiName, string queryParameters = "")
        {
            var path = _publicBasePath + apiName.ToLower();
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
            if (!IsPrivateApiEnabled)
            {
                throw new NotSupportedException("Access key and secret required.");
            }

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ff");
            var path = _privateBasePath + apiName.ToLower();
            if (!string.IsNullOrEmpty(queryParameters))
            {
                path += "?" + queryParameters;
            }

            var text = timestamp + "GET" + path;
            var sign = BitConverter.ToString(_hmac.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();
            using (var request = new HttpRequestMessage(HttpMethod.Get, path))
            {
                request.Headers.Clear();
                request.Headers.Add("ACCESS-KEY", _key);
                request.Headers.Add("ACCESS-TIMESTAMP", timestamp);
                request.Headers.Add("ACCESS-SIGN", sign);

                var response = new BitFlyerResponse<T>();
                try
                {
                    response.ParseResponseMessage(text, _client.SendAsync(request).Result);
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

        internal BitFlyerResponse<T> PrivatePost<T>(string apiName, string body)
        {
            if (!IsPrivateApiEnabled)
            {
                throw new NotSupportedException("Access key and secret required.");
            }

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ff");
            var path = _privateBasePath + apiName.ToLower();

            var text = timestamp + "POST" + path + body;
            var sign = BitConverter.ToString(_hmac.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLower();
            using (var request = new HttpRequestMessage(HttpMethod.Post, path))
            using (var content = new StringContent(body))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
                request.Headers.Clear();
                request.Headers.Add("ACCESS-KEY", _key);
                request.Headers.Add("ACCESS-TIMESTAMP", timestamp);
                request.Headers.Add("ACCESS-SIGN", sign);

                var response = new BitFlyerResponse<T>();
                try
                {
                    response.ParseResponseMessage(text, _client.SendAsync(request).Result);
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
