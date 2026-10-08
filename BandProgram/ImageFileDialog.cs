using System.IO;
using System.Windows.Forms;

namespace BandProgram
{
	internal static class ImageFileDialog
	{
		public static ImageFile Show()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog()
			{
				Title = "이미지 파일 불러오기",
				FileName = "",
				Filter = "그림 파일 (*.jpg, *.jpeg, *.gif, *.bmp, *.png) | *.jpg; *.jpeg; *.gif; *.bmp; *.png;"
			};
			DialogResult dialogResult = openFileDialog.ShowDialog();
			if (dialogResult != DialogResult.OK)
			{
				return null;
			}
			string safeFileName = openFileDialog.SafeFileName;
			string fileName = openFileDialog.FileName;
			string str = fileName.Replace(safeFileName, "");
			return new ImageFile(safeFileName, str, (new FileInfo(fileName)).Length);
		}
	}
}
