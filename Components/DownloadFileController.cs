using Microsoft.AspNetCore.Mvc;
using System.Web.Http;
using System.IO;

namespace WebServerManager.Components;

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
public class DownloadFileController : Controller
{
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
			return NotFound();

		try
		{
			return new FileStreamResult(System.IO.File.Open(parsed, FileMode.Open), "application/octet-stream") 
			{ 
				FileDownloadName = filename ?? Path.GetFileName(parsed) 
			};
		}
		catch
		{
			return Forbid();
		}
	}
}
