using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace BandProgram
{
	// 업데이트 zip: BandProgram.exe와 selenium-manager.exe 두 파일만, 폴더 없이 들어 있어야 한다.
	public static class UpdatePackage
	{
		public const string ExeName = "BandProgram.exe";

		public const string SeleniumManagerName = "selenium-manager.exe";

		private static readonly string[] ExpectedNames = { ExeName, SeleniumManagerName };

		public static string Sha256Hex(string path)
		{
			using (FileStream stream = File.OpenRead(path))
			{
				return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
			}
		}

		public static bool TryExtract(string zipPath, string targetDir, out string error)
		{
			try
			{
				using (ZipArchive zip = ZipFile.OpenRead(zipPath))
				{
					var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
					foreach (ZipArchiveEntry entry in zip.Entries)
					{
						string name = entry.FullName;
						if (name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0 || name.Contains("..")
							|| Array.FindIndex(ExpectedNames, n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) < 0
							|| entries.ContainsKey(name))
						{
							error = string.Concat("예상하지 못한 항목: ", name);
							return false;
						}
						entries[name] = entry;
					}
					if (entries.Count != ExpectedNames.Length)
					{
						error = "필요한 파일이 없음";
						return false;
					}
					foreach (string name in ExpectedNames)
					{
						entries[name].ExtractToFile(Path.Combine(targetDir, name), true);
					}
				}
				error = null;
				return true;
			}
			catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException)
			{
				error = string.Concat("zip 오류: ", ex.Message);
				return false;
			}
		}
	}
}
