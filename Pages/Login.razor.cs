using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;
using WebServerManager.Components.Authorize;

namespace WebServerManager.Pages;

partial class Login
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	private IHttpContextAccessor HttpContextAccessor { get; set; }
	[Inject]
	private NavigationManager NavigationManager { get; set; }
	[Inject]
	private IJSRuntime JS { get; set; }
	[Inject]
	private ILogger<Login> Logger { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public string Username { get; set; } = "";
	public string Password { get; set; } = "";
	public string Message { get; set; } = "";
	public int ExpiresInMin { get; set; } = 60 * 24 * 7;

	protected override void OnInitialized()
	{
		if (Utils.CheckLogin(HttpContextAccessor))
			NavigationManager.NavigateTo("/");
		base.OnInitialized();
	}
	public async void LoginEnter()
	{
		if (!Manager.Users.TryGetValue(Username, out var user))
			goto Failed;

		if (user != HashChecker.GetHash(Password))
			goto Failed;

		string token = HashChecker.GenerateHash();
		Manager.ActiveTokens[Username] = token;
		RemoveTokenTimeout(Username, ExpiresInMin * 60 * 1000);

		await JS!.InvokeVoidAsync("WriteCookie", "username", Username, ExpiresInMin * 60);
		await JS!.InvokeVoidAsync("WriteCookie", "token", token, ExpiresInMin * 60);

		Logger.LogInformation("The user {username} logged in, token expires in {minute} minutes.", Username, ExpiresInMin);

		NavigationManager.NavigateTo("/", true);

		return;
	Failed:
		Message = "Incorrect Password/Username";
		StateHasChanged();
		return;
	}
	private async void RemoveTokenTimeout(string key, int timeoutMs)
	{
		await Task.Delay(timeoutMs);
		Logger.LogInformation("Revoked user token for {key}.", key);
		Manager.ActiveTokens.Remove(key);
	}
}