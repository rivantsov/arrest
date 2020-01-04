Notes about two ASP.NET (core) projects in SyncAsyncDeadlockTests folder

Arrest component includes SyncAsync.cs class implementing sync-to-async bridge. 
It is used in implementation of sync versions of methods like Get, Put which call 
async methods internally. 
SyncAsync implements a safe, reliable way to call async methods (HttpClient is all-async API)
from pure sync methods - which, it turns out, is not a trivial task. 
When running inside ASP.NET or ASP.NET-Core environment, it is easy to run into 
a thread deadlock (google it). Like everything works in console app (unit test project)
but deadlocks in a real ASP.NET (core) app. 
Microsoft did NOT provide a read-to-use solution for sync-async calls at this time, 
so we had to figure out something.  
These 2 test projects demonstrate/verify that the sync/async bridge works correctly, 
and does not result in deadlocks. Just make each project a Startup project and launch it 
(F5) from Visual Studio. It should show a web page with 'success - no deadlock' message. 
You can also look inside the SyncAsyncTestController.cs and try changing the implementation
to one of the commented out methods, and see that the result is a thread deadlock. 