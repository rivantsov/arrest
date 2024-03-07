using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Arrest.Internals {
  using HeaderTuple = ValueTuple<string, string>;

  public enum ReturnValueKind {
    None,
    Object,
    HttpResponseMessage,
    HttpStatusCode,
    HttpContent,
    Stream,
  }


  public class CallContext {
    public int TryCount;

    public HttpMethod HttpMethod;
    public string UrlTemplate;
    public List<object> UrlParameters = new List<object>();
    public string Url; 

    public Type RequestBodyType;
    public object RequestBodyObject;
    public string RequestBodyString;
    public HttpContent RequestContent; 

    public Type ResponseBodyType;
    public object ResponseBodyObject;
    public string ResponseBodyString;
    public ReturnValueKind ReturnValueKind;

    public DateTime StartedAtUtc;
    public long StartTimestamp;
    public TimeSpan TimeElapsed;
    public Exception Exception;

    public HttpRequestMessage Request;
    public HttpResponseMessage Response;

    // unpacked from args
    public CancellationToken CancellationToken;
    public string AcceptMedaTypes;
    public List<HeaderTuple> DynamicHeaders = new List<HeaderTuple>();
    public ArgContextBox ResponseBox; 

    internal CallContext() { }
  }
}
