using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Runtime.InteropServices;
using System.Text;

namespace WebServerManager.Pages;

public partial class LogRecord
{
	#region Definition
	public const int OneLoadLineCount = 50;
	#endregion

	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	private IHttpContextAccessor HttpContextAccessor { get; set; }
	[Inject]
	private NavigationManager NavigationManager { get; set; }
	[Inject]
	private IJSRuntime JS { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	#region Properties
	public int LoadCount { get; set; } = 1;
	public string LastMessage { get; set; } = "";
	#endregion

	protected override void OnInitialized()
	{
		if (Utils.CheckLogin(this.HttpContextAccessor))
		{
			base.OnInitialized();
			string username = this.HttpContextAccessor.HttpContext!.Request.Cookies["username"]!;
			if (!Manager.SuperUsers.Contains(username))
				this.NavigationManager.NavigateTo("/Forbidden", true);
			return;
		}
		this.NavigationManager.NavigateTo("/Login");
	}

	#region Log operations
	public void LoadMore()
	{
		this.LoadCount++;
		this.StateHasChanged();
	}
	// private readonly static byte[] NextLine = new byte[1] { 0x0A };
	public async void ExportAll()
	{
		this.LastMessage = "Preparing file...";
		this.StateHasChanged();

		string TempLocation = "/tmp";
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) TempLocation = "%temp%";
		string outLocation = Path.Combine(Environment.ExpandEnvironmentVariables(TempLocation), Path.GetRandomFileName() + ".log");

		using FileStream output = File.Create(outLocation);
		foreach (string data in yt6983138.Common.Logger.AllLogs)
		{
			output.Write(Encoding.UTF8.GetBytes(data));
			// output.Write(NextLine);
		}

		string pathToGo = $"/api/DownloadFile?address={System.Net.WebUtility.UrlEncode(outLocation)}";
		await this.JS.InvokeVoidAsync("openWindow", pathToGo);
		this.LastMessage = "File downloaded.";
		this.StateHasChanged();
	}
	#endregion
}
