namespace WebServerManager;

public class Config
{
	public const string ROOT_DIR = @"/";

	public string DirectoryIconPath { get; set; } = "/Assets/FileExplorer/Folder.svg";
	public string FileExplorerDefaultStartPath { get; set; } = ".";
}
