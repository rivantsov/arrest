using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arrest.Internals;
using Arrest.Json;

namespace Arrest {
  // args: operationContext, clientName, urlTemplate, urlArgs, callData, response, requestBody, responseBody, timeMs, exc 
  using RestClientLogAction = Action<object, string, string, object[], HttpRequestMessage, HttpResponseMessage, object, object, int, Exception>;

  public delegate void ApiLogger(object context, string clientName, HttpRequestMessage callData, string requestBody,
           HttpResponseMessage response, string responseBody, int timeMs); 


  public class RestClientSettings {
    public string ServiceUrl;
    public readonly IContentSerializer Serializer;
    public string ExplicitAcceptList; //sometimes needed to explicitly add certain variations returned by server
    public Encoding Encoding = Encoding.UTF8;

    public readonly Type BadRequestContentType;
    public readonly Type ServerErrorContentType;

    public readonly RestClientEvents Events = new RestClientEvents(); 

    public RestClientLogAction LogAction;


    public RestClientSettings(string serviceUrl,
          IContentSerializer serializer = null, string explicitAcceptList = null, RestClientLogAction log = null,
          Type badRequestContentType = null, Type serverErrorContentType = null) {
      if(serviceUrl != null && serviceUrl.EndsWith("/"))
        serviceUrl = serviceUrl.Substring(0, serviceUrl.Length - 1);
      ServiceUrl = serviceUrl;
      Serializer = serializer ?? new JsonContentSerializer();
      ExplicitAcceptList = explicitAcceptList ?? Serializer.ContentTypes; 
      LogAction = log;
      ServerErrorContentType = serverErrorContentType ?? typeof(string);
      BadRequestContentType = badRequestContentType ?? ServerErrorContentType;
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
