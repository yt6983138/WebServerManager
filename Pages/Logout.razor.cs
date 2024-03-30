﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WebServerManager.Pages;

partial class Logout
{

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
		if (Utils.CheckLogin(HttpContextAccessor))
		{
			string username = HttpContextAccessor.HttpContext!.Request.Cookies["username"]!;
			Manager.ActiveTokens.Remove(username);
			Logger.LogInformation("The user {username} logged out!", username);
		}

		NavigationManager.NavigateTo("/Login");
		base.OnInitialized();
	}
}
