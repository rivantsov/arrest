using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Arrest.Internals;

namespace Arrest.Sync {

  public static class RestClientSyncExtensions {

    public static TResult Get<TResult>(this RestClient client, string url, params object[] args) {
      return SyncAsync.RunSync(() => client.GetAsync<TResult>(url, args));
    }//method

    public static TResult Post<TContent, TResult>(this RestClient client, TContent content, string url, params object[] args) {
      return SyncAsync.RunSync(() => client.PostAsync<TContent, TResult>(content, url, args));
    }

    public static TResult Put<TContent, TResult>(this RestClient client, TContent content, string url, params object[] args) {
      return SyncAsync.RunSync(() => client.PutAsync<TContent, TResult>(content, url, args));
    }

    public static TResult Send<TContent, TResult>(this RestClient client, HttpMethod method,
                                      TContent content, string url, object[] args, string acceptMediaType = null) {
      return SyncAsync.RunSync(() => client.SendAsync<TContent, TResult>(method, content, url, args, acceptMediaType));
    }

    public static HttpStatusCode Delete(this RestClient client, string url, params object[] args) {
      return SyncAsync.RunSync(() => client.DeleteAsync(url, args));
    }

    public static byte[] GetBinary(this RestClient client, string url, object[] args, string acceptMediaType = "application/octet-stream") {
      return SyncAsync.RunSync(() => client.GetBinaryAsync(url, args, acceptMediaType));
    }

    public static string GetString(this RestClient client, string url, object[] args, string acceptMediaType = "text/plain") {
      return SyncAsync.RunSync(() => client.GetStringAsync(url, args, acceptMediaType));
    }

  } //class
}
