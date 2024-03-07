using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Arrest {
  public class RetryPolicy {
    public double[] RetryIntervalsSec;
    public HttpStatusCode[] RetryHttpStatusCodes = new HttpStatusCode[] {
      HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout,
      HttpStatusCode.RequestTimeout, HttpStatusCode.Conflict, HttpStatusCode.ExpectationFailed, 
    };

    public RetryPolicy(params double[] retryIntervals) {
      RetryIntervalsSec = retryIntervals ?? new double[] { 1, 5, 30, 60 };
    }

    public bool ShouldRetry(HttpStatusCode status) {
      return RetryHttpStatusCodes != null && RetryHttpStatusCodes.Contains(status); 
    }
  }
}
