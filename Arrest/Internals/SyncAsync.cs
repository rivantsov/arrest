using System;
using System.Threading;
using System.Threading.Tasks;

namespace Arrest.Internals {

  public static class SyncAsync {

    class AsyncJob<T> {
      public Func<Task<T>> JobFunc;
      public T Result;
      public ManualResetEventSlim CompletedSignal;
      public Exception Exception;
    }

    public static TResult RunSync<TResult>(Func<Task<TResult>> func) {
      var job = new AsyncJob<TResult>() { JobFunc = func, CompletedSignal = new ManualResetEventSlim(false) };
      var thread = new Thread(ExecuteJob<TResult>);
      thread.Start(job);
      job.CompletedSignal.Wait();
      if (job.Exception != null)
        throw job.Exception;
      return job.Result;
    }

    public static void RunSync(Func<Task> func) {
      Func<Task<int>> funcX = async () => { await func(); return 0; };
      RunSync(funcX); 
    }

    private static void ExecuteJob<TResult>(object job) {
      var jobT = (AsyncJob<TResult>)job;
      try {
        jobT.Result = jobT.JobFunc().Result;
      } catch(AggregateException aex) { 
        //unwrap aggregate exc; there should be just one inside
        jobT.Exception = aex.InnerExceptions[0];
      } catch (Exception ex) {
        jobT.Exception = ex;
      } finally {
        jobT.CompletedSignal.Set();
      }
    }
  }

}
