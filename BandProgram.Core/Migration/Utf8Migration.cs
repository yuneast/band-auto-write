using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BandProgram
{
	public sealed class MigrationResult
	{
		public List<string> Converted { get; } = new List<string>();

		public List<string> Failed { get; } = new List<string>();
	}

	// .NET Framework 버전은 텍스트 파일을 CP949(한국어 Windows의 Encoding.Default)로 저장했다.
	// 프로그램 시작 시 데이터 폴더의 *.txt와 AutoDoc/**/*.txt 중 올바른 UTF-8이 아닌 파일을
	// CP949로 보고 UTF-8(BOM 없음)로 바꾼다. 원본은 backup-cp949/에 같은 상대 경로로 남긴다.
	public static class Utf8Migration
	{
		public const string BackupFolder = "backup-cp949";

		private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

		private static readonly Encoding Cp949 = CreateCp949();

		public static MigrationResult Run(string dataDir)
		{
			MigrationResult result = new MigrationResult();
			foreach (string file in FindTextFiles(dataDir))
			{
				string relative = Path.GetRelativePath(dataDir, file);
				try
				{
					if (ConvertIfNeeded(dataDir, file, relative))
					{
						result.Converted.Add(relative);
					}
				}
				catch (Exception ex)
				{
					result.Failed.Add(string.Concat(relative, ": ", ex.Message));
				}
			}
			return result;
		}

		internal static bool IsValidUtf8(byte[] bytes)
		{
			try
			{
				StrictUtf8.GetString(bytes);
				return true;
			}
			catch (DecoderFallbackException)
			{
				return false;
			}
		}

		private static IEnumerable<string> FindTextFiles(string dataDir)
		{
			if (!Directory.Exists(dataDir))
			{
				return Enumerable.Empty<string>();
			}
			IEnumerable<string> files = Directory.GetFiles(dataDir, "*.txt", SearchOption.TopDirectoryOnly);
			string autoDoc = Path.Combine(dataDir, "AutoDoc");
			if (Directory.Exists(autoDoc))
			{
				files = files.Concat(Directory.GetFiles(autoDoc, "*.txt", SearchOption.AllDirectories));
			}
			return files;
		}

		private static bool ConvertIfNeeded(string dataDir, string file, string relative)
		{
			byte[] bytes = File.ReadAllBytes(file);
			if (IsValidUtf8(bytes))
			{
				return false;
			}

			string backup = Path.Combine(dataDir, BackupFolder, relative);
			Directory.CreateDirectory(Path.GetDirectoryName(backup));
			if (!File.Exists(backup))
			{
				File.WriteAllBytes(backup, bytes); // 처음 원본만 보존
			}

			string temp = string.Concat(file, ".utf8tmp");
			File.WriteAllText(temp, Cp949.GetString(bytes), AppText.Encoding);
			File.Move(temp, file, true);
			return true;
		}

		private static Encoding CreateCp949()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			return Encoding.GetEncoding(949);
		}
	}
}
