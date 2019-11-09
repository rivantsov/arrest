using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Arrest {

  public class RestCallInfo {
    public HttpMethod HttpMethod;
    public string UrlTemplate;
    public object[] UrlParameters;
    public string Url; 

    public Type RequestBodyType;
    public object RequestBodyObject;
    public string RequestBodyString;
    public string AcceptMedaiType;

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
