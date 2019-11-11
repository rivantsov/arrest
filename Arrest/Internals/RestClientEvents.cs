using System;
using System.Collections.Generic;
using System.Text;

namespace Arrest.Internals {

  public class RestClientEventArgs : EventArgs {
    public readonly RestCallData CallData;
    internal RestClientEventArgs(RestCallData callData) {
      CallData = callData;
    }
  }

  public class RestClientEvents {
    public event EventHandler<RestClientEventArgs> SendingRequest;
    public event EventHandler<RestClientEventArgs> ReceivedResponse;
    public event EventHandler<RestClientEventArgs> ReceivedError;
    public event EventHandler<RestClientEventArgs> CallCompleted;

    internal void OnSendingRequest(RestClient client, RestCallData callData) {
      SendingRequest?.Invoke(client, new RestClientEventArgs(callData));
    }

    internal void OnReceivedResponse(RestClient client, RestCallData callData) {
      ReceivedResponse?.Invoke(client, new RestClientEventArgs(callData));
    }

    internal void OnReceivedError(RestClient client, RestCallData callData) {
      ReceivedError?.Invoke(client, new RestClientEventArgs(callData));
    }
    internal void OnCompleted(RestClient client, RestCallData callData) {
      CallCompleted?.Invoke(client, new RestClientEventArgs(callData));
    }
  } //class
}
