using Newtonsoft.Json;
using WebServerManager.Components;
using WebServerManager.Components.Generic;

namespace WebServerManager;

public static class Manager
{
	public const string ConfigLocation = @"./Config.json";
	public const string UserFileLocation = @"./Users.json";

	public static Config Config { get; set; }
	public static List<string> SuperUsers { get; set; } = new() { "admin" };
	public static Dictionary<string, string> Users { get; set; } = new() { { "admin", "D033E22AE348AEB5660FC2140AEC35850C4DA997" } };
	// default: admin admin

	public static Dictionary<string, Event<EventHandler>> UserEvents { get; set; } = new();
	public static Dictionary<string, string> ActiveTokens { get; set; } = new();
	public static Dictionary<string, List<TerminalCollection>> Connections { get; set; } = new();
	static Manager()
	{
		try
		{
			Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigLocation))!;
		}
		catch
		{
			Config = new();
			WriteConfig();
		}
		try
		{
			(Users, SuperUsers) = JsonConvert.DeserializeObject<(Dictionary<string, string>, List<string>)>(File.ReadAllText(UserFileLocation))!;
		}
		catch
		{
			WriteUsers();
		}
		AppDomain.CurrentDomain.ProcessExit += (_, _2) => WriteConfig();
	}
	public static void WriteConfig() =>
		File.WriteAllText(ConfigLocation, JsonConvert.SerializeObject(Config));
	public static void WriteUsers() =>
		File.WriteAllText(UserFileLocation, JsonConvert.SerializeObject((Users, SuperUsers)));
}
