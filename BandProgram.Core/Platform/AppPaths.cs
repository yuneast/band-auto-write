using System;
using System.IO;

namespace BandProgram
{
	// 기존 Application.StartupPath 자리. 고객 앱은 exe 폴더, Dev 셸은 --data 폴더를 쓴다.
	public static class AppPaths
	{
		private static string dataDir = Path.GetFullPath(AppContext.BaseDirectory);

		public static string DataDir
		{
			get { return dataDir; }
			set { dataDir = Path.GetFullPath(value); }
		}

		// 기존 코드의 startPath 형식: '/' 구분자, 끝에 '/'
		public static string DataDirWithSlash
		{
			get { return string.Concat(dataDir.Replace('\\', '/').TrimEnd('/'), "/"); }
		}
	}
}
