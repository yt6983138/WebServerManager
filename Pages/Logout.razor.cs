using Microsoft.AspNetCore.Components;

namespace WebServerManager.Pages;

public partial class Logout
{
	private readonly static EventId EventId = new(114511, "UserLogIO");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	private IHttpContextAccessor HttpContextAccessor { get; set; }
	[Inject]
	private NavigationManager NavigationManager { get; set; }
	[Inject]
	private ILogger<Logout> Logger { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	protected override void OnInitialized()
	{
		if (Utils.CheckLogin(this.HttpContextAccessor))
		{
			string username = this.HttpContextAccessor.HttpContext!.Request.Cookies["username"]!;
			Manager.ActiveTokens.Remove(username);
			this.Logger.LogInformation(EventId, "The user {username} logged out!", username);
		}

		this.NavigationManager.NavigateTo("/Login");
		base.OnInitialized();
	}
}
