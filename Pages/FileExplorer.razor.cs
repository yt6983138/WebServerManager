using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;
using System.Diagnostics;
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
			OnCurrentItemChange();
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
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	#region Utils/Shared functions
	public void CloseSubWindow()
		=> SubWindow = WindowType.None;
	public async void OnCurrentItemChange()
	{
		try
		{
			ChildFiles = CurrentDirectory.GetFiles().OrderBy(FileInfoComparer).ToList();
			ChildDirectories = CurrentDirectory.GetDirectories().OrderBy(DirectoryInfoComparer).ToList();
			// LastMessage = "";
		}
		catch (Exception ex)
		{
			ChildFiles = new();
			ChildDirectories = new();
			LastMessage = ex.Message;
		}
		Selected = new();
		await InvokeAsync(() => ValueSetter.Invoke(null, false));
	}
	public void RemoveOrAdd(bool condition, FileSystemInfo info)
	{
		if (condition)
			Selected.Add(info);
		else
			Selected.Remove(info);

		StateHasChanged();
	}
	#endregion

	#region File operations
	public void Rename()
	{
		SubWindow = WindowType.InputField;
		InputFieldSubmit = () =>
		{
			InputFieldSubmit = () => { };
			SubWindow = WindowType.None;
			if (Selected.Count == 0)
				goto Final;
			try
			{
				var item = Selected[0];
				if (item is FileInfo info)
				{
					info.MoveTo(Path.Combine(info.Directory!.FullName, InputFieldBind));
				}
				else if (item is DirectoryInfo info2)
				{
					info2.MoveTo(Path.Combine(info2.Parent?.FullName ?? @"/", InputFieldBind));
				}
				LastMessage = "";
			}
			catch (Exception ex)
			{
				LastMessage = ex.Message;
			}
		Final:
			InputFieldBind = "";
			CurrentDirectory = CurrentDirectory; // reload
		};
	}
	public void MoveTo()
	{
		CopyOrMove = OperationType.Move;
		foreach (var item in Selected)
		{
			CopyOrMoveSource.Add(item);
		}

		StateHasChanged();
	}
	public void CopyTo()
	{
		CopyOrMove = OperationType.Copy;
		foreach (var item in Selected)
		{
			CopyOrMoveSource.Add(item);
		}

		StateHasChanged();
	}
	public void PutHere()
	{
		string currentPath = CurrentDirectory.FullName;
		try
		{
			if (CopyOrMove == OperationType.Move)
			{
				foreach (var thing in CopyOrMoveSource)
				{
					if (thing is DirectoryInfo dInfo)
						dInfo.MoveTo(Path.Combine(currentPath, dInfo.Name));
					else if (thing is FileInfo fInfo)
						fInfo.MoveTo(Path.Combine(currentPath, fInfo.Name));
				}
			}
			else if (CopyOrMove == OperationType.Copy)
			{
				foreach (var thing in CopyOrMoveSource)
				{
					if (thing is DirectoryInfo dInfo)
						dInfo.CopyAll(CurrentDirectory);
					else if (thing is FileInfo fInfo)
						fInfo.CopyTo(Path.Combine(currentPath, fInfo.Name));
				}
			}
			CurrentDirectory = CurrentDirectory;
			LastMessage = "";
		}
		catch (Exception ex)
		{
			LastMessage = ex.Message;
		}
		CopyOrMoveSource = new();
		CopyOrMove = OperationType.None;
	}
	public void DeleteSelected()
	{
		foreach (var thing in Selected)
		{
			try
			{
				if (thing is DirectoryInfo info)
					info.Delete(true);
				else thing.Delete();
				LastMessage = "";
			}
			catch (Exception ex)
			{
				LastMessage = ex.Message;
			}
		}
		CurrentDirectory = CurrentDirectory;
	}
	public void GoTo()
	{
		SubWindow = WindowType.InputField;
		InputFieldSubmit = () =>
		{
			SubWindow = WindowType.None;
			if (InputFieldBind.IsNullOrEmpty())
				goto Final;
			var target = new DirectoryInfo(InputFieldBind);
			if (target.Exists)
			{
				CurrentDirectory = target;
				LastMessage = "";
			}
			else
				LastMessage = "Target path does not exist.";

			Final:
			InputFieldBind = "";
			InputFieldSubmit = () => { };
		};
	}
	public void NewFolder()
	{
		SubWindow = WindowType.InputField;
		InputFieldSubmit = () =>
		{
			SubWindow = WindowType.None;
			try
			{
				CurrentDirectory.CreateSubdirectory(InputFieldBind);
				LastMessage = "";
			}
			catch (Exception ex)
			{
				LastMessage = ex.Message;
			}
			CurrentDirectory = CurrentDirectory; // reload
			InputFieldBind = "";
			InputFieldSubmit = () => { };
		};
	}
	public void NewFile()
	{
		SubWindow = WindowType.InputField;
		InputFieldSubmit = () =>
		{
			SubWindow = WindowType.None;
			try
			{
				File.Create(Path.Combine(CurrentDirectory.FullName, InputFieldBind)).Close();
				LastMessage = "";
			}
			catch (Exception ex)
			{
				LastMessage = ex.Message;
			}
			CurrentDirectory = CurrentDirectory; // reload
			InputFieldBind = "";
			InputFieldSubmit = () => { };
		};
	}

	#region Download/Upload
	public async void UploadFile(InputFileChangeEventArgs e)
	{
		const long maxSize = 1024L * 1024L * 1024L;
		LastMessage = "Files have began to upload...";
		StateHasChanged();
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
					using FileStream fs = new(Path.Combine(CurrentDirectory.FullName, file.Name), FileMode.Create, FileAccess.Write);
					fs.Write(buffer);
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
			var currentTask = uploadTasks[i];
			var taskSuccess = await currentTask;
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
		LastMessage = sb.ToString();
		CurrentDirectory = CurrentDirectory;
	}
	public async void DownloadFile()
	{
		LastMessage = "Your file(s) are preparing...";

		if (Selected.Count != 1 || Selected[0] is not FileInfo)
		{
			// pack all
			string TempLocation = "/tmp";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) TempLocation = "%temp%";
			string outLocation = Path.Combine(Environment.ExpandEnvironmentVariables(TempLocation), Path.GetRandomFileName() + ".zip");
			using FileStream output = File.Create(outLocation);
			using ZipOutputStream zipOutput = new(output);

			foreach (var info in Selected)
			{
				if (info is FileInfo fInfo)
				{
					var newEntry = new ZipEntry(fInfo.Name)
					{
						// Note the zip format stores 2 second granularity
						DateTime = fInfo.LastWriteTime,
						Size = fInfo.Length
					};

					zipOutput.PutNextEntry(newEntry);

					// Zip the file in buffered chunks
					// the "using" will close the stream even if an exception occurs
					var buffer = new byte[4096];
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
			await JS.InvokeVoidAsync("openWindow", pathToGo);
		}
		else
		{
			string pathToGo = $"/api/DownloadFile?address={System.Net.WebUtility.UrlEncode(Selected[0].FullName)}";
			await JS.InvokeVoidAsync("openWindow", pathToGo);
			LastMessage = "File downloaded.";
		}

		static void CompressFolder(DirectoryInfo directory, ZipOutputStream zipStream, string rootEntryName)
		{
			var files = directory.GetFiles();

			foreach (var fi in files)
			{

				// Make the name in zip based on the folder
				var entryName = Path.Combine(rootEntryName, fi.Name);

				// Remove drive from name and fix slash direction
				entryName = ZipEntry.CleanName(entryName);

				var newEntry = new ZipEntry(entryName)
				{
					// Note the zip format stores 2 second granularity
					DateTime = fi.LastWriteTime,
					Size = fi.Length
				};

				zipStream.PutNextEntry(newEntry);

				// Zip the file in buffered chunks
				// the "using" will close the stream even if an exception occurs
				var buffer = new byte[4096];
				using (FileStream fsInput = File.OpenRead(fi.FullName))
				{
					StreamUtils.Copy(fsInput, zipStream, buffer);
				}
				zipStream.CloseEntry();
			}

			// Recursively call CompressFolder on all folders in path
			var folders = directory.GetDirectories();
			foreach (var folder in folders)
			{
				CompressFolder(folder, zipStream, Path.Combine(rootEntryName, folder.Name));
			}
		}
	}
	#endregion

	#endregion

	public void ViewDetails(FileSystemInfo file)
	{
		SubWindow = WindowType.Details; 
		DetailsSelected = file;
		Size = -1;
		Files = -1;
		Folders = -1;
		if (DetailsSelected is DirectoryInfo dInfo)
		{
			Task.Run(() => InvokeAsync(() =>
			{
				SumFolderSize(dInfo, ref Size, ref Files, ref Folders);
			}));
		}
		else
		{
			Size = ((FileInfo)DetailsSelected).Length;
		}
	}
	public void SumFolderSize(DirectoryInfo directory, ref long size, ref int files, ref int folders)
	{
		folders++;
		try
		{
			foreach (var file in directory.GetFiles())
			{
				try
				{
					size += file.Length;
					files++;
					StateHasChanged();
				}
				catch { }
			}
			foreach (var dir in directory.GetDirectories())
			{
				try
				{
					if (dir.LinkTarget != null)
						continue;
					SumFolderSize(dir, ref size, ref files, ref folders);
				}
				catch { }
			}
		}
		catch { }
	}
	protected override void OnInitialized()
	{
		CurrentDirectory = new(Manager.Config.FileExplorerDefaultStartPath);
		if (Utils.CheckLogin(HttpContextAccessor))
		{
			base.OnInitialized();
			return;
		}
		NavigationManager.NavigateTo("/Login");
	}
}
