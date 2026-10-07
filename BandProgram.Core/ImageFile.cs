using System;

namespace BandProgram
{
	public class ImageFile
	{
		private string fileName;

		private string path;

		private long fileSize;

		public ImageFile()
		{
		}

		public ImageFile(string fileName, string path)
		{
			this.fileName = fileName;
			this.path = path;
		}

		public ImageFile(string fileName, string path, long fileSize)
		{
			this.fileName = fileName;
			this.path = path;
			this.fileSize = fileSize;
		}

		public string getFileName()
		{
			return this.fileName;
		}

		public long getFileSize()
		{
			return this.fileSize;
		}

		public string getPath()
		{
			return this.path;
		}
	}
}