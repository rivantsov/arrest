﻿using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Arrest.Internals;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace Arrest {
  public partial class RestClient {
    public static JsonSerializerOptions DefaultJsonOptions;

    public readonly RestClientSettings Settings;
    public readonly HttpClient HttpClient;
    public HttpRequestHeaders DefaultRequestHeaders => HttpClient.DefaultRequestHeaders;
    public readonly RestClientEvents Events = new RestClientEvents();

    #region static constructor
    static RestClient() {
      DefaultJsonOptions = new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null, // remove camel-style policy
        IncludeFields = true,
      };
      DefaultJsonOptions.Converters.Add(new JsonStringEnumConverter());
    }
    #endregion


    #region constructors

    public RestClient(string baseUrl, HttpClient httpClient = null, JsonSerializerOptions jsonOptions = null, RetryPolicy retryPolicy = null, int timeoutSec = 30)
      : this(new RestClientSettings(baseUrl, jsonOptions, retryPolicy: retryPolicy, timeoutSec: timeoutSec), httpClient) { }

    public RestClient(RestClientSettings settings, HttpClient httpClient = null) {
      Settings = settings;

      if (httpClient != null) {
        this.HttpClient = httpClient;
      } else {
        // create global singleton handler if not created yet
        SharedHttpClientHandler = SharedHttpClientHandler ?? new HttpClientHandler();
        HttpClient = new HttpClient(SharedHttpClientHandler);
      }
      HttpClient.Timeout = TimeSpan.FromSeconds(Settings.TimeoutSec);
    }

    #endregion

    #region Headers

    public void AddAuthorizationHeader(string headerValue, string scheme = "Bearer") {
      DefaultRequestHeaders.Authorization = null;
      DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, headerValue);
    }
    public void AddRequestHeader(string name, string value) {
      DefaultRequestHeaders.Add(name, value);
    }
    public void RemoveRequestHeader(string name) {
      if (DefaultRequestHeaders.Contains(name))
        DefaultRequestHeaders.Remove(name);
    }

    #endregion

    #region async get/post/put/delete methods 

    public Task<TResult> GetAsync<TResult>(string url, params object[] args) {
      return SendAsyncImpl<object, TResult>(HttpMethod.Get, url, args, null);
    }//method

    public Task<TResult> PostAsync<TContent, TResult>(TContent body, string url, params object[] args) {
      return SendAsyncImpl<TContent, TResult>(HttpMethod.Post, url, args, body);
    }

    public Task<TResult> PutAsync<TContent, TResult>(TContent body, string url, params object[] args) {
      return SendAsyncImpl<TContent, TResult>(HttpMethod.Put, url, args, body);
    }

    public Task<HttpStatusCode> DeleteAsync(string url, params object[] args) {
      return SendAsyncImpl<DBNull, HttpStatusCode>(HttpMethod.Delete, url, args, null);
    }

    public async Task<byte[]> GetBinaryAsync(string url, params object[] args) {
      var resultContent = await SendAsyncImpl<DBNull, HttpContent>(HttpMethod.Get, url, args, null, acceptMediaTypes: "application/octet-stream");
      var result = await resultContent.ReadAsByteArrayAsync();
      return result;
    }

    public async Task<string> GetStringAsync(string url, params object[] args) {
      var content = await SendAsyncImpl<object, HttpContent>(HttpMethod.Get, url, args,
                            null, acceptMediaTypes: "text/plain");
      var result = await content.ReadAsStringAsync();
      return result;
    }

    public async Task<TResult> SendAsync<TContent, TResult>(HttpMethod method, TContent body, string urlTemplate, object[] args,
             string acceptMediaType = null) {
      return await SendAsyncImpl<TContent, TResult>(method, urlTemplate, args, body, acceptMediaType);
    }
    #endregion

    #region Sync methods
    public TResult Get<TResult>(string url, params object[] args) {
      return SyncAsync.RunSync(() => this.GetAsync<TResult>(url, args));
    }//method

    public TResult Post<TContent, TResult>(TContent content, string url, params object[] args) {
      return SyncAsync.RunSync(() => this.PostAsync<TContent, TResult>(content, url, args));
    }

    public TResult Put<TContent, TResult>(TContent content, string url, params object[] args) {
      return SyncAsync.RunSync(() => this.PutAsync<TContent, TResult>(content, url, args));
    }

    public TResult Send<TContent, TResult>(HttpMethod method,
                                      TContent content, string url, object[] args, string acceptMediaType = null) {
      return SyncAsync.RunSync(() => this.SendAsync<TContent, TResult>(method, content, url, args, acceptMediaType));
    }

    public HttpStatusCode Delete(string url, params object[] args) {
      return SyncAsync.RunSync(() => this.DeleteAsync(url, args));
    }

    public byte[] GetBinary(string url, params object[] args) {
      return SyncAsync.RunSync(() => this.GetBinaryAsync(url, args));
    }

    public string GetString(string url, params object[] args) {
      return SyncAsync.RunSync(() => this.GetStringAsync(url, args));
    }
    #endregion

    /// <summary>
    /// Formats URL from a template with standard numeric placeholders, ex: {0}. All argument values 
    /// are URL-escaped. The returned URL is full URL, starting with base service address. 
    /// </summary>
    /// <param name="template">URL template, not including base service part.</param>
    /// <param name="args">Arguments to insert into the template.</param>
    /// <returns>Full formatted URL.</returns>
    public string GetFullUrl(string template, IList<object> args) {
      if (string.IsNullOrWhiteSpace(template))
        return Settings.ServiceUrl;
      var url = RestUtility.FormatUrl(template, args.ToArray());
      string fullUrl;
      //Check if template is abs URL
      if (url.StartsWith("http://") || template.StartsWith("https://"))
        fullUrl = template;
      else {
        var ch0 = url[0];
        var needDelim = ch0 != '/' && ch0 != '?';
        var delim = needDelim ? "/" : string.Empty;
        fullUrl = Settings.ServiceUrl + delim + url;
      }
      return fullUrl;
    }

    //For staging sites, to allow using https with self-issued certificates
    public static void SetAllowSelfIssuedCertificates() {
      ServicePointManager.ServerCertificateValidationCallback = (x1, x2, x3, x4) => true;
    }

  }
}
