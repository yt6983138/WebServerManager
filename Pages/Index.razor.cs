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
	protected override void OnInitialized()
	{
		if (Utils.CheckLogin(HttpContextAccessor))
		{
			base.OnInitialized();
			return;
		}
		NavigationManager.NavigateTo("/Login");
	}
}
