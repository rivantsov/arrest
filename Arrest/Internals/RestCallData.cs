using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Arrest.Internals {

  public enum ReturnValueKind {
    None,
    Object,
    HttpResponseMessage,
    HttpStatusCode,
    HttpContent,
    Stream,
  }


  public class RestCallData {
    public HttpMethod HttpMethod;
    public string UrlTemplate;
    public object[] UrlParameters;
    public string Url; 

    public Type RequestBodyType;
    public object RequestBodyObject;
    public string RequestBodyString;

    public Type ResponseBodyType;
    public object ResponseBodyObject;
    public string ResponseBodyString;
    public ReturnValueKind ReturnValueKind;

    public DateTime StartedAtUtc;
    public TimeSpan TimeElapsed;
    public Exception Exception;

    public HttpRequestMessage Request;
    public HttpResponseMessage Response;

    internal RestCallData() { }
  }
}
