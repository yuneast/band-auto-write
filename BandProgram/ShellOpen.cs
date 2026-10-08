using System.Diagnostics;

namespace BandProgram
{
	// .NET 10의 Process.Start(string)은 UseShellExecute=false라 폴더·이미지를 열지 못한다.
	internal static class ShellOpen
	{
		public static void Open(string path)
		{
			Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
		}
	}
}
