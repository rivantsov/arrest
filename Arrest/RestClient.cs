using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Security;
using System.Net.Http.Headers;
using System.Threading;
using System.IO;
using Arrest.Json;
using Arrest.Internals;
using System.Diagnostics;

namespace Arrest {

  /// <summary>REST client class. </summary>
  public partial class RestClient {

    #region static members
    // Note on multi-threading, and reuse of HttpClient: Async methods are thread-safe, see Remarks section here: 
    // https://msdn.microsoft.com/en-us/library/system.net.http.httpclient(v=vs.110).aspx
    // Turns out you MUST use a global singleton of  HttpClient: 
    //   http://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
    //   https://www.infoq.com/news/2016/09/HttpClient
    // We create singleton of HttpClientHandler which actually handles all HTTP interactions. 
    public static HttpClientHandler SharedHttpClientHandler { get; private set; }

    #endregion 

    /// <summary>Identifier of the client, written to log, to easier identify entries.</summary> 
    public string ClientName;
    public readonly RestClientSettings Settings;
    public readonly HttpClient HttpClient;
    public CancellationToken CancellationToken;
    public readonly object AppContext;
    public HttpRequestHeaders DefaultRequestHeaders => HttpClient.DefaultRequestHeaders;


    #region constructors

    public RestClient(string baseUrl, object appContext = null,
                     CancellationToken? cancellationToken = null,
                     JsonNameMapping nameMapping = JsonNameMapping.Default, 
                     Type badRequestContentType = null)
      : this(new RestClientSettings(baseUrl, new JsonContentSerializer(nameMapping), badRequestContentType: badRequestContentType),
            appContext: appContext) { }

    public RestClient(RestClientSettings settings, string clientName = null, object appContext = null, 
                 CancellationToken? cancellationToken = null, HttpClient httpClient = null) {
      RestClientSettings.Validate(settings); 
      Settings = settings;
      AppContext = appContext;
      ClientName =  clientName;
      if (cancellationToken.HasValue)
        CancellationToken = cancellationToken.Value;
      // In Web environment (Asp.NET core) the HttpClient should be created using IHttpClientFactory
      //  https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-2.2
      //  In this case create client through this factory and pass it here

      if (httpClient != null) {
        this.HttpClient = httpClient;
      } else {
        // create global singleton handler if not created yet
        SharedHttpClientHandler = SharedHttpClientHandler ?? new HttpClientHandler();
        HttpClient = new HttpClient(SharedHttpClientHandler);
      }
    }

    #endregion

    #region Headers

    public void AddAuthorizationHeader(string headerValue, string scheme = "Bearer") {
      DefaultRequestHeaders.Authorization = null; 
      DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, headerValue);
    }
    public void AddRequestHeader(string name, string value) {
      DefaultRequestHeaders.Add(name, value);
    }
    public void RemoveRequestHeader(string name) {
      if (DefaultRequestHeaders.Contains(name))
        DefaultRequestHeaders.Remove(name);
    }

    #endregion

    #region public get/post/put/delete methods 

    public Task<TResult> GetAsync<TResult>(string url, params object[] args) {
      return SendAsyncImpl<object, TResult>(HttpMethod.Get, url, args, null);
    }//method

    public Task<TResult> PostAsync<TContent, TResult>(TContent body, string url, params object[] args) {
      return SendAsyncImpl<TContent, TResult>(HttpMethod.Post, url, args, body);
    }

    public Task<TResult> PutAsync<TContent, TResult>(TContent body, string url, params object[] args) {
      return SendAsyncImpl<TContent, TResult>(HttpMethod.Put, url, args, body);
    }

    public Task<HttpStatusCode> DeleteAsync(string url, params object[] args) {
      return SendAsyncImpl<DBNull, HttpStatusCode>(HttpMethod.Delete, url, args, null);
    }

    public async Task<byte[]> GetBinaryAsync(string url, object[] args = null, string acceptMediaType = "application/octet-stream") {
      var resultContent = await SendAsyncImpl<DBNull, HttpContent>(HttpMethod.Get, url, args, null, acceptMediaType: acceptMediaType);
      var result = await resultContent.ReadAsByteArrayAsync();
      return result;
    }

    public async Task<string> GetStringAsync(string url, object[] args = null, string acceptMediaType = "text/plain") {
      var resultContent = await SendAsyncImpl<object, HttpContent>(HttpMethod.Get, url, args, 
                            null, acceptMediaType: acceptMediaType);
      var result = await resultContent.ReadAsStringAsync();
      return result;
    }

    public async Task<TResult> SendAsync<TContent, TResult>(HttpMethod method, TContent body, string urlTemplate, object[] args, 
             string acceptMediaType = null) {
      return await SendAsyncImpl<TContent, TResult>(method, urlTemplate, args, body, acceptMediaType);
    }
    #endregion

    #region  URL formatting helpers
    /// <summary>
    /// Formats URL from a template with standard numeric placeholders, ex: {0}. All argument values 
    /// are URL-escaped. The returned URL is full URL, starting with base service address. 
    /// </summary>
    /// <param name="template">URL template, not including base service part.</param>
    /// <param name="args">Arguments to insert into the template.</param>
    /// <returns>Full formatted URL.</returns>
    public string FormatUrl(string template, params object[] args) {
      if (string.IsNullOrWhiteSpace(template))
        return Settings.ServiceUrl;
      var url = RestClientHelper.FormatUrl(template, args);
      string fullUrl;
      //Check if template is abs URL
      if (url.StartsWith("http://") || template.StartsWith("https://"))
        fullUrl = template;
      else {
        var ch0 = url[0];
        var needDelim = ch0 != '/' && ch0 != '?';
        var delim = needDelim ? "/" : string.Empty;
        fullUrl = Settings.ServiceUrl + delim + url;
      }
      return fullUrl;
    }

    /// <summary>
    /// Formats the query part of URL from properties (fields) of an object (names and values).
    /// Null-value parameters are skipped. All values are URL-escaped. 
    /// </summary>
    /// <param name="queryParams">Query parameters object.</param>
    /// <returns>Constructed query part.</returns>
    public string BuildUrlQuery(object queryParams) {
      return RestClientHelper.BuildUrlQuery(queryParams); 
    }

    #endregion

    //For staging sites, to allow using https with self-issued certificates
    public static void SetAllowSelfIssuedCertificates() {
      ServicePointManager.ServerCertificateValidationCallback = (x1, x2, x3, x4) => true;
    }


    #region private methods

    private async Task<TResult> SendAsyncImpl<TBody, TResult>(HttpMethod method, string urlTemplate, object[] urlParameters,
                        TBody body, string acceptMediaType = null) {
      var start = GetTimestamp();
      var callData = new RestCallData() {
        StartedAtUtc = RestClientHelper.GetUtc(),
        HttpMethod = method,
        UrlTemplate = urlTemplate,
        UrlParameters = urlParameters,
        Url = FormatUrl(urlTemplate, urlParameters),
        RequestBodyType = typeof(TBody),
        ResponseBodyType = typeof(TResult),
        RequestBodyObject = body,
        AcceptMediaType = acceptMediaType ?? Settings.ExplicitAcceptList ?? Settings.Serializer.ContentTypes,
      };

      // Create RequestMessage, setup headers, serialize body
      callData.Request = new HttpRequestMessage(callData.HttpMethod, callData.Url);
      var headers = callData.Request.Headers;
      headers.Add("accept", callData.AcceptMediaType);
      foreach (var kv in this.DefaultRequestHeaders)
        headers.Add(kv.Key, kv.Value);
      BuildHttpRequestContent(callData);

      Settings.Events.OnSendingRequest(this, callData);

      //actually make a call
      callData.Response = await HttpClient.SendAsync(callData.Request, this.CancellationToken);
      callData.TimeElapsed = GetTimeSince(start); //measure time in case we are about to cancel and throw

      //check error
      if (callData.Response.IsSuccessStatusCode) {
        Settings.Events.OnReceivedResponse(this, callData);
        await ReadResponseBodyAsync(callData).ConfigureAwait(false);
      } else {
        callData.Exception = await this.ReadErrorResponseAsync(callData);
        Settings.Events.OnReceivedError(this, callData);
      }
      // get time again to include deserialization time
      callData.TimeElapsed = GetTimeSince(start);
      // Log
      // args: operationContext, clientName, urlTemplate, urlArgs, request, response, requestBody, responseBody, timeMs, exc 
      var timeMs = (int) callData.TimeElapsed.TotalMilliseconds;
      Settings.LogAction?.Invoke(this.AppContext, this.ClientName, callData.UrlTemplate, callData.UrlParameters,
                            callData.Request, callData.Response, callData.RequestBodyString, callData.ResponseBodyString,
                            timeMs, callData.Exception);
      Settings.Events.OnCompleted(this, callData);
      if (callData.Exception != null)
        throw callData.Exception;
      return (TResult)callData.ResponseBodyObject;
    }//method

    private async Task ReadResponseBodyAsync(RestCallData callData) {
      var content = callData.Response.Content;
      // check response body kind
      var returnValueKind = GetReturnValueKind(callData.ResponseBodyType);
      callData.ResponseBodyString = "(not set)";
      switch (returnValueKind) {
        case ReturnValueKind.None:
          return;
        case ReturnValueKind.HttpResponseMessage:
          callData.ResponseBodyObject = callData.Response;
          return;
        case ReturnValueKind.HttpContent:
          callData.ResponseBodyObject = content;
          return;
        case ReturnValueKind.Stream:
          callData.ResponseBodyObject = await content.ReadAsStreamAsync();
          return;
        case ReturnValueKind.HttpStatusCode:
          var status = callData.Response.StatusCode;
          callData.ResponseBodyObject = status;
          return;
        case ReturnValueKind.Object:
          // read as string and then deserialize
          callData.ResponseBodyString = await content.ReadAsStringAsync();
          if (!string.IsNullOrEmpty(callData.ResponseBodyString))
            callData.ResponseBodyObject = Settings.Serializer.Deserialize(callData.ResponseBodyType, callData.ResponseBodyString);
          return;
      }// switch
    }

    private void BuildHttpRequestContent(RestCallData request) {
      var body = request.RequestBodyObject;
      if (body == null)
        return;
      // = ApiClientUtil.GetRequestBodyKind(request.HttpMethod, request.RequestBodyType);
      if (typeof(HttpContent).IsAssignableFrom(request.RequestBodyType))
        request.Request.Content = (HttpContent)body;
      else if (typeof(Stream).IsAssignableFrom(request.RequestBodyType)) {
        var stream = (Stream)body;
        request.Request.Content = new StreamContent(stream);
      } else {
        var strContent = Settings.Serializer.Serialize(body);
        request.Request.Content = new StringContent(strContent, this.Settings.Encoding, GetRequestMediaType());
      }
    }

    private string GetRequestMediaType() {
      var serMediaType = Settings.Serializer.ContentTypes;
      if (!serMediaType.Contains(','))
        return serMediaType;
      // there are multiple media types, grab the first one
      return serMediaType.Split(',')[0];
    }

    #endregion

    #region private utilities

    public enum ReturnValueKind {
      None,
      Object,
      HttpResponseMessage,
      HttpStatusCode,
      HttpContent,
      Stream,
    }

    private static ReturnValueKind GetReturnValueKind(Type type) {
      if (type == typeof(DBNull))
        return ReturnValueKind.None;
      if (type == typeof(HttpResponseMessage))
        return ReturnValueKind.HttpResponseMessage;
      if (type == typeof(HttpStatusCode))
        return ReturnValueKind.HttpStatusCode;
      if (type == typeof(HttpContent))
        return ReturnValueKind.HttpContent;
      if (typeof(System.IO.Stream).IsAssignableFrom(type))
        return ReturnValueKind.Stream;
      return ReturnValueKind.Object;
    }

    private static long GetTimestamp() {
      return Stopwatch.GetTimestamp();
    }

    private static TimeSpan GetTimeSince(long start) {
      var now = Stopwatch.GetTimestamp();
      var time = TimeSpan.FromMilliseconds((now - start) * 1000 / Stopwatch.Frequency);
      return time;
    }



    #endregion
  }//class
}
