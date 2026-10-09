using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace BandProgram
{
	public enum UpdateOutcome
	{
		NoUpdate,
		UpdateAvailable,
		Failed,
		ReadyToRestart
	}

	public sealed class UpdateResult
	{
		public UpdateOutcome Outcome { get; private set; }

		public string Reason { get; private set; }

		public bool IsPermissionError { get; private set; }

		public UpdateManifest Manifest { get; private set; }

		public SwapJournal Journal { get; private set; }

		public string NewExePath { get; private set; }

		internal static UpdateResult NoUpdate(string reason)
		{
			return new UpdateResult { Outcome = UpdateOutcome.NoUpdate, Reason = reason };
		}

		internal static UpdateResult Available(UpdateManifest manifest)
		{
			return new UpdateResult { Outcome = UpdateOutcome.UpdateAvailable, Manifest = manifest, Reason = string.Concat("새 버전 ", manifest.Version) };
		}

		internal static UpdateResult Failed(string reason, bool isPermissionError = false)
		{
			return new UpdateResult { Outcome = UpdateOutcome.Failed, Reason = reason, IsPermissionError = isPermissionError };
		}

		internal static UpdateResult Ready(UpdateManifest manifest, SwapJournal journal, string newExePath)
		{
			return new UpdateResult { Outcome = UpdateOutcome.ReadyToRestart, Manifest = manifest, Journal = journal, NewExePath = newExePath, Reason = string.Concat("업데이트 완료 ", manifest.Version) };
		}
	}

	// 서명된 version.json으로 새 버전을 확인하고, zip을 받아 검증한 뒤 파일을 교체한다.
	// 프로세스 실행과 종료는 호출하는 쪽(WinForms)이 맡는다. 어떤 경우에도 예외를 던지지 않는다.
	public sealed class Updater
	{
		public static readonly Uri DefaultManifestUrl = new Uri("http://newsoft.kr/download/version.json");

		private readonly HttpClient http;
		private readonly Uri manifestUrl;
		private readonly string publicKeyBase64;
		private readonly Version currentVersion;
		private readonly string appDir;

		public Updater(HttpClient http, Uri manifestUrl, string publicKeyBase64, Version currentVersion, string appDir)
		{
			this.http = http;
			this.manifestUrl = manifestUrl;
			this.publicKeyBase64 = publicKeyBase64;
			this.currentVersion = currentVersion;
			this.appDir = appDir;
		}

		public TimeSpan ManifestTimeout { get; set; } = TimeSpan.FromSeconds(5);

		public TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromMinutes(5);

		public long MaxManifestBytes { get; set; } = 64 * 1024;

		public long MaxZipBytes { get; set; } = 200L * 1024 * 1024;

		public UpdateResult Check()
		{
			try
			{
				byte[] manifestBytes;
				byte[] signature;
				try
				{
					manifestBytes = GetBytes(manifestUrl);
					signature = GetBytes(new Uri(string.Concat(manifestUrl.ToString(), ".sig")));
				}
				catch (Exception ex)
				{
					return UpdateResult.Failed(string.Concat("확인 실패: ", ex.Message));
				}
				if (!ManifestVerifier.Verify(manifestBytes, signature, publicKeyBase64))
				{
					return UpdateResult.Failed("서명 검증 실패");
				}
				UpdateManifest manifest;
				string error;
				if (!UpdateManifest.TryParse(manifestBytes, out manifest, out error))
				{
					return UpdateResult.Failed(string.Concat("version.json 오류: ", error));
				}
				if (manifest.Version <= currentVersion)
				{
					return UpdateResult.NoUpdate(string.Concat("최신 버전 사용 중 (", currentVersion, ")"));
				}
				return UpdateResult.Available(manifest);
			}
			catch (Exception ex)
			{
				return UpdateResult.Failed(string.Concat("확인 실패: ", ex.Message));
			}
		}

		public UpdateResult Install(UpdateManifest manifest, Action<long, long?> progress)
		{
			string stage = "다운로드 실패";
			string zipPath = null;
			try
			{
				string updateDir = Path.Combine(appDir, UpdateInstaller.UpdateDirName);
				zipPath = Path.Combine(updateDir, "BandProgram.zip");
				string newDir = Path.Combine(updateDir, "new");
				if (Directory.Exists(updateDir))
				{
					Directory.Delete(updateDir, true);
				}
				Directory.CreateDirectory(newDir);
				Download(manifest.Url, zipPath, progress);

				stage = "해시 확인 실패";
				string actual = UpdatePackage.Sha256Hex(zipPath);
				if (!string.Equals(actual, manifest.Sha256, StringComparison.Ordinal))
				{
					return UpdateResult.Failed(string.Concat("해시 불일치 (", actual, ")"));
				}

				stage = "압축 해제 실패";
				string error;
				if (!UpdatePackage.TryExtract(zipPath, newDir, out error))
				{
					return UpdateResult.Failed(error);
				}

				stage = "교체 실패";
				SwapJournal journal = UpdateInstaller.Swap(appDir, newDir);
				return UpdateResult.Ready(manifest, journal, Path.Combine(appDir, UpdatePackage.ExeName));
			}
			catch (UnauthorizedAccessException ex)
			{
				DeletePartial(stage, zipPath);
				return UpdateResult.Failed(string.Concat(stage, "(권한): ", ex.Message), true);
			}
			catch (Exception ex)
			{
				DeletePartial(stage, zipPath);
				return UpdateResult.Failed(string.Concat(stage, ": ", ex.Message));
			}
		}

		// 다운로드 단계에서 실패하면 받다 만 zip을 지운다.
		private static void DeletePartial(string stage, string zipPath)
		{
			if (zipPath == null || stage != "다운로드 실패")
			{
				return;
			}
			try
			{
				if (File.Exists(zipPath))
				{
					File.Delete(zipPath);
				}
			}
			catch (Exception)
			{
			}
		}

		private byte[] GetBytes(Uri url)
		{
			using (var cts = new CancellationTokenSource(ManifestTimeout))
			using (HttpResponseMessage response = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).GetAwaiter().GetResult())
			{
				response.EnsureSuccessStatusCode();
				long? length = response.Content.Headers.ContentLength;
				if (length.HasValue && length.Value > MaxManifestBytes)
				{
					throw new IOException("응답이 너무 큽니다");
				}
				using (Stream source = response.Content.ReadAsStreamAsync(cts.Token).GetAwaiter().GetResult())
				using (var buffer = new MemoryStream())
				{
					byte[] chunk = new byte[8192];
					int read;
					while ((read = source.ReadAsync(chunk, 0, chunk.Length, cts.Token).GetAwaiter().GetResult()) > 0)
					{
						buffer.Write(chunk, 0, read);
						if (buffer.Length > MaxManifestBytes)
						{
							throw new IOException("응답이 너무 큽니다");
						}
					}
					return buffer.ToArray();
				}
			}
		}

		private void Download(Uri url, string path, Action<long, long?> progress)
		{
			using (var cts = new CancellationTokenSource(DownloadTimeout))
			using (HttpResponseMessage response = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).GetAwaiter().GetResult())
			{
				response.EnsureSuccessStatusCode();
				long? total = response.Content.Headers.ContentLength;
				if (total.HasValue && total.Value > MaxZipBytes)
				{
					throw new IOException("파일이 너무 큽니다");
				}
				using (Stream source = response.Content.ReadAsStreamAsync(cts.Token).GetAwaiter().GetResult())
				using (FileStream target = File.Create(path))
				{
					byte[] buffer = new byte[81920];
					long received = 0;
					int read;
					while ((read = source.ReadAsync(buffer, 0, buffer.Length, cts.Token).GetAwaiter().GetResult()) > 0)
					{
						received += read;
						if (received > MaxZipBytes)
						{
							throw new IOException("파일이 너무 큽니다");
						}
						target.Write(buffer, 0, read);
						if (progress != null)
						{
							progress(received, total);
						}
					}
				}
			}
		}
	}
}
