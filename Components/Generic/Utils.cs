using Microsoft.AspNetCore.Components;
using System.Text;

namespace WebServerManager;

public static class Utils
{
	public static void Discard<T>(this T _) { }
	public static bool IsNullOrEmpty(this string? str)
		=> string.IsNullOrEmpty(str);
	public static bool CheckLogin(IHttpContextAccessor httpContextAccessor)
	{
		string? token = httpContextAccessor.HttpContext!.Request.Cookies["token"];
		string? username = httpContextAccessor.HttpContext!.Request.Cookies["username"];
		if (!username.IsNullOrEmpty() && Manager.ActiveTokens.TryGetValue(username!, out var _val) && _val == token)
		{
			return true;
		}
		return false;
	}
	public static bool TryAdd<T>(this IList<T> list, T item)
	{
		if (list.Contains(item))
			return false;
		list.Add(item);
		return true;
	}
	public static bool TryGetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, out TValue value, Func<TValue> valueGetter)
	{
		if (dict.TryGetValue(key, out value))
		{
			return true;
		}
		value = valueGetter();
		dict[key] = value;
		return false;
	}
	public static async Task<(bool, TValue)> TryGetOrCreateAsync<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<Task<TValue>> valueGetter)
	{
		if (dict.TryGetValue(key, out var value))
		{
			return (true, value);
		}
		value = await valueGetter();
		dict[key] = value;
		return (false, value);
	}

	private static string[] Units { get; } = new string[6] { "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
	public static string FileSizeFormatter(long sizeInByte, bool forceFormatAll = false)
	{
		const float OneThousandAnd24thOf1 = 1 / 1024f;
		if (sizeInByte < 1024)
			return $"{sizeInByte} Bytes";

		float[] sizeInOtherUnits = new float[6]; // k, m ,g ...
		sizeInOtherUnits[0] = sizeInByte * OneThousandAnd24thOf1;
		for (int i = 1;  i < sizeInOtherUnits.Length; i++)
		{
			sizeInOtherUnits[i] = sizeInOtherUnits[i - 1] * OneThousandAnd24thOf1;
		}
		if (forceFormatAll)
		{
			StringBuilder output = new();
			for (int i = 5; i >= 0; i--)
			{
				float size = sizeInOtherUnits[i] % 1024;
				if (size < 1)
					continue;
				output.Append(Math.Floor(size));
				output.Append(' ');
				output.Append(Units[i]);
				output.Append(", ");
			}
			output.Append(sizeInByte % 1024);
			output.Append(" Bytes, ");
			output.Remove(output.Length - 2, 2);
			return output.ToString();
		}

		for (int i = 5; i >= 0; i--)
		{
			float size = sizeInOtherUnits[i] % 1024;
			if (size < 1)
				continue;
			return $"{size:.00} {Units[i]}";
		}
		return "Error: this should never happen!";

	}
	public static void CopyAll(this DirectoryInfo source, DirectoryInfo targetParent)
	{
		DirectoryInfo target = new(Path.Combine(targetParent.FullName, source.Name));

		Directory.CreateDirectory(target.FullName);

		// Copy each file into the new directory.
		foreach (FileInfo fi in source.GetFiles())
		{
			Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
			fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
		}

		// Copy each subdirectory using recursion.
		foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
		{
			DirectoryInfo nextTargetSubDir =
				target.CreateSubdirectory(diSourceSubDir.Name);
			CopyAll(diSourceSubDir, nextTargetSubDir);
		}
	}
}
