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
    public HttpRequestHeaders DefaultRequestHeaders => HttpClient.DefaultRequestHeaders;
    public readonly RestClientEvents Events = new RestClientEvents();

    #region constructors

    public RestClient(string baseUrl, HttpClient httpClient = null)
      : this(new RestClientSettings(baseUrl), httpClient) { }

    public RestClient(RestClientSettings settings, HttpClient httpClient = null) {
      RestClientSettings.Validate(settings); 
      Settings = settings;

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
      var content = await SendAsyncImpl<object, HttpContent>(HttpMethod.Get, url, args, 
                            null, acceptMediaType: acceptMediaType);
      var result = await content.ReadAsStringAsync();
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
      var url = RestUtility.FormatUrl(template, args);
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
      return RestUtility.BuildUrlQueryFromObject(queryParams); 
    }

    #endregion

    //For staging sites, to allow using https with self-issued certificates
    public static void SetAllowSelfIssuedCertificates() {
      ServicePointManager.ServerCertificateValidationCallback = (x1, x2, x3, x4) => true;
    }


    #region private methods

    private async Task<TResult> SendAsyncImpl<TBody, TResult>(HttpMethod method, string urlTemplate, object[] urlParameters,
                        TBody body, string acceptMediaType = null, CancellationToken token = default) {
      var start = RestUtility.GetTimestamp();
      var callData = new RestCallData() {
        StartedAtUtc = RestUtility.GetUtc(),
        HttpMethod = method,
        UrlTemplate = urlTemplate,
        UrlParameters = urlParameters,
        Url = FormatUrl(urlTemplate, urlParameters),
        RequestBodyType = typeof(TBody),
        ResponseBodyType = typeof(TResult),
        ReturnValueKind = RestUtility.GetReturnValueKind(typeof(TResult)),
        RequestBodyObject = body,
      };

      // Create RequestMessage, setup headers, serialize body
      callData.Request = new HttpRequestMessage(callData.HttpMethod, callData.Url);
      var headers = callData.Request.Headers;
      headers.Add("accept", acceptMediaType ?? this.Settings.AcceptContentTypes);
      foreach (var kv in this.DefaultRequestHeaders)
        headers.Add(kv.Key, kv.Value);
      BuildHttpRequestContent(callData);

      this.Events.OnSendingRequest(this, callData);

      //actually make a call
      callData.Response = await HttpClient.SendAsync(callData.Request, token);
      callData.TimeElapsed = RestUtility.GetTimeSince(start); //measure time in case we are about to cancel and throw

      //check error
      if (callData.Response.IsSuccessStatusCode) {
        this.Events.OnReceivedResponse(this, callData);
        await ReadResponseBodyAsync(callData);
      } else {
        callData.Exception = await this.ReadErrorResponseAsync(callData);
        this.Events.OnReceivedError(this, callData);
      }
      // get time again to include deserialization time
      callData.TimeElapsed = RestUtility.GetTimeSince(start);
      // Log
      // args: operationContext, clientName, urlTemplate, urlArgs, request, response, requestBody, responseBody, timeMs, exc 
      var timeMs = (int) callData.TimeElapsed.TotalMilliseconds;
      this.Events.OnCompleted(this, callData);
      if (callData.Exception != null)
        throw callData.Exception;
      return (TResult)callData.ResponseBodyObject;
    }//method

    private async Task ReadResponseBodyAsync(RestCallData callData) {
      var content = callData.Response.Content;
      // check response body kind
      callData.ResponseBodyString = "(not set)";
      switch (callData.ReturnValueKind) {
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
            callData.ResponseBodyObject = Settings.Serializer.Deserialize(
                 callData.ResponseBodyString, callData.ResponseBodyType);
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
        var json = Settings.Serializer.Serialize(body);
        request.Request.Content = new StringContent(json, this.Settings.Encoding, this.Settings.OutputContentType);
      }
    }

    internal virtual async Task<Exception> ReadErrorResponseAsync(RestCallData callData) {
      string details = null;
      try {
        details = await callData.Response.Content.ReadAsStringAsync();
      } catch (Exception exc) {
        details = $"(failed to read content: {exc.Message} )";
      }
      if (callData.Response.StatusCode == HttpStatusCode.BadRequest)
        return new BadRequestException(details);
      else
        return new RestException(details, callData.Response.StatusCode);
    }


    #endregion

  }//class
}
