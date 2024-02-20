using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

      var builder = WebApplication.CreateBuilder();
      builder.Services.AddControllers()
                   .PartManager.ApplicationParts.Add(new AssemblyPart(typeof(TestDataController).Assembly));
      builder.WebHost.UseUrls(ServiceUrl);
      var app = builder.Build();
      app.MapControllers();
      app.StartAsync().Wait();
    }
  }
}
