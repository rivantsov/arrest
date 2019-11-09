using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Arrest {
  public partial class RestClient {

    public enum ReturnValueKind {
      None,
      Object,
      HttpResponseMessage,
      HttpStatusCode,
      HttpContent,
      Stream,
    }

    private static ReturnValueKind GetReturnValueKind(Type type) {
      if (type == typeof(DBNull))
        return ReturnValueKind.None;
      if (type == typeof(HttpResponseMessage))
        return ReturnValueKind.HttpResponseMessage;
      if (type == typeof(HttpStatusCode))
        return ReturnValueKind.HttpStatusCode;
      if (type == typeof(HttpContent))
        return ReturnValueKind.HttpContent;
      if (typeof(System.IO.Stream).IsAssignableFrom(type))
        return ReturnValueKind.Stream;
      return ReturnValueKind.Object;
    }

    private static string FormatUri(string template, params object[] args) {
      if (args == null || args.Length == 0)
        return template; 
      var sArgs = args.Select(a => EscapeForUri(a)).ToArray(); //escape
      return string.Format(template, sArgs);
    }

    private static string EscapeForUri(object value) {
      return value == null ? string.Empty : Uri.EscapeDataString(value.ToString());
    }

    private static long GetTimestamp() {
      return Stopwatch.GetTimestamp();
    }

    private static TimeSpan GetTimeSince(long start) {
      var now = Stopwatch.GetTimestamp();
      var time = TimeSpan.FromMilliseconds((now - start) * 1000 / Stopwatch.Frequency);
      return time;
    }



  }
}
