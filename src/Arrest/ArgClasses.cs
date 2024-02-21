using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Arrest.Internals;

namespace Arrest {
  /// <summary>Used for returning response message in one of the arguments of GetAsync, PostAsync etc calls.</summary>
  /// <remarks>Pass an instance in one of the arguments and after the call it will contain
  /// HttpResponseMessage. As an example, you can use it to retrieve a response header.</remarks>
  public class ResponseBox {
    public HttpResponseMessage ResponseMessage => CallData.Response;
    public RestCallData CallData { get; internal set; }
    
    public IEnumerable<string> GetResponseHeader(string name) {
      if (ResponseMessage.Headers.TryGetValues(name, out var values))
        return values;
      return null; 
    }
  }

  public struct AcceptMediaType {
    public readonly string MediaType;
    public AcceptMediaType(string type) {
      MediaType = type; 
    }
  }

}
