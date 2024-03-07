using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Arrest.Internals;

namespace Arrest {
  /// <summary>Used for returning response message in one of the arguments of GetAsync, PostAsync etc calls.</summary>
  /// <remarks>Pass an instance in one of the arguments and after the call it will contain
  /// HttpResponseMessage. As an example, you can use it to retrieve a response header.</remarks>
  public class ArgContextBox {
    public HttpResponseMessage ResponseMessage => CallContext.Response;
    public CallContext CallContext { get; internal set; }
    
    public IEnumerable<string> GetResponseHeader(string name) {
      if (ResponseMessage.Headers.TryGetValues(name, out var values))
        return values;
      return null; 
    }
  }

  /// <summary>
  /// Use this type to pass accept media type(s) as an argument to GET/POST etc calls. 
  /// </summary>
  public struct ArgAcceptMediaType {
    public readonly string MediaType;
    public ArgAcceptMediaType(string type) {
      MediaType = type; 
    }
  }

}
