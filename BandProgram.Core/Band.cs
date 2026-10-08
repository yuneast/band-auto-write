using System;

namespace BandProgram
{
	public class Band
	{
		private string name;

		private string cover;

		private int member_count;

		private string band_key;

		public Band()
		{
		}

		public Band(string name, string cover, int member_count, string band_key)
		{
			this.name = name;
			this.cover = cover;
			this.member_count = member_count;
			this.band_key = band_key;
		}

		public string getBandKey()
		{
			return this.band_key;
		}

		public string getCover()
		{
			return this.cover;
		}

		public int getMemberCnt()
		{
			return this.member_count;
		}

		public string getName()
		{
			return this.name;
		}
	}
}