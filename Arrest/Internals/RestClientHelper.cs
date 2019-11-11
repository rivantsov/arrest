using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Arrest.Internals {
  public static class RestClientHelper {

    // Replace with AppTime.UtcNow or any facility that provides testable environment. 
    public static Func<DateTime> GetUtc = () => DateTime.UtcNow;

    public static string FormatUri(string template, params object[] args) {
      if (args == null || args.Length == 0)
        return template; 
      var sArgs = args.Select(a => EscapeForUri(a)).ToArray(); //escape
      return string.Format(template, sArgs);
    }

    public static string EscapeForUri(object value) {
      return value == null ? string.Empty : Uri.EscapeDataString(value.ToString());
    }

    public static long GetTimestamp() {
      return Stopwatch.GetTimestamp();
    }

    public static TimeSpan GetTimeSince(long start) {
      var now = Stopwatch.GetTimestamp();
      var time = TimeSpan.FromMilliseconds((now - start) * 1000 / Stopwatch.Frequency);
      return time;
    }

    //For staging sites, to allow using https with self-issued certificates
    public static void SetAllowSelfIssuedCertificates() {
      ServicePointManager.ServerCertificateValidationCallback = (x1, x2, x3, x4) => true;
    }



  }
}
