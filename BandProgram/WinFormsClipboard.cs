using System.Threading;
using System.Windows.Forms;

namespace BandProgram
{
	internal sealed class WinFormsClipboard : IClipboard
	{
		public void SetText(string text)
		{
			Thread thread = new Thread(() => Clipboard.SetText(text));
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();
		}
	}
}
