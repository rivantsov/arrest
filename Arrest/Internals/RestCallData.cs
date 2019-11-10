using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Arrest.Internals {

  public class RestCallData {
    public HttpMethod HttpMethod;
    public string UrlTemplate;
    public object[] UrlParameters;
    public string Url; 

    public Type RequestBodyType;
    public object RequestBodyObject;
    public string RequestBodyString;
    public string AcceptMediaType;

    public Type ResponseBodyType;
    public object ResponseBodyObject;
    public string ResponseBodyString;

    public DateTime StartedAtUtc;
    public TimeSpan TimeElapsed;
    public Exception Exception;

    public HttpRequestMessage Request;
    public HttpResponseMessage Response;
  }
}
