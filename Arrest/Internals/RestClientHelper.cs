using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace Arrest.Internals {
  public static class RestClientHelper {

    // Replace with AppTime.UtcNow or any facility that provides testable environment. 
    public static Func<DateTime> GetUtc = () => DateTime.UtcNow;

    public static string FormatUrl(string template, params object[] args) {
      const string errorMessage = "Illegal character ':' in URL template. If you are trying to use " +
          "formatting options inside placeholders (ex: {0:hh:MM}) - this is not supported, " +
          "format each value explicitly before formatting the URL.";
      if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(template))
        return template;
      if (template.Contains(':'))
        throw new ArgumentException(errorMessage);
      var sArgs = args.Select(a => EscapeForUrl(a)).ToArray(); //escape
      return string.Format(template, sArgs);
    }

    public static string FormatAsUrlQuery(object value) {
      if (value == null)
        return string.Empty;
      var segments = new List<string>();
      var type = value.GetType();
      var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty;
      var members = type.GetMembers(flags);
      foreach(var member in members) {
        var pv = member.GetValue(value);
        if (pv != null)
          segments.Add($"{member.Name}={EscapeForUrl(pv)}");
      }
      return string.Join("&", segments);
    }

    private static object GetValue(this MemberInfo member, object instance) {
      switch (member) {
        case FieldInfo f:  return f.GetValue(instance);
        case PropertyInfo p: return p.GetValue(instance);
        default: return null; 
      }

    }

    public static string EscapeForUrl(object value) {
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
