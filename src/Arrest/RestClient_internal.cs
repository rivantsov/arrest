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

    #region SharedHttpClientHandler
    // Note on multi-threading, and reuse of HttpClient: Async methods are thread-safe, see Remarks section here: 
    // https://msdn.microsoft.com/en-us/library/system.net.http.httpclient(v=vs.110).aspx
    // Turns out you MUST use a global singleton of  HttpClient: 
    //   http://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
    //   https://www.infoq.com/news/2016/09/HttpClient
    // We create singleton of HttpClientHandler which actually handles all HTTP interactions. 
    public static HttpClientHandler SharedHttpClientHandler { get; private set; }

    #endregion 


    #region  URL formatting helpers
    /// <summary>
    /// Formats URL from a template with standard numeric placeholders, ex: {0}. All argument values 
    /// are URL-escaped. The returned URL is full URL, starting with base service address. 
    /// </summary>
    /// <param name="template">URL template, not including base service part.</param>
    /// <param name="args">Arguments to insert into the template.</param>
    /// <returns>Full formatted URL.</returns>
    public string GetFullUrl(string template, params object[] args) {
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

    #endregion


    #region private methods

    private async Task<TResult> SendAsyncImpl<TBody, TResult>(HttpMethod method, string urlTemplate, object[] urlParameters,
                        TBody body, string acceptMediaType = null, CancellationToken token = default) {
      var start = RestUtility.GetTimestamp();
      var callData = new RestCallData() {
        StartedAtUtc = RestUtility.GetUtc(),
        HttpMethod = method,
        UrlTemplate = urlTemplate,
        UrlParameters = urlParameters,
        Url = GetFullUrl(urlTemplate, urlParameters),
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
