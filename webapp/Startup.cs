using IotSensorWeb.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IotSensorWeb
{
  public class Startup
  {
    private IoTHubEventsReader _ioTHubEventsReader;

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddSignalR();

      services.AddCors(o =>
      {
        o.AddPolicy("Everything", p => 
        {
          p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
        });
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      _ioTHubEventsReader = new IoTHubEventsReader(app.ApplicationServices);

      app.UseFileServer();

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseCors("Everything");

      app.UseSignalR(routes =>
      {
        routes.MapHub<SensorHub>("/sensor");
      });

    }
  }
}
