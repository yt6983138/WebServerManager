using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Runtime.InteropServices;
using System.Text;
using WebServerManager.Components;

namespace WebServerManager.Pages;



public partial class FileExplorer
{
	#region Defintion
	public enum WindowType
	{
		None,
		InputField,
		Details
	}
	public enum OperationType
	{
		None,
		Move,
		Copy
	}
	private const string SizeToolTip = "Click for details";
	private const string NameToolTip = "Click to open";
	private const string IconsRelativePath = "/Assets/FileExplorer/";
	private readonly static EventId EventId = new(114513, "FileOperation");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	private DirectoryInfo IconsPath { get; set; } // should only be assigned once
	private List<(string, string)> AvailableIconNames { get; set; } // ^^ also no .png .svg extension etc
	private string Username { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	#region Misc
	private DirectoryInfo _currentDirectory = null!;

	private CheckBox.ValueSet ValueSetter { get; set; } = new();
	#endregion

	#region Current directory things
	public DirectoryInfo CurrentDirectory
	{
		get => this._currentDirectory;
		set
		{
			this._currentDirectory = value;
			this.OnCurrentItemChange();
		}
	}
	public DirectoryInfo? Parent
	{
		get => this._currentDirectory.Parent;
	}
	public Func<FileInfo, object> FileInfoComparer { get; set; } = (info) => info.Name;
	public Func<DirectoryInfo, object> DirectoryInfoComparer { get; set; } = (info) => info.Name;
	public List<FileInfo> ChildFiles { get; private set; } = new();
	public List<DirectoryInfo> ChildDirectories { get; private set; } = new();
	public string LastMessage { get; private set; } = "";
	#endregion

	#region Selection things
	public List<FileSystemInfo> Selected { get; set; } = new();
	public OperationType CopyOrMove { get; set; } = OperationType.None;
	public List<FileSystemInfo> CopyOrMoveSource { get; set; } = new();
	#endregion

	#region Sub-window things
	public WindowType SubWindow { get; set; } = WindowType.None;
	public string InputFieldBind { get; set; } = "";
	public Action InputFieldSubmit { get; set; } = () => { };
	public FileSystemInfo? DetailsSelected { get; set; }
	private bool Summed { get; set; } = false;
	private long Size = -1;
	private int Files = -1;
	private int Folders = -1;
	#endregion

	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	private IJSRuntime JS { get; set; }
	[Inject]
	private IHttpContextAccessor HttpContextAccessor { get; set; }
	[Inject]
	private NavigationManager NavigationManager { get; set; }
	[Inject]
	private ILogger<FileExplorer> Logger { get; set; }
	[Inject]
	private IWebHostEnvironment WebHostEnvironment { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	#region Utils/Shared functions
	public void CloseSubWindow()
		=> this.SubWindow = WindowType.None;
	public async void OnCurrentItemChange()
	{
		try
		{
			this.ChildFiles = this.CurrentDirectory.GetFiles().OrderBy(this.FileInfoComparer).ToList();
			this.ChildDirectories = this.CurrentDirectory.GetDirectories().OrderBy(this.DirectoryInfoComparer).ToList();
			// LastMessage = "";
		}
		catch (Exception ex)
		{
			this.ChildFiles = new();
			this.ChildDirectories = new();
			this.LastMessage = ex.Message;
		}
		this.Selected = new();
		await this.InvokeAsync(() => this.ValueSetter.Invoke(null, false));
	}
	public void RemoveOrAdd(bool condition, FileSystemInfo info)
	{
		if (condition)
			this.Selected.Add(info);
		else
			this.Selected.Remove(info);

		this.StateHasChanged();
	}
	public void SumFolderSize(DirectoryInfo directory, ref long size, ref int files, ref int folders)
	{
		folders++;
		try
		{
			foreach (FileInfo file in directory.GetFiles())
			{
				try
				{
					size += file.Length;
					files++;
				}
				catch { }
			}
			foreach (DirectoryInfo dir in directory.GetDirectories())
			{
				try
				{
					if (dir.LinkTarget != null)
						continue;
					this.SumFolderSize(dir, ref size, ref files, ref folders);
				}
				catch { }
			}
		}
		catch { }
		this.StateHasChanged();
	}
	public string GetIconRegardingToTheFileExtensionOrDefault(FileInfo fileInfo)
	{
		foreach ((string name, string extension) in this.AvailableIconNames)
		{
			if (fileInfo.Extension[1..].Equals(name, StringComparison.CurrentCultureIgnoreCase))
				return IconsRelativePath + name + extension;
		}
		return IconsRelativePath + "Unknown.svg";
	}
	#endregion

	#region File operations
	public void Rename()
	{
		this.SubWindow = WindowType.InputField;
		this.InputFieldSubmit = () =>
		{
			this.InputFieldSubmit = () => { };
			this.SubWindow = WindowType.None;
			if (this.Selected.Count == 0)
				goto Final;
			try
			{
				FileSystemInfo item = this.Selected[0];
				if (item is FileInfo info)
				{
					info.MoveTo(Path.Combine(info.Directory!.FullName, this.InputFieldBind));
				}
				else if (item is DirectoryInfo info2)
				{
					info2.MoveTo(Path.Combine(info2.Parent?.FullName ?? @"/", this.InputFieldBind));
				}
				this.Logger.LogInformation(
					EventId,
					"The user {user} renamed item {oldName} at {path} to {newName}",
					this.Username,
					item.Name,
					Path.GetDirectoryName(item.FullName),
					this.InputFieldBind
				);
				this.LastMessage = "";
			}
			catch (Exception ex)
			{
				this.LastMessage = ex.Message;
			}
		Final:
			this.InputFieldBind = "";
			this.CurrentDirectory = this.CurrentDirectory; // reload
		};
	}
	public void MoveTo()
	{
		this.CopyOrMove = OperationType.Move;
		foreach (FileSystemInfo item in this.Selected)
		{
			this.CopyOrMoveSource.Add(item);
		}

		this.StateHasChanged();
	}
	public void CopyTo()
	{
		this.CopyOrMove = OperationType.Copy;
		foreach (FileSystemInfo item in this.Selected)
		{
			this.CopyOrMoveSource.Add(item);
		}

		this.StateHasChanged();
	}
	public void PutHere()
	{
		string currentPath = this.CurrentDirectory.FullName;
		try
		{
			if (this.CopyOrMove == OperationType.Move)
			{
				foreach (FileSystemInfo thing in this.CopyOrMoveSource)
				{
					string path = thing.FullName;

					if (thing is DirectoryInfo dInfo)
						dInfo.MoveTo(Path.Combine(currentPath, dInfo.Name));
					else if (thing is FileInfo fInfo)
						fInfo.MoveTo(Path.Combine(currentPath, fInfo.Name));
					this.Logger.LogInformation(
						EventId,
						"The user {user} moved item {item} from {path1} to {path2}.",
						this.Username,
						thing.Name,
						Path.GetDirectoryName(path),
						this.CurrentDirectory.FullName
					);
				}
			}
			else if (this.CopyOrMove == OperationType.Copy)
			{
				foreach (FileSystemInfo thing in this.CopyOrMoveSource)
				{
					string path = thing.FullName;

					if (thing is DirectoryInfo dInfo)
						dInfo.CopyAll(this.CurrentDirectory);
					else if (thing is FileInfo fInfo)
						fInfo.CopyTo(Path.Combine(currentPath, fInfo.Name));
					this.Logger.LogInformation(
						EventId,
						"The user {user} copied item {item} from {path1} to {path2}.",
						this.Username,
						thing.Name,
						Path.GetDirectoryName(path),
						this.CurrentDirectory.FullName
					);
				}
			}
			this.CurrentDirectory = this.CurrentDirectory;
			this.LastMessage = "";
		}
		catch (Exception ex)
		{
			this.LastMessage = ex.Message;
		}
		this.CopyOrMoveSource = new();
		this.CopyOrMove = OperationType.None;
	}
	public void DeleteSelected()
	{
		foreach (FileSystemInfo thing in this.Selected)
		{
			try
			{
				if (thing is DirectoryInfo info)
					info.Delete(true);
				else thing.Delete();
				this.LastMessage = "";
				this.Logger.LogInformation(
					EventId,
					"The user {user} deleted item {item} at path {path}.",
					this.Username,
					thing.Name,
					this.CurrentDirectory.FullName
				);
			}
			catch (Exception ex)
			{
				this.LastMessage = ex.Message;
			}
		}
		this.CurrentDirectory = this.CurrentDirectory;
	}
	public void GoTo()
	{
		this.SubWindow = WindowType.InputField;
		this.InputFieldSubmit = () =>
		{
			this.SubWindow = WindowType.None;
			if (this.InputFieldBind.IsNullOrEmpty())
				goto Final;
			DirectoryInfo target = new(this.InputFieldBind);
			if (target.Exists)
			{
				this.CurrentDirectory = target;
				this.LastMessage = "";
			}
			else
				this.LastMessage = "Target path does not exist.";

			Final:
			this.InputFieldBind = "";
			this.InputFieldSubmit = () => { };
		};
	}
	public void NewFolder()
	{
		this.SubWindow = WindowType.InputField;
		this.InputFieldSubmit = () =>
		{
			this.SubWindow = WindowType.None;
			try
			{
				this.CurrentDirectory.CreateSubdirectory(this.InputFieldBind);
				this.LastMessage = "";
				this.Logger.LogInformation(
					EventId,
					"The user {user} made new folder {item} at path {path}.",
					this.Username,
					this.InputFieldBind,
					this.CurrentDirectory.FullName
				);
			}
			catch (Exception ex)
			{
				this.LastMessage = ex.Message;
			}
			this.CurrentDirectory = this.CurrentDirectory; // reload
			this.InputFieldBind = "";
			this.InputFieldSubmit = () => { };
		};
	}
	public void NewFile()
	{
		this.SubWindow = WindowType.InputField;
		this.InputFieldSubmit = () =>
		{
			this.SubWindow = WindowType.None;
			try
			{
				File.Create(Path.Combine(this.CurrentDirectory.FullName, this.InputFieldBind)).Close();
				this.LastMessage = "";
				this.Logger.LogInformation(
					EventId,
					"The user {user} made new file {item} at path {path}.",
					this.Username,
					this.InputFieldBind,
					this.CurrentDirectory.FullName
				);
			}
			catch (Exception ex)
			{
				this.LastMessage = ex.Message;
			}
			this.CurrentDirectory = this.CurrentDirectory; // reload
			this.InputFieldBind = "";
			this.InputFieldSubmit = () => { };
		};
	}
	public void ViewDetails(FileSystemInfo file)
	{
		this.SubWindow = WindowType.Details;
		this.DetailsSelected = file;
		this.Summed = false;
		this.Size = -1;
		this.Files = -1;
		this.Folders = -1;
		if (this.DetailsSelected is DirectoryInfo dInfo)
		{
			Task.Run(() => this.InvokeAsync(() =>
			{
				this.SumFolderSize(dInfo, ref this.Size, ref this.Files, ref this.Folders);
				this.Summed = true;
			}));
		}
		else
		{
			this.Size = ((FileInfo)this.DetailsSelected).Length;
			this.Summed = true;
		}
	}

	#region Download/Upload
	public async void UploadFile(InputFileChangeEventArgs e)
	{
		const long maxSize = 1024L * 1024L * 1024L;
		this.LastMessage = "Files have began to upload...";
		this.StateHasChanged();
		IReadOnlyList<IBrowserFile> files = e.GetMultipleFiles(int.MaxValue);
		List<Task<bool>> uploadTasks = new();
		foreach (IBrowserFile file in files)
		{
			Task<bool> upload = Task.Run(async () =>
			{
				try
				{
					using Stream stream = file.OpenReadStream(maxSize);
					byte[] buffer = new byte[file.Size];
					await stream.ReadAsync(buffer);
					using FileStream fs = new(Path.Combine(this.CurrentDirectory.FullName, file.Name), FileMode.Create, FileAccess.Write);
					fs.Write(buffer);
					this.Logger.LogInformation(
						EventId,
						"The user {user} uploaded item {item} to {path}.",
						this.Username,
						file.Name,
						this.CurrentDirectory.FullName
					);
					return true;
				}
				catch
				{
					return false;
				}
			});
			uploadTasks.Add(upload);
		}
		StringBuilder sb = new("File(s) uploaded successfully! ");
		bool everFailed = false;
		for (int i = 0; i < uploadTasks.Count; i++)
		{
			Task<bool> currentTask = uploadTasks[i];
			bool taskSuccess = await currentTask;
			if (taskSuccess == false)
			{
				if (!everFailed)
					sb.Append("One or more files failed to upload: ");
				sb.Append('"');
				sb.Append(files[i].Name);
				sb.Append("\", ");
			}
		}
		if (everFailed)
			sb.Remove(sb.Length - 2, 2);
		this.LastMessage = sb.ToString();
		this.CurrentDirectory = this.CurrentDirectory;
	}
	public async void DownloadFile()
	{
		this.LastMessage = "Your file(s) are preparing...";

		if (this.Selected.Count != 1 || this.Selected[0] is not FileInfo)
		{
			// pack all
			string TempLocation = "/tmp";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) TempLocation = "%temp%";
			string outLocation = Path.Combine(Environment.ExpandEnvironmentVariables(TempLocation), Path.GetRandomFileName() + ".zip");
			using FileStream output = File.Create(outLocation);
			using ZipOutputStream zipOutput = new(output);

			foreach (FileSystemInfo info in this.Selected)
			{
				if (info is FileInfo fInfo)
				{
					ZipEntry newEntry = new(fInfo.Name)
					{
						// Note the zip format stores 2 second granularity
						DateTime = fInfo.LastWriteTime,
						Size = fInfo.Length
					};

					zipOutput.PutNextEntry(newEntry);

					// Zip the file in buffered chunks
					// the "using" will close the stream even if an exception occurs
					byte[] buffer = new byte[4096];
					using (FileStream fsInput = fInfo.OpenRead())
					{
						StreamUtils.Copy(fsInput, zipOutput, buffer);
					}
					zipOutput.CloseEntry();
				}
				else if (info is DirectoryInfo dInfo)
				{
					CompressFolder(dInfo, zipOutput, dInfo.Name);
				}
			}
			string pathToGo = $"/api/DownloadFile?address={System.Net.WebUtility.UrlEncode(outLocation)}";
			await this.JS.InvokeVoidAsync("openWindow", pathToGo);
		}
		else
		{
			string pathToGo = $"/api/DownloadFile?address={System.Net.WebUtility.UrlEncode(this.Selected[0].FullName)}";
			await this.JS.InvokeVoidAsync("openWindow", pathToGo);
			this.LastMessage = "File downloaded.";
		}

		static void CompressFolder(DirectoryInfo directory, ZipOutputStream zipStream, string rootEntryName)
		{
			FileInfo[] files = directory.GetFiles();

			foreach (FileInfo fi in files)
			{

				// Make the name in zip based on the folder
				string entryName = Path.Combine(rootEntryName, fi.Name);

				// Remove drive from name and fix slash direction
				entryName = ZipEntry.CleanName(entryName);

				ZipEntry newEntry = new(entryName)
				{
					// Note the zip format stores 2 second granularity
					DateTime = fi.LastWriteTime,
					Size = fi.Length
				};

				zipStream.PutNextEntry(newEntry);

				// Zip the file in buffered chunks
				// the "using" will close the stream even if an exception occurs
				byte[] buffer = new byte[4096];
				using (FileStream fsInput = File.OpenRead(fi.FullName))
				{
					StreamUtils.Copy(fsInput, zipStream, buffer);
				}
				zipStream.CloseEntry();
			}

			// Recursively call CompressFolder on all folders in path
			DirectoryInfo[] folders = directory.GetDirectories();
			foreach (DirectoryInfo folder in folders)
			{
				CompressFolder(folder, zipStream, Path.Combine(rootEntryName, folder.Name));
			}
		}
	}
	#endregion

	#endregion

	protected override void OnInitialized()
	{
		this.CurrentDirectory = new(Manager.Config.UtilsDefaultStartPath);
		if (Utils.CheckLogin(this.HttpContextAccessor))
		{
			this.IconsPath = new(Path.Combine(this.WebHostEnvironment.WebRootPath, IconsRelativePath[1..]));

			this.AvailableIconNames = this.IconsPath.GetFiles()
				.Select(file => (Path.GetFileNameWithoutExtension(file.Name), file.Extension))
				.ToList();

			this.Username = this.HttpContextAccessor.HttpContext!.Request.Cookies["username"]!;

			base.OnInitialized();
			return;
		}
		this.NavigationManager.NavigateTo("/Login");
	}
}
