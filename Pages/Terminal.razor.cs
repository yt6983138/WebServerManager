using Microsoft.AspNetCore.Components;
using Pty.Net;
using System.Text;
using WebServerManager.Components;
using WebServerManager.Components.Circuits;
using WebServerManager.Components.Generic;
using XtermBlazor;

namespace WebServerManager.Pages;

public partial class Terminal
{
	#region Misc
	private static readonly TerminalOptions Options = new()
	{
		CursorBlink = true,
		CursorStyle = CursorStyle.Bar,
		Columns = Manager.Config.TerminalColumnCount,
		Rows = Manager.Config.TerminalRowCount
	};

	public bool Exited { get; set; } = false;
	#endregion

	public Event<EventHandler> UserEvents { get; set; }
	public List<TerminalCollection> UserTerminalCollections { get; set; }
	public TerminalCollection CurrentCollection { get; set; }
	public Xterm XTerminal { get; set; }
	public IPtyConnection Connection { get; set; }
	public string TerminalName { get; set; }
	public string UserName { get; set; }

	#region Injection
	[Inject]
	private ICircuitAccessor CircuitAccessor { get; set; }
	[Inject]
	private NavigationManager NavigationManager { get; set; }
	[Inject]
	private IHttpContextAccessor HttpContextAccessor { get; set; }
	[Inject]
	private ILogger<UserManager> Logger { get; set; }

	[Parameter, SupplyParameterFromQuery]
	public int ReloadTerm { get; set; } = int.MaxValue;
	#endregion

	#region Binding
	public string CurrentCollectionName
	{
		get => this.CurrentCollection?.Name ?? "";
		set
		{
			if (this.CurrentCollection is not null)
				this.CurrentCollection.Name = value;
		}
	}
	#endregion

	public EventHandler DefaultHandler { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public Terminal()
	{
		this.DefaultHandler = new EventHandler(async (_, _2) => await this.InvokeAsync(this.StateHasChanged));
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	protected override void OnInitialized() // this executes twice for some reason
	{
		if (Utils.CheckLogin(this.HttpContextAccessor))
		{
			base.OnInitialized();
			this.UserName = this.HttpContextAccessor.HttpContext!.Request.Cookies["username"]!;
			if (!Manager.SuperUsers.Contains(this.UserName))
				this.NavigationManager.NavigateTo("/Forbidden", true);
			if (Manager.UserEvents.TryGetValue(this.UserName, out Event<EventHandler>? e))
			{
				this.UserEvents = e;
			}
			else
			{
				this.UserEvents = new();
				Manager.UserEvents[this.UserName] = this.UserEvents;
			}

			this.UserEvents += this.DefaultHandler;
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
				this.UserName,
				out List<TerminalCollection>? collections,
				() => new()
			);
			this.UserTerminalCollections = collections;
			TrackingCircuitHandler.CircuitDisconnected += this.OnExit;
			_ = Task.Run(async () =>
			{
				await this.InvokeAsync(async () =>
				{
					await Task.Delay(100); // fuck this, the xterminal element can only be accessed after firstRender done
					if (this.UserTerminalCollections.All(t => t.Occupied))
					{
						this.UserTerminalCollections.Add(await this.CreateCollection($"Terminal - {DateTime.Now}"));
						await this.SwitchTerminal(this.UserTerminalCollections.Count - 1, false, getted);
					}
					else if (this.UserTerminalCollections.ElementAtOrDefault(this.ReloadTerm)?.Occupied == false)
					{
						await this.SwitchTerminal(this.ReloadTerm, false);
					}
					else
					{
						await this.SwitchTerminal(this.UserTerminalCollections.IndexOf(this.UserTerminalCollections.First(x => x.Occupied == false)), false);
					}
					this.BackgroundTask();
					this.StateHasChanged();
				});
			});
		}
		await base.OnAfterRenderAsync(firstRender);
		return;
	}
	public async Task SwitchTerminal(int index, bool removeOldBind = true, bool isOldSession = true)
	{
		if (this.UserTerminalCollections.Count == 0 && index == 0)
			this.UserTerminalCollections.Add(await this.CreateCollection("Terminal0"));
		TerminalCollection collection = this.UserTerminalCollections[index];
		if (collection.Occupied)
			throw new InvalidOperationException("The target connection is occupied.");
		if (this.CurrentCollection is not null)
		{
			this.CurrentCollection.Occupied = false;
		}
		if (removeOldBind)
		{
			this.Connection.ProcessExited -= this.OnPtyExit;
		}
		collection.Occupied = true;
		this.Connection = collection.Connection;
		this.TerminalName = collection.Name;
		this.CurrentCollection = collection;
		this.Connection.ProcessExited += this.OnPtyExit;
		if (isOldSession)
		{
			await this.XTerminal.Clear();
			await this.XTerminal.WriteLine("\n[Recovered session, press enter to continue]");
		}
		else
		{
			this.Connection.ProcessExited += this.GeneralOnPtyExit;
		}
	}
	public async Task<TerminalCollection> CreateCollection(string name)
	{
		_ = Task.Delay(200).ContinueWith((t) => this.UserEvents.Invoke(this, EventArgs.Empty));

		return new(false, await PtyProvider.SpawnAsync(
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
	}
	public void OnExit(object? _, ICircuitAccessor accessor)
	{
		if (accessor.CurrentCircuit == this.CircuitAccessor.CurrentCircuit)
		{
			this.Exited = true;
			TrackingCircuitHandler.CircuitDisconnected -= this.OnExit;
			this.Connection.ProcessExited -= this.OnPtyExit;
			this.CurrentCollection.Occupied = false;
			this.UserEvents -= this.DefaultHandler;
		}
	}
	public async void OnPtyExit(object? _, PtyExitedEventArgs e)
	{
		await this.XTerminal.WriteLine($"\n[Process exited with exit code {e.ExitCode}, reload to restart instance.]");
		this.UserTerminalCollections.Remove(this.CurrentCollection);
		this.UserEvents.Invoke(this, EventArgs.Empty);
	}
	public void GeneralOnPtyExit(object? s, PtyExitedEventArgs e)
	{
		List<TerminalCollection> collection = Manager.Connections[this.UserName];
		collection.Remove(collection.Find(c => c.Connection == (IPtyConnection)s!)!);
	}
	public void OnKey(KeyEventArgs args)
	{
		this.Connection.WriterStream.Write(Encoding.ASCII.GetBytes(args.Key));
	}
	public async void BackgroundTask()
	{
		byte[] data = new byte[4096];
		while (!this.Exited)
		{
			Array.Clear(data);
			try
			{
				int count;
				count = await this.Connection.ReaderStream.ReadAsync(data);
				if (count == 0)
				{
					await Task.Delay(64);
				}
				else
				{
					await this.XTerminal.Write(data);
				}
				await Task.Delay(16);
			}
			catch { }
		}
	}
	public async void CreateTerminalButtonClick()
	{
		await this.XTerminal.Clear();
		this.UserTerminalCollections.Add(await this.CreateCollection($"Terminal - {DateTime.Now}"));
		await this.SwitchTerminal(this.UserTerminalCollections.Count - 1, false, false);
	}
}
