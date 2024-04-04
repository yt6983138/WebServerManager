using XtermBlazor;
using Pty.Net;
using System.Text;
using System;
using Microsoft.AspNetCore.Components;
using WebServerManager.Components.Circuits;
using WebServerManager.Components;

namespace WebServerManager.Pages;

partial class Terminal
{
	private static readonly TerminalOptions Options = new()
	{
		CursorBlink = true,
		CursorStyle = CursorStyle.Bar,
		Columns = Manager.Config.TerminalColumnCount,
		Rows = Manager.Config.TerminalRowCount
	};

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public List<TerminalCollection> UserTerminalCollections { get; set; }
	public TerminalCollection CurrentCollection { get; set; }
	public Xterm XTerminal { get; set; }
	public IPtyConnection Connection { get; set; }
	public string TerminalName { get; set; }
	public string UserName { get; set; }
	[Inject]
	private ICircuitAccessor CircuitAccessor { get; set; }
	[Inject]
	private NavigationManager NavigationManager { get; set; }
	[Inject]
	private IHttpContextAccessor HttpContextAccessor { get; set; }
	[Inject]
	private ILogger<UserManager> Logger { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public bool Exited { get; set; } = false;
	protected override void OnInitialized() // this executes twice for some reason
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
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			bool getted = // getted = got old or created
				Manager.Connections.TryGetOrCreate(
				UserName,
				out var collections,
				() => new()
			);
			UserTerminalCollections = collections;
			TrackingCircuitHandler.CircuitDisconnected += OnExit;
			Task.Run(async () =>
			{
				await this.InvokeAsync(async () =>
				{
					await Task.Delay(100); // fuck this, the xterminal element can only be accessed after oninitialized done
					if (UserTerminalCollections.All(t => t.Occupied))
						UserTerminalCollections.Add(await CreateCollection($"Terminal{UserTerminalCollections.Count}"));
					await SwitchTerminal(UserTerminalCollections.Count - 1, false, getted);
					BackgroundTask();
					StateHasChanged();
				});
			});
		}
		await base.OnAfterRenderAsync(firstRender);
		return;
	}
	public async Task SwitchTerminal(int index, bool removeOldBind = true, bool isOldSession = true)
	{
		if (UserTerminalCollections.Count == 0 && index == 0)
			UserTerminalCollections.Add(await CreateCollection("Terminal0"));
		var collection = UserTerminalCollections[index];
		if (collection.Occupied)
			throw new InvalidOperationException("The target connection is occupied.");
		if (CurrentCollection is not null)
		{
			CurrentCollection.Occupied = false;
		}
		if (removeOldBind)
		{
			Connection.ProcessExited -= OnPtyExit;
		}
		collection.Occupied = true;
		Connection = collection.Connection;
		TerminalName = collection.Name;
		CurrentCollection = collection;
		Connection.ProcessExited += OnPtyExit;
		if (isOldSession)
		{
			await XTerminal.Clear();
			await XTerminal.WriteLine("\n[Recovered session, press enter to continue]");
		}
		else
		{
			Connection.ProcessExited += GeneralOnPtyExit;
		}
	}
	public async Task<TerminalCollection> CreateCollection(string name)
		=> new(false, await PtyProvider.SpawnAsync(
			new PtyOptions()
			{
				App = Manager.Config.TerminalExecutableName,
				Cwd = Manager.Config.UtilsDefaultStartPath,
				Cols = Manager.Config.TerminalColumnCount,
				Rows = Manager.Config.TerminalRowCount
			},
			CancellationToken.None),
			name
		);
	public void OnExit(object? _, ICircuitAccessor accessor)
	{
		if (accessor.CurrentCircuit == CircuitAccessor.CurrentCircuit)
		{
			Exited = true;
			TrackingCircuitHandler.CircuitDisconnected -= OnExit;
			Connection.ProcessExited -= OnPtyExit;
			CurrentCollection.Occupied = false;
		}
	}
	public async void OnPtyExit(object? _, PtyExitedEventArgs e)
	{
		await XTerminal.WriteLine($"\n[Process exited with exit code {e.ExitCode}, reload to restart instance.]");
		UserTerminalCollections.Remove(CurrentCollection);
	}
	public void GeneralOnPtyExit(object? s, PtyExitedEventArgs e)
	{
		var collection = Manager.Connections[UserName];
		collection.Remove(collection.Find(c => c.Connection == (IPtyConnection)s!)!);
	}
	public void OnKey(KeyEventArgs args)
	{
		this.Connection.WriterStream.Write(Encoding.ASCII.GetBytes(args.Key));
	}
	public async void BackgroundTask()
	{
		byte[] data = new byte[4096];
		while (!Exited)
		{
			Array.Clear(data);
			try
			{
				int count;
				count = await Connection.ReaderStream.ReadAsync(data);
				if (count == 0)
				{
					await Task.Delay(64);
				}
				else
				{
					await XTerminal.Write(data);
				}
			}
			catch { }
			await Task.Delay(16);
		}
	}
}
