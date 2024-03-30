using System.Security.Cryptography;
using System.Text;

namespace WebServerManager.Components.Authorize;

public static class HashChecker
{
	private readonly static SHA1 _sha = SHA1.Create();

	public static string GetHash(string obj)
		=> Convert.ToHexString(_sha.ComputeHash(Encoding.UTF8.GetBytes(obj)));
	public static string GenerateHash(string? obj = null)
		=> GetHash(obj ?? Path.GetRandomFileName());
}
