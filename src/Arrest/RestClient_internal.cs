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
using System.Collections;

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


    private async Task<TResult> SendAsyncImpl<TBody, TResult>(HttpMethod method, string urlTemplate, object[] args,
                        TBody body, string acceptMediaTypes = null) {

      var callContext = InitCallData<TBody, TResult>(method, urlTemplate, args, body, acceptMediaTypes);
      var waitTimes = Settings?.RetryPolicy?.RetryIntervalsSec ?? new double[] { 0.0 }; // make sure there's at lease one element
      foreach(var waitTime in waitTimes) {
        await SendMessageAsync(callContext);
        if (!ShouldRetry(callContext))
          break;
        if (waitTime > 0) // waitTime should be > 0 here, but just in case
          await Task.Delay((int)waitTime * 1000); // seconds to ms
      }
      // post-call actions
      if (callContext.Exception != null)
        throw callContext.Exception;
      return (TResult)callContext.ResponseBodyObject;
    }

    private async Task SendMessageAsync(CallContext callContext) {
      callContext.Exception = null;
      callContext.Response = null; 
      callContext.TryCount++;
      callContext.StartTimestamp = RestUtility.GetTimestamp();
      BuildRequestMessage(callContext);

      this.Events.OnSendingRequest(this, callContext);
      callContext.Response = await HttpClient.SendAsync(callContext.Request, callContext.CancellationToken);
      this.Events.OnCompleted(this, callContext);

      callContext.TimeElapsed = RestUtility.GetTimeSince(callContext.StartTimestamp);
      //check error
      if (callContext.Response.IsSuccessStatusCode) {
        this.Events.OnReceivedResponse(this, callContext);
        await ReadResponseBodyAsync(callContext);
      } else {
        callContext.Exception = await this.ReadErrorResponseAsync(callContext);
        this.Events.OnReceivedError(this, callContext);
      }
      // get time again to include deserialization time
      callContext.TimeElapsed = RestUtility.GetTimeSince(callContext.StartTimestamp);
    }

    private CallContext InitCallData<TBody, TResult>(HttpMethod method, string urlTemplate, object[] args,
                        TBody body, string acceptMediaTypes = null) {
      var callContext = new CallContext() {
        StartedAtUtc = RestUtility.GetUtc(),
        StartTimestamp = RestUtility.GetTimestamp(),
        HttpMethod = method,
        UrlTemplate = urlTemplate,
        Url = urlTemplate, // default, in case no args
        AcceptMedaTypes = acceptMediaTypes ?? this.Settings.AcceptMediaTypes,
        RequestBodyType = typeof(TBody),
        ResponseBodyType = typeof(TResult),
        ReturnValueKind = GetReturnValueKind(typeof(TResult)),
        RequestBodyObject = body,
      };
      // Preprocess args - separate UrlTemplate args from otheres: headers, cancelToken, ArgContextBox
      PreprocessArgs(callContext, args);
      callContext.Url = GetFullUrl(callContext.UrlTemplate, callContext.UrlParameters);
      BuildHttpRequestContent(callContext);
      return callContext;
    }

    private void PreprocessArgs(CallContext callContext, object[] args) {
      if (args == null || args.Length == 0)
        return;
      foreach(var arg in args) {
        switch(arg) {
          case null:
            callContext.UrlParameters.Add(null);
            break;
          case CancellationToken tkn:
            callContext.CancellationToken = tkn;
            break;
          case ArgAcceptMediaType mt:
            callContext.AcceptMedaTypes = mt.MediaType;
            break;
          case ArgContextBox rb:
            callContext.ResponseBox = rb;
            rb.CallContext = callContext; 
            break;
          case ValueTuple<string, string> hdr:
            callContext.DynamicHeaders.Add(hdr);
            break; 
          default:
            callContext.UrlParameters.Add(arg);
            break; 
        }
      }
    }

    private void BuildRequestMessage(CallContext callContext) {
      // Create RequestMessage, setup headers
      callContext.Request = new HttpRequestMessage(callContext.HttpMethod, callContext.Url);
      callContext.Request.Content = callContext.RequestContent;
      var headers = callContext.Request.Headers;
      headers.Add("accept", callContext.AcceptMedaTypes);
      foreach (var kv in this.DefaultRequestHeaders)
        headers.Add(kv.Key, kv.Value);
      // dynamic headers
      foreach (var tpl in callContext.DynamicHeaders)
        headers.Add(tpl.Item1, tpl.Item2);
    }


    private async Task ReadResponseBodyAsync(CallContext callContext) {
      var content = callContext.Response.Content;
      // check response body kind
      callContext.ResponseBodyString = "(not set)";
      switch (callContext.ReturnValueKind) {
        case ReturnValueKind.None:
          return;
        case ReturnValueKind.HttpResponseMessage:
          callContext.ResponseBodyObject = callContext.Response;
          return;
        case ReturnValueKind.HttpContent:
          callContext.ResponseBodyObject = content;
          return;
        case ReturnValueKind.Stream:
          callContext.ResponseBodyObject = await content.ReadAsStreamAsync();
          return;
        case ReturnValueKind.HttpStatusCode:
          var status = callContext.Response.StatusCode;
          callContext.ResponseBodyObject = status;
          return;
        case ReturnValueKind.Object:
          // read as string and then deserialize
          callContext.ResponseBodyString = await content.ReadAsStringAsync();
          if (!string.IsNullOrEmpty(callContext.ResponseBodyString))
            callContext.ResponseBodyObject = Settings.Serializer.Deserialize(
                 callContext.ResponseBodyString, callContext.ResponseBodyType);
          return;
      }// switch
    }

    private void BuildHttpRequestContent(CallContext callContext) {
      var body = callContext.RequestBodyObject;
      if (body == null)
        return;
      // = ApiClientUtil.GetRequestBodyKind(callContext.HttpMethod, callContext.RequestBodyType);
      if (typeof(HttpContent).IsAssignableFrom(callContext.RequestBodyType))
        callContext.RequestContent = (HttpContent)body;
      else if (typeof(Stream).IsAssignableFrom(callContext.RequestBodyType)) {
        var stream = (Stream)body;
        callContext.RequestContent = new StreamContent(stream);
      } else {
        var json = Settings.Serializer.Serialize(body);
        callContext.RequestContent = new StringContent(json, this.Settings.Encoding, this.Settings.DefaultSendMediaType);
      }
    }

    internal virtual async Task<Exception> ReadErrorResponseAsync(CallContext callContext) {
      var status = callContext.Response.StatusCode;
      string body = null;
      try {
        body = await callContext.Response.Content.ReadAsStringAsync();
      } catch (Exception exc) {
        body = $"(failed to read content: {exc.Message} )";
      }
      if (string.IsNullOrEmpty(body))
        body = $"Server returned status code {(int)status} {status}, no details returned.";
      if (status == HttpStatusCode.BadRequest)
        return new BadRequestException(body);
      else
        return new RestException(body, callContext.Response.StatusCode);
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

    private bool ShouldRetry(CallContext callContext) {
      if (callContext.Exception == null || Settings.RetryPolicy == null ||
          callContext.CancellationToken.IsCancellationRequested || callContext.Response == null)
        return false;
      var httpStatus = callContext.Response.StatusCode;
      return this.Settings.RetryPolicy.ShouldRetry(httpStatus);
    }

  }//class
}
