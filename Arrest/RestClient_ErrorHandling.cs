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
      string details = null;
      try {
        details = await callData.Response.Content.ReadAsStringAsync();
      } catch(Exception exc) {
        details = "(no details returned)";
      }
      if (callData.Response.StatusCode == HttpStatusCode.BadRequest)
        return new BadRequestException(details);
      else 
        return new RestException(details, callData.Response.StatusCode);
    }

  }//class
}//ns
