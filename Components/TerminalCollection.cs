using Pty.Net;

namespace WebServerManager.Components;

public class TerminalCollection(bool Occupied, IPtyConnection Connection, string Name)
{
	public bool Occupied { get; set; } = Occupied;
	public IPtyConnection Connection { get; set; } = Connection;
	public string Name { get; set; } = Name;
}
