using System.Diagnostics;

namespace BandProgram
{
	public sealed class PbcopyClipboard : IClipboard
	{
		public void SetText(string text)
		{
			ProcessStartInfo psi = new ProcessStartInfo("pbcopy")
			{
				RedirectStandardInput = true,
				UseShellExecute = false
			};
			// LANG이 없으면 pbcopy가 입력을 UTF-8로 해석하지 않아 한글이 들어가지 않는다.
			psi.Environment["LANG"] = "en_US.UTF-8";
			using (Process process = Process.Start(psi))
			{
				process.StandardInput.Write(text);
				process.StandardInput.Close();
				process.WaitForExit();
			}
		}
	}
}
