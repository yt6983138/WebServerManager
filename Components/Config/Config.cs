using System.Runtime.InteropServices;

namespace WebServerManager;

public class Config
{
	public const string ROOT_DIR = @"/";
	private static readonly string _terminalExecutable;
    static Config()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _terminalExecutable = "cmd.exe";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _terminalExecutable = "zsh";
        else
            _terminalExecutable = "bash";
    }

    public string DirectoryIconPath { get; set; } = "/Assets/FileExplorer/Folder.svg";
	public string UtilsDefaultStartPath { get; set; } = ".";
    public string TerminalExecutableName { get; set; } = _terminalExecutable;
    public int TerminalColumnCount { get; set; } = 80;
    public int TerminalRowCount { get; set; } = 24;
}
