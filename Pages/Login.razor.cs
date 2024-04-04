using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;
using WebServerManager.Components.Authorize;

namespace WebServerManager.Pages;

partial class Login
{
	private readonly static EventId EventId = new(114511, "UserLogIO");

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
		if (Utils.CheckLogin(this.HttpContextAccessor))
			this.NavigationManager.NavigateTo("/");
		base.OnInitialized();
	}
	public async void LoginEnter()
	{
		if (!Manager.Users.TryGetValue(this.Username, out var user))
			goto Failed;

		if (user != HashChecker.GetHash(this.Password))
			goto Failed;

		string token = HashChecker.GenerateHash();
		Manager.ActiveTokens[this.Username] = token;
		this.RemoveTokenTimeout(this.Username, this.ExpiresInMin * 60 * 1000);

		await this.JS!.InvokeVoidAsync("WriteCookie", "username", this.Username, this.ExpiresInMin * 60);
		await this.JS!.InvokeVoidAsync("WriteCookie", "token", token, this.ExpiresInMin * 60);

		this.Logger.LogInformation(EventId, "The user {username} logged in, token expires in {minute} minutes.", this.Username, this.ExpiresInMin);

		this.NavigationManager.NavigateTo("/", true);

		return;
	Failed:
		this.Message = "Incorrect Password/Username";
		this.StateHasChanged();
		return;
	}
	private async void RemoveTokenTimeout(string key, int timeoutMs)
	{
		await Task.Delay(timeoutMs);
		this.Logger.LogInformation(EventId, "Revoked user token for {key}.", key);
		Manager.ActiveTokens.Remove(key);
	}
}
