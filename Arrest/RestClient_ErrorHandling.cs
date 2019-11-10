using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arrest.Internals;

namespace Arrest {


  public partial class RestClient {

    internal virtual async Task<Exception> ReadErrorResponseAsync(RestCallData callData) {
      try {
        var response = callData.Response;
        var hasBody = response.Content != null && response.Content.Headers.ContentLength > 0;
        if(!hasBody)
          return new RestException("Web API call failed, no details returned. HTTP Status: " + response.StatusCode,
               response.StatusCode);
        callData.ResponseBodyString = await response.Content.ReadAsStringAsync();
        switch (response.StatusCode) {
          case HttpStatusCode.BadRequest:
            if(Settings.BadRequestContentType == typeof(string))
              return await ReadErrorResponseUntypedAsync(response);
            var errObj = Settings.Serializer.Deserialize(Settings.BadRequestContentType, callData.ResponseBodyString);
            return new BadRequestException(errObj);
          default:
            if(Settings.ServerErrorContentType == typeof(string))
              return await ReadErrorResponseUntypedAsync(response);
            //deserialize custom object
            try {
              var serverErr = Settings.Serializer.Deserialize(Settings.ServerErrorContentType, callData.ResponseBodyString);
              return new RestException("Server error: " + callData.ResponseBodyString, response.StatusCode, serverErr);
            } catch(Exception ex) {
              var remoteErr = callData.ResponseBodyString;
              var msg = $"Server error: {remoteErr}. RestClient: failed to deserialize response into error object, exc: {ex}";
              return new RestException(msg, response.StatusCode, remoteErr);
            }
        }//switch 
      } catch(Exception exc) {
        Type errorType = callData.Response.StatusCode == HttpStatusCode.BadRequest ? 
                             Settings.BadRequestContentType : Settings.ServerErrorContentType;
        var explain = $@"Failed to read error response returned from the service.
Expected content type: {errorType}. Consider changing it to match the error response for remote service. 
Deserializer error: {exc.Message}";
        throw new Exception(explain, exc);
      }
    }

    public async Task<RestException> ReadErrorResponseUntypedAsync(HttpResponseMessage response) {
      var content = await response.Content.ReadAsStringAsync();
      string message, details; //if multiline, split
      SplitErrorMessage(content, out message, out details);
      return new RestException(message, response.StatusCode, details);
    }

    private void SplitErrorMessage(string message, out string firstLine, out string others) {
      firstLine = message;
      others = null;
      if(string.IsNullOrWhiteSpace(message))
        return;
      var nlPos = message.IndexOf('\n');
      if(nlPos < 0)
        return;
      firstLine = message.Substring(0, nlPos);
      others = message.Substring(nlPos + 1);
    }


  }//class
}//ns
