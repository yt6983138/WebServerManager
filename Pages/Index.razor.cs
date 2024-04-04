using Microsoft.AspNetCore.Components;

namespace WebServerManager.Pages;

public partial class Index
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	private NavigationManager NavigationManager { get; set; }
	[Inject]
	private IHttpContextAccessor HttpContextAccessor { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public bool IsSuperUser { get; set; } = false;

	private static IReadOnlyDictionary<string, string> NormalTools { get; } = new Dictionary<string, string>()
	{
		{ "Logout", "/Logout" },
		{ "File Explorer", "/FileExplorer" }
	};
	private static IReadOnlyDictionary<string, string> SuperTools { get; } = new Dictionary<string, string>()
	{
		{ "Log Record", "/LogRecord" },
		{ "User Manager", "/UserManager" },
		{ "Terminal", "/Terminal" }
	};
	protected override void OnInitialized()
	{
		if (Utils.CheckLogin(this.HttpContextAccessor))
		{
			base.OnInitialized();
			if (Manager.SuperUsers.Contains(this.HttpContextAccessor.HttpContext!.Request.Cookies["username"]!))
				this.IsSuperUser = true;
			return;
		}
		this.NavigationManager.NavigateTo("/Login");
	}
}
