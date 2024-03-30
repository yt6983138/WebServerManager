namespace WebServerManager;

public class Config
{
	public const string ROOT_DIR = @"/";

	public string DirectoryIconPath { get; set; } = "/Assets/FileExplorer/Folder.png";
	public string UnidentifiedIconPath { get; set; } = "/Assets/FileExplorer/Unknown.png";
	public string FileExplorerDefaultStartPath { get; set; } = ".";
}
