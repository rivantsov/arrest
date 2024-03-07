using System;
using System.Collections.Generic;
using System.Text;
using Arrest.Internals;

namespace Arrest {

  public class RestClientEventArgs : EventArgs {
    public readonly CallContext CallData;
    internal RestClientEventArgs(CallContext callContext) {
      CallData = callContext;
    }
  }

  public class RestClientEvents {
    public event EventHandler<RestClientEventArgs> SendingRequest;
    public event EventHandler<RestClientEventArgs> ReceivedResponse;
    public event EventHandler<RestClientEventArgs> ReceivedError;
    public event EventHandler<RestClientEventArgs> CallCompleted;

    internal void OnSendingRequest(RestClient client, CallContext callContext) {
      SendingRequest?.Invoke(client, new RestClientEventArgs(callContext));
    }

    internal void OnReceivedResponse(RestClient client, CallContext callContext) {
      ReceivedResponse?.Invoke(client, new RestClientEventArgs(callContext));
    }

    internal void OnReceivedError(RestClient client, CallContext callContext) {
      ReceivedError?.Invoke(client, new RestClientEventArgs(callContext));
    }
    internal void OnCompleted(RestClient client, CallContext callContext) {
      CallCompleted?.Invoke(client, new RestClientEventArgs(callContext));
    }
  } //class
}
