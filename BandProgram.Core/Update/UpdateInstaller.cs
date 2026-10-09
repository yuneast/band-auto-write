using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BandProgram
{
	// 실행 중인 exe도 이름 바꾸기는 허용되므로, 옛 파일을 *.old.exe로 옮기고 새 파일을 그 자리에 넣는다.
	public static class UpdateInstaller
	{
		public const string UpdateDirName = ".update";

		private static readonly string[] Files = { UpdatePackage.ExeName, UpdatePackage.SeleniumManagerName };

		public static string OldName(string fileName)
		{
			return string.Concat(Path.GetFileNameWithoutExtension(fileName), ".old", Path.GetExtension(fileName));
		}

		// 지난 업데이트의 잔여물 정리. 이전 프로세스가 종료 중이면 잠겨 있을 수 있어 재시도하고, 끝내 실패하면 다음 실행으로 넘긴다.
		public static void Cleanup(string appDir, int attempts = 10, int delayMs = 200)
		{
			var targets = new List<string>();
			foreach (string file in Files)
			{
				targets.Add(Path.Combine(appDir, OldName(file)));
			}
			string updateDir = Path.Combine(appDir, UpdateDirName);
			for (int i = 0; i < attempts; i++)
			{
				bool done = true;
				foreach (string path in targets)
				{
					done &= TryDelete(() =>
					{
						if (File.Exists(path))
						{
							File.SetAttributes(path, FileAttributes.Normal);
							File.Delete(path);
						}
					});
				}
				done &= TryDelete(() =>
				{
					if (Directory.Exists(updateDir))
					{
						foreach (string file in Directory.EnumerateFiles(updateDir, "*", SearchOption.AllDirectories))
						{
							File.SetAttributes(file, FileAttributes.Normal);
						}
						Directory.Delete(updateDir, true);
					}
				});
				if (done)
				{
					return;
				}
				Thread.Sleep(delayMs);
			}
		}

		public static SwapJournal Swap(string appDir, string newFilesDir)
		{
			var journal = new SwapJournal();
			try
			{
				foreach (string file in Files)
				{
					string target = Path.Combine(appDir, file);
					string old = Path.Combine(appDir, OldName(file));
					if (File.Exists(old))
					{
						File.SetAttributes(old, FileAttributes.Normal);
						File.Delete(old);
					}
					if (File.Exists(target))
					{
						journal.Move(target, old);
					}
					journal.Move(Path.Combine(newFilesDir, file), target);
				}
				return journal;
			}
			catch (Exception ex)
			{
				if (!journal.Undo())
				{
					throw new IOException(string.Concat("교체 실패 후 되돌리기 일부 실패: ", ex.Message), ex);
				}
				throw;
			}
		}

		private static bool TryDelete(Action delete)
		{
			try
			{
				delete();
				return true;
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				return false;
			}
		}
	}

	public sealed class SwapJournal
	{
		private readonly List<KeyValuePair<string, string>> moves = new List<KeyValuePair<string, string>>();

		internal void Move(string from, string to)
		{
			File.Move(from, to);
			moves.Add(new KeyValuePair<string, string>(from, to));
		}

		// 한 이름 바꾸기를 역순으로 되돌린다. 하나가 실패해도 나머지는 계속한다.
		public bool Undo()
		{
			bool ok = true;
			for (int i = moves.Count - 1; i >= 0; i--)
			{
				try
				{
					File.Move(moves[i].Value, moves[i].Key);
				}
				catch (Exception)
				{
					ok = false;
				}
			}
			moves.Clear();
			return ok;
		}
	}
}
