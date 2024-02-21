using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace Arrest.Internals {
  public static class RestUtility {

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
      var sArgs = args.Select(a => FormatForUrl(a)).ToArray(); //escape
      return string.Format(template, sArgs);
    }

    public static string BuildUrlQueryFromObject(object value) {
      if (value == null)
        return string.Empty;
      var segments = new List<string>();
      var type = value.GetType();
      var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty;
      var members = type.GetMembers(flags);
      foreach(var member in members) {
        var pv = member.GetValue(value);
        if (pv == null)
          continue;
        var pvStr = FormatForUrl(pv);
        segments.Add($"{member.Name}={pvStr}");
      }
      return string.Join("&", segments);
    }

    public static string FormatForUrl(object value) {
      // convert using invariant culture
      var str = Convert.ToString(value, CultureInfo.InvariantCulture);
      return EscapeForUrl(str);
    }

    public static string EscapeForUrl(string value) {
      return string.IsNullOrEmpty(value) ? string.Empty : Uri.EscapeDataString(value);
    }

    private static object GetValue(this MemberInfo member, object instance) {
      switch (member) {
        case FieldInfo f:  return f.GetValue(instance);
        case PropertyInfo p: return p.GetValue(instance);
        default: return null; 
      }
    }

    internal static ReturnValueKind GetReturnValueKind(Type type) {
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

    internal static long GetTimestamp() {
      return Stopwatch.GetTimestamp();
    }

    internal static TimeSpan GetTimeSince(long start) {
      var now = Stopwatch.GetTimestamp();
      var time = TimeSpan.FromMilliseconds((now - start) * 1000 / Stopwatch.Frequency);
      return time;
    }



  }
}
