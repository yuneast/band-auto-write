using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BandProgram
{
	internal class ADB
	{
		private static ADB instance;

		internal static string Executable
		{
			get { return OperatingSystem.IsWindows() ? "adb.exe" : "adb"; }
		}

		private Process process;

		private ProcessStartInfo startInfo;

		public ADB()
		{
		}

		public void changeIpWithADB(string deviceNum)
		{
			this.executeADB("shell svc data disable");
			Util.getInstance().delay(3000);
			this.executeADB("shell svc data enable");
		}

		private string executeADB(string command)
		{
			this.startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			this.startInfo.UseShellExecute = false;
			this.startInfo.RedirectStandardOutput = true;
			this.startInfo.FileName = ADB.Executable;
			this.startInfo.Arguments = command;
			this.process.StartInfo.CreateNoWindow = true;
			this.process.StartInfo = this.startInfo;
			this.process.Start();
			string end = this.process.StandardOutput.ReadToEnd();
			this.process.WaitForExit();
			return end;
		}

		public List<string> getDeivces()
		{
			List<string> strs = new List<string>();
			string[] strArrays = this.executeADB("get-serialno").Split(new char[] { '\n' });
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				strs.Add(strArrays[i]);
			}
			return strs;
		}

		public static ADB getInstnace()
		{
			if (ADB.instance == null)
			{
				ADB.instance = new ADB()
				{
					process = new Process(),
					startInfo = new ProcessStartInfo()
				};
			}
			return ADB.instance;
		}
	}
}