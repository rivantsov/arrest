using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arrest.Internals;

namespace Arrest {
  // args: urlTemplate, urlArgs, response, requestBody, responseBody, timeMs, exc 
  using RestClientLogAction = Action<string, object[], HttpRequestMessage, HttpResponseMessage, object, object, int, Exception>;

  public delegate void ApiLogger(string clientName, HttpRequestMessage callData, string requestBody,
           HttpResponseMessage response, string responseBody, int timeMs); 


  public class RestClientSettings {
    public string ServiceUrl;
    public readonly IContentSerializer Serializer;
    public Encoding Encoding = Encoding.UTF8;

    // Some APIs follow this draft for errors in REST APIs: https://tools.ietf.org/html/draft-nottingham-http-problem-07
    // So returned error is JSon but content type is problem+json
    public string AcceptContentTypes = "application/json, application/problem+json";
    public string OutputContentType = "application/json";

    public readonly RestClientEvents Events = new RestClientEvents(); 
    public RestClientLogAction LogAction;


    public RestClientSettings(string serviceUrl, IContentSerializer serializer = null, RestClientLogAction log = null,
          Type badRequestContentType = null, Type serverErrorContentType = null) {
      if(serviceUrl != null && serviceUrl.EndsWith("/"))
        serviceUrl = serviceUrl.Substring(0, serviceUrl.Length - 1);
      ServiceUrl = serviceUrl;
      Serializer = serializer ?? new JsonContentSerializer();
      LogAction = log;
    }

    #region Validation
    internal static void Validate(RestClientSettings settings) {
      ThrowIf(settings == null, "Settings parameter may not be null.");
      ThrowIf(settings.Serializer == null, "Settings.Serializer property may not be null.");
      ThrowIf(string.IsNullOrWhiteSpace(settings.ServiceUrl), "ServiceUrl may not be empty.");
    }

    private static void ThrowIf(bool value, string message) {
      if (value)
        throw new Exception(message);
    }

    #endregion 
  }//class
}
