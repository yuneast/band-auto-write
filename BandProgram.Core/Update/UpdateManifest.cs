using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BandProgram
{
	// 서버의 version.json. 서명 검증을 통과한 바이트만 해석한다.
	public sealed class UpdateManifest
	{
		private static readonly Regex Sha256Pattern = new Regex("^[0-9a-f]{64}$");

		public Version Version { get; private set; }

		public Uri Url { get; private set; }

		public string Sha256 { get; private set; }

		public static bool TryParse(byte[] json, out UpdateManifest manifest, out string error)
		{
			manifest = null;
			try
			{
				using (JsonDocument doc = JsonDocument.Parse(json))
				{
					JsonElement root = doc.RootElement;
					if (root.ValueKind != JsonValueKind.Object)
					{
						error = "객체가 아님";
						return false;
					}
					Version version;
					if (!TryGetString(root, "version", out string versionText) || !Version.TryParse(versionText, out version))
					{
						error = "version 없음 또는 형식 오류";
						return false;
					}
					Uri url;
					if (!TryGetString(root, "url", out string urlText)
						|| !Uri.TryCreate(urlText, UriKind.Absolute, out url)
						|| (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
					{
						error = "url 없음 또는 http(s)가 아님";
						return false;
					}
					string sha = TryGetString(root, "sha256", out string shaText) ? shaText.ToLowerInvariant() : null;
					if (sha == null || !Sha256Pattern.IsMatch(sha))
					{
						error = "sha256 없음 또는 형식 오류";
						return false;
					}
					manifest = new UpdateManifest { Version = version, Url = url, Sha256 = sha };
					error = null;
					return true;
				}
			}
			catch (JsonException ex)
			{
				error = string.Concat("JSON 오류: ", ex.Message);
				return false;
			}
		}

		private static bool TryGetString(JsonElement root, string name, out string value)
		{
			JsonElement element;
			if (root.TryGetProperty(name, out element) && element.ValueKind == JsonValueKind.String)
			{
				value = element.GetString();
				return true;
			}
			value = null;
			return false;
		}
	}
}
