using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;

namespace BandProgram
{
	// 시작할 때 새 버전을 확인하고, 있으면 묻지 않고 업데이트한 뒤 새 exe를 실행한다(강제 업데이트).
	// 어떤 실패든 지금 버전으로 계속 실행한다. 쓰기 권한 오류만 사용자에게 알린다.
	internal static class AutoUpdate
	{
		private const string UpdatedFlag = "--updated";

		private static readonly string AppDir = AppContext.BaseDirectory;

		// true를 돌려주면 새 버전을 실행했으니 지금 프로세스는 바로 끝내야 한다.
		public static bool Run(string[] args)
		{
			try
			{
				UpdateInstaller.Cleanup(AppDir);
				if (Array.IndexOf(args, UpdatedFlag) >= 0)
				{
					return false;
				}

				UpdateResult result;
				using (var http = new HttpClient())
				{
					Version current = typeof(AutoUpdate).Assembly.GetName().Version;
					var updater = new Updater(http, Updater.DefaultManifestUrl, UpdatePublicKey.Base64, current, AppDir);
					UpdateResult check = updater.Check();
					if (check.Outcome != UpdateOutcome.NoUpdate)
					{
						Log(check.Reason);
					}
					if (check.Outcome != UpdateOutcome.UpdateAvailable)
					{
						return false;
					}
					using (var form = new UpdateForm(updater, check.Manifest))
					{
						Application.Run(form);
						result = form.Result;
					}
				}

				if (result == null || result.Outcome != UpdateOutcome.ReadyToRestart)
				{
					Log(result == null ? "업데이트 중 예기치 않은 오류" : result.Reason);
					if (result != null && result.IsPermissionError)
					{
						MessageBox.Show("업데이트하지 못했습니다. 프로그램 폴더의 쓰기 권한을 확인해 주세요.");
					}
					return false;
				}

				Log(result.Reason);
				try
				{
					var psi = new ProcessStartInfo(result.NewExePath) { UseShellExecute = false, WorkingDirectory = AppDir };
					foreach (string arg in args)
					{
						psi.ArgumentList.Add(arg);
					}
					psi.ArgumentList.Add(UpdatedFlag);
					Process.Start(psi);
					return true;
				}
				catch (Exception ex)
				{
					Log(string.Concat("새 버전 실행 실패, 되돌림: ", ex.Message));
					result.Journal.Undo();
					return false;
				}
			}
			catch (Exception ex)
			{
				Log(string.Concat("업데이트 처리 중 오류: ", ex.Message));
				return false;
			}
		}

		private static void Log(string message)
		{
			try
			{
				File.AppendAllText(Path.Combine(AppDir, "update.log"),
					string.Concat(DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), " ", message, Environment.NewLine));
			}
			catch
			{
			}
		}
	}
}
