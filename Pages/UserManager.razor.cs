using Microsoft.AspNetCore.Components;
using WebServerManager.Components;
using WebServerManager.Components.Authorize;

namespace WebServerManager.Pages;

public partial class UserManager
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
	private readonly static EventId EventId = new(114512, "UserManagent");
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
		if (Utils.CheckLogin(this.HttpContextAccessor))
		{
			base.OnInitialized();
			this.UserName = this.HttpContextAccessor.HttpContext!.Request.Cookies["username"]!;
			if (!Manager.SuperUsers.Contains(this.UserName))
				this.NavigationManager.NavigateTo("/Forbidden", true);
			return;
		}
		this.NavigationManager.NavigateTo("/Login");
	}

	#region Utils/Shared functions
	public void CloseSubWindow()
		=> this.CurrentSubWindow = WindowType.None;
	private void Refresh()
	{
		this.Selected = new();
		this.ValueSetter.Invoke(null, false);
		this.StateHasChanged();
	}
	private void RemoveOrAdd(bool condition, string selected)
	{
		if (condition)
			this.Selected.Add(selected);
		else
			this.Selected.Remove(selected);

		this.StateHasChanged();
	}
	private void ResetNewUsernameAndPassword()
	{
		this.NewUsername = "";
		this.NewPassword = "";
	}
	#endregion

	#region User operations
	public void AddUserSubmit()
	{
		this.CloseSubWindow();
		if (Manager.Users.ContainsKey(this.NewUsername))
		{
			this.Message = $"The user \"{this.NewUsername}\" already exists!";
			return;
		}
		string passwordHash = HashChecker.GetHash(this.NewPassword);
		Manager.Users.Add(this.NewUsername, passwordHash);
		Manager.WriteUsers();
		this.Message = OperationDone;
		this.Logger.LogInformation(EventId, "The user {username} added new user {anotherUser} with password {password}!", this.UserName, this.NewUsername, this.NewPassword);
		this.ResetNewUsernameAndPassword();
		this.Refresh();
	}
	public void RemoveUsers()
	{
		foreach (string username in this.Selected)
		{
			Manager.Users.Remove(username);
			Manager.SuperUsers.Remove(username);
		}
		Manager.WriteUsers();
		this.Message = OperationDone;
		this.Logger.LogInformation(EventId, "User {username} removed user(s): [{users}].", this.UserName, string.Join(", ", this.Selected));
		this.Refresh();
	}
	public void MarkUsersAsSuperUser()
	{
		foreach (string username in this.Selected)
		{
			Manager.SuperUsers.TryAdd(username);
		}
		Manager.WriteUsers();
		this.Message = OperationDone;
		this.Logger.LogInformation(EventId, "User {username} marked user(s) as super user: [{users}].", this.UserName, string.Join(", ", this.Selected));
		this.Refresh();
	}
	public void MarkUsersAsNormalUser()
	{
		foreach (string username in this.Selected)
		{
			Manager.SuperUsers.Remove(username);
		}
		Manager.WriteUsers();
		this.Message = OperationDone;
		this.Logger.LogInformation(EventId, "User {username} marked user(s) as normal user: [{users}].", this.UserName, string.Join(", ", this.Selected));
		this.Refresh();
	}
	public void ChangePasswordForUser()
	{
		this.CloseSubWindow();
		Manager.Users[this.SubWindowSingleUserSelection!] = HashChecker.GetHash(this.NewPassword);
		this.SubWindowSingleUserSelection = null;
		Manager.WriteUsers();
		this.Message = OperationDone;
		this.Logger.LogInformation(EventId, "User {username} changed password to {password} for user {newUser}.", this.UserName, this.NewPassword, this.NewUsername);
		this.ResetNewUsernameAndPassword();
		this.Refresh();
	}
	public void ChangeUsernameForUser()
	{
		this.CloseSubWindow();
		if (Manager.Users.ContainsKey(this.NewUsername))
		{
			this.Message = "The username you tried to change already exists!";
			return;
		}
		string password = Manager.Users[this.SubWindowSingleUserSelection!];
		Manager.Users.Remove(this.SubWindowSingleUserSelection!);
		Manager.Users[this.NewUsername] = password;
		Manager.WriteUsers();
		this.Message = OperationDone;
		this.Logger.LogInformation(EventId, "User {username} changed username to {newUsername} for user {oldUsername}.", this.UserName, this.NewUsername, this.SubWindowSingleUserSelection);
		this.SubWindowSingleUserSelection = null;
		this.ResetNewUsernameAndPassword();
		this.Refresh();
	}
	#endregion
}
