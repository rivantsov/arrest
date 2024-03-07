using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Arrest.Internals;

namespace Arrest {

  public class RestClientSettings {
    /// <summary>Identifier of the client, written to log, to easier identify entries.</summary> 
    public string ClientName;
    public string ServiceUrl;
    public readonly IContentSerializer Serializer;
    public Encoding Encoding = Encoding.UTF8;
    public JsonSerializerOptions JsonOptions;
    public RetryPolicy RetryPolicy;
    public int TimeoutSec;

    // Some APIs follow this draft for errors in REST APIs: https://tools.ietf.org/html/draft-nottingham-http-problem-07
    // So returned error is JSon but content type is problem+json
    public string AcceptMediaTypes = "application/json, application/problem+json";
    public string DefaultSendMediaType = "application/json";


    public RestClientSettings(string serviceUrl, JsonSerializerOptions jsonOptions = null, IContentSerializer serializer = null,
                                   RetryPolicy retryPolicy = null,  int timeoutSec = 30) {
      if(serviceUrl != null && serviceUrl.EndsWith("/"))
        serviceUrl = serviceUrl.Substring(0, serviceUrl.Length - 1);
      ServiceUrl = serviceUrl;
      JsonOptions = jsonOptions ?? RestClient.DefaultJsonOptions;
      Serializer = serializer ?? new JsonContentSerializer(this.JsonOptions);
      RetryPolicy = retryPolicy; //do not create default; no retries by default
      TimeoutSec = timeoutSec; 
    }

  }//class
}
