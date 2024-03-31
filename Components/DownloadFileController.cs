using Microsoft.AspNetCore.Mvc;
using System.Web.Http;
using System.IO;

namespace WebServerManager.Components;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
public class DownloadFileController : Controller
{
	private readonly static EventId EventId = new(114510, "DownloadFile");

	private ILogger<DownloadFileController> Logger { get; set; }
    public DownloadFileController(ILogger<DownloadFileController> logger)
    {
		Logger = logger;
    }

    [Microsoft.AspNetCore.Mvc.HttpGet]
	public IActionResult Get(string address, string? filename = null)
	{
		string? token = Request.Cookies["token"];
		string? username = Request.Cookies["username"];
		if (username.IsNullOrEmpty() || !Manager.ActiveTokens.TryGetValue(username!, out var _val) || _val != token)
		{
			return Unauthorized();
		}

		string parsed = System.Net.WebUtility.UrlDecode(address);

		if (!System.IO.File.Exists(parsed))
		{
			Logger.LogInformation(EventId, "The user {user} requested file '{file}' which does not exist!", username, parsed);
			return NotFound();
		}

		try
		{
			var stream = System.IO.File.Open(parsed, FileMode.Open);
			Logger.LogInformation(EventId, "The user {user} requested file '{file}' which is a successfully request.", username, parsed);
			return new FileStreamResult(stream, "application/octet-stream") 
			{ 
				FileDownloadName = filename ?? Path.GetFileName(parsed) 
			};
		}
		catch (Exception ex)
		{
			Logger.LogInformation(EventId, ex, "The user {user} requested file '{file}' which failed with {exception}.", username, parsed, ex.GetType().Name);
			return Forbid();
		}
	}
}
