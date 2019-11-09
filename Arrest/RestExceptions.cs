using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Arrest {

  public class RestException : Exception {
    public readonly HttpStatusCode Status;
    public object Details; 
    public RestException(string message, HttpStatusCode status, object details = null) : base(message) {
      Status = status;
      Details = details; 
    }
    public override string ToString() {
      return $"Status: {Status}, {Message}  Details: {Details}";
    }
  }

  public class BadRequestException : RestException {
    public BadRequestException( object details) 
      : base("BadRequest status returned by the server.", HttpStatusCode.BadRequest, details) {
    }
    public override string ToString() {
      return $"BadRequest, errors: {Details}";
    }
  }//class


}
