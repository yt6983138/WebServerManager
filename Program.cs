using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using WebServerManager;
using WebServerManager.Components.Circuits;
using yt6983138.Common;

internal class Program
{
	private static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		builder.Logging.ClearProviders();
		// builder.Logging.AddConsole();

		builder.Logging.AddConfiguration();

		builder.Logging.Services.TryAddEnumerable(
			ServiceDescriptor.Singleton<ILoggerProvider, LoggerProvider>());
		LoggerProviderOptions.RegisterProviderOptions
			<LoggerConfiguration, LoggerProvider>(builder.Logging.Services);


		builder.Services.AddRazorPages();
		builder.Services.AddServerSideBlazor();
		builder.Services.AddHttpContextAccessor();
		builder.Services.AddMvc(options => options.EnableEndpointRouting = false);
		builder.Services.AddScoped<ICircuitAccessor, CircuitAccessor>();
		builder.Services.AddScoped<CircuitHandler, TrackingCircuitHandler>();
		//builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);


		var app = builder.Build();

		if (!app.Environment.IsDevelopment())
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();

		app.MapControllers().AllowAnonymous();

		app.UseHttpsRedirection();

		app.UseStaticFiles();

		app.UseRouting();

		app.UseMvcWithDefaultRoute();

		app.MapBlazorHub();
		app.MapFallbackToPage("/_Host");

		app.Run();
	}
}