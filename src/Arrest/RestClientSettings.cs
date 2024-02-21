using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arrest.Internals;

namespace Arrest {

  public class RestClientSettings {
    /// <summary>Identifier of the client, written to log, to easier identify entries.</summary> 
    public string ClientName;
    public string ServiceUrl;
    public readonly IContentSerializer Serializer;
    public Encoding Encoding = Encoding.UTF8;

    // Some APIs follow this draft for errors in REST APIs: https://tools.ietf.org/html/draft-nottingham-http-problem-07
    // So returned error is JSon but content type is problem+json
    public string AcceptMediaTypes = "application/json, application/problem+json";
    public string DefaultSendMediaType = "application/json";


    public RestClientSettings(string serviceUrl, IContentSerializer serializer = null) {
      if(serviceUrl != null && serviceUrl.EndsWith("/"))
        serviceUrl = serviceUrl.Substring(0, serviceUrl.Length - 1);
      ServiceUrl = serviceUrl;
      Serializer = serializer ?? new JsonContentSerializer();
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
