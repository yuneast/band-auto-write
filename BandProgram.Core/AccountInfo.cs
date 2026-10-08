using System;

namespace BandProgram
{
	public class AccountInfo
	{
		private string id;

		private string pw;

		public AccountInfo()
		{
		}

		public AccountInfo(string id, string pw)
		{
			this.id = id;
			this.pw = pw;
		}

		public string getID()
		{
			return this.id;
		}

		public string getPW()
		{
			return this.pw;
		}

		public void setAccountInfo(string id, string pw)
		{
			this.id = id;
			this.pw = pw;
		}
	}
}