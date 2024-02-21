using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Arrest.Tests {
  internal static class TestEnv {
    public const string ServiceUrl = "https://localhost:8008";

    private static bool _initialized;

    public static void EnsureInitialized() {
      if (_initialized)
        return;
      _initialized = true;

      // Start REST service
      var builder = WebApplication.CreateBuilder();
      builder.Services.AddControllers()
           // System.Text.Json does not serialize fields by default, we need to enable it explicitly
           .AddJsonOptions(o => {
             o.JsonSerializerOptions.IncludeFields = true;
             o.JsonSerializerOptions.PropertyNamingPolicy = null; //disable name policy, prop names as-is
           })
           .PartManager.ApplicationParts.Add(new AssemblyPart(typeof(TestDataController).Assembly)); // to ensure it finds the controller
      builder.WebHost.UseUrls(ServiceUrl);
      var app = builder.Build();
      app.MapControllers();
      app.StartAsync().Wait();
    }
  }
}
