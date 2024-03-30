using Microsoft.AspNetCore.Components;
using WebServerManager.Components;
using WebServerManager.Components.Authorize;

namespace WebServerManager.Pages;

partial class UserManager
{
	#region Defintion
	public enum WindowType
	{
		None,
		AddUser,
		ChangePassword,
		ChangeUsername
	}
	private const string OperationDone = "The operation has done successfully.";
	#endregion

	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	private NavigationManager NavigationManager { get; set; }
	[Inject]
	private IHttpContextAccessor HttpContextAccessor { get; set; }
	[Inject]
	private ILogger<UserManager> Logger { get; set; }
	#endregion

	#region Infos
	public string UserName { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public string Message { get; set; } = "";
	#endregion

	#region Sub-window things
	public WindowType CurrentSubWindow { get; set; } = WindowType.None;
	public string? SubWindowSingleUserSelection { get; set; }
	public string NewUsername { get; set; } = "";
	public string NewPassword { get; set; } = "";
	#endregion

	#region Misc
	public List<string> Selected { get; set; } = new();
	public CheckBox.ValueSet ValueSetter { get; set; } = new();
	#endregion

	protected override void OnInitialized()
	{
		if (Utils.CheckLogin(HttpContextAccessor))
		{
			base.OnInitialized();
			UserName = HttpContextAccessor.HttpContext!.Request.Cookies["username"]!;
			if (!Manager.SuperUsers.Contains(UserName))
				NavigationManager.NavigateTo("/Forbidden", true);
			return;
		}
		NavigationManager.NavigateTo("/Login");
	}

	#region Utils/Shared functions
	public void CloseSubWindow()
		=> CurrentSubWindow = WindowType.None;
	private void Refresh()
	{
		Selected = new();
		ValueSetter.Invoke(null, false);
		StateHasChanged();
	}
	private void RemoveOrAdd(bool condition, string selected)
	{
		if (condition)
			Selected.Add(selected);
		else
			Selected.Remove(selected);

		StateHasChanged();
	}
	private void ResetNewUsernameAndPassword()
	{
		NewUsername = "";
		NewPassword = "";
	}
	#endregion

	#region User operations
	public void AddUserSubmit()
	{
		CloseSubWindow();
		if (Manager.Users.ContainsKey(NewUsername))
		{
			Message = $"The user \"{NewUsername}\" already exists!";
			return;
		}
		string passwordHash = HashChecker.GetHash(NewPassword);
		Manager.Users.Add(NewUsername, passwordHash);
		Manager.WriteUsers();
		Message = OperationDone;
		Logger.LogInformation("The user {username} added new user {anotherUser} with password {password}!", UserName, NewUsername, NewPassword);
		ResetNewUsernameAndPassword();
		Refresh();
	}
	public void RemoveUsers()
	{
		foreach (string username in Selected)
		{
			Manager.Users.Remove(username);
			Manager.SuperUsers.Remove(username);
		}
		Manager.WriteUsers();
		Message = OperationDone;
		Logger.LogInformation("User {username} removed user(s): [{users}].", UserName, string.Join(", ", Selected));
		Refresh();
	}
	public void MarkUsersAsSuperUser()
	{
		foreach (string username in Selected)
		{
			Manager.SuperUsers.TryAdd(username);
		}
		Manager.WriteUsers();
		Message = OperationDone;
		Logger.LogInformation("User {username} marked user(s) as super user: [{users}].", UserName, string.Join(", ", Selected));
		Refresh();
	}
	public void MarkUsersAsNormalUser()
	{
		foreach (string username in Selected)
		{
			Manager.SuperUsers.Remove(username);
		}
		Manager.WriteUsers();
		Message = OperationDone;
		Logger.LogInformation("User {username} marked user(s) as normal user: [{users}].", UserName, string.Join(", ", Selected));
		Refresh();
	}
	public void ChangePasswordForUser()
	{
		CloseSubWindow();
		Manager.Users[SubWindowSingleUserSelection!] = HashChecker.GetHash(NewPassword);
		SubWindowSingleUserSelection = null;
		Manager.WriteUsers();
		Message = OperationDone;
		Logger.LogInformation("User {username} changed password to {password} for user {newUser}.", UserName, NewPassword, NewUsername);
		ResetNewUsernameAndPassword();
		Refresh();
	}
	public void ChangeUsernameForUser()
	{
		CloseSubWindow();
		if (Manager.Users.ContainsKey(NewUsername))
		{
			Message = "The username you tried to change already exists!";
			return;
		}
		string password = Manager.Users[SubWindowSingleUserSelection!];
		Manager.Users.Remove(SubWindowSingleUserSelection!);
		Manager.Users[NewUsername] = password;
		Manager.WriteUsers();
		Message = OperationDone;
		Logger.LogInformation("User {username} changed username to {newUsername} for user {oldUsername}.", UserName, NewUsername, SubWindowSingleUserSelection);
		SubWindowSingleUserSelection = null;
		ResetNewUsernameAndPassword();
		Refresh();
	}
	#endregion
}
