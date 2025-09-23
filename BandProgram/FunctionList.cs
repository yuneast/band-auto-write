using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;


namespace BandProgram
{
	internal class FunctionList
	{
		private Util util = Util.getInstance();

		private string startPath = string.Concat(Application.StartupPath.Replace('\\', '/'), "/");

		public int post_type { get; set; }

		public int post_betweenWork;

		public bool post_reservedCheck;

		public bool post_pasteCheck;

		public int post_reserveHour;

		public int post_reserveMin;

		public int post_recCnt { get; set; }

		public int post_recBetweenWork { get; set; }

		public int comment_type;

		public int comment_betweenWork;

		public bool comment_reservedCheck;

		public bool comment_pastCheck;

		public int comment_reserveHour;

		public int comment_reserveMin;

		public int comment_recCnt;

		public int comment_recBetweenWork;

		public int chatting_type;

		public int chatting_betweenWork;

		public bool chatting_reservedCheck;

		public bool chatting_pasteCheck;

		public int chatting_reserveHour;

		public int chatting_reserveMin;

		public int chatting_recCnt;

		public int chatting_recBetweenWork;

		private string band_query;

		private int band_findCnt;

		private int band_minMember;

		private int band_maxMember;

		public FunctionList()
		{
		}

		private bool backToEndWithChattingList(BandInfo band)
		{
			bool flag;
			try
			{
				if (band.chattingList.Count<int>() <= 1)
				{
					flag = false;
				}
				else
				{
					List<int> nums = band.chattingList;
					int num = nums.Count<int>();
					List<int> nums1 = new List<int>();
					for (int i = 1; i < num; i++)
					{
						nums1.Add(nums.ElementAt<int>(i));
					}
					nums1.Add(nums.ElementAt<int>(0));
					band.chattingList = nums1;
					string str = "bandList.txt";
					List<string> strs = this.util.readAll(str);
					this.util.createNotePad(str);
					foreach (string str1 in strs)
					{
						if (!str1.Split(new char[] { '\t' }).ElementAt<string>(1).Equals(band.num))
						{
							this.util.writeStream(str, str1);
						}
						else
						{
							Util.getInstance().writeStream(str, string.Concat(new string[] { band.name, "\t", band.num, "\t", this.intListToString(band.postingList), "\t", this.intListToString(band.commentList), "\t", this.intListToString(band.chattingList) }));
						}
					}
					flag = true;
				}
			}
			catch
			{
				flag = true;
			}
			return flag;
		}

		private bool backToEndWithCommnetList(BandInfo band)
		{
			bool flag;
			try
			{
				if (band.commentList.Count<int>() <= 1)
				{
					flag = false;
				}
				else
				{
					List<int> nums = band.commentList;
					int num = nums.Count<int>();
					List<int> nums1 = new List<int>();
					for (int i = 1; i < num; i++)
					{
						nums1.Add(nums.ElementAt<int>(i));
					}
					nums1.Add(nums.ElementAt<int>(0));
					band.commentList = nums1;
					string str = "bandList.txt";
					List<string> strs = this.util.readAll(str);
					this.util.createNotePad(str);
					foreach (string str1 in strs)
					{
						if (!str1.Split(new char[] { '\t' }).ElementAt<string>(1).Equals(band.num))
						{
							this.util.writeStream(str, str1);
						}
						else
						{
							Util.getInstance().writeStream(str, string.Concat(new string[] { band.name, "\t", band.num, "\t", this.intListToString(band.postingList), "\t", this.intListToString(band.commentList), "\t", this.intListToString(band.chattingList) }));
						}
					}
					flag = true;
				}
			}
			catch
			{
				flag = true;
			}
			return flag;
		}

		private bool backToEndWithPostingList(BandInfo band)
		{
			bool flag;
			try
			{
				if (band.postingList.Count<int>() <= 1)
				{
					flag = false;
				}
				else
				{
					List<int> nums = band.postingList;
					int num = nums.Count<int>();
					List<int> nums1 = new List<int>();
					for (int i = 1; i < num; i++)
					{
						nums1.Add(nums.ElementAt<int>(i));
					}
					nums1.Add(nums.ElementAt<int>(0));
					band.postingList = nums1;
					string str = "bandList.txt";
					List<string> strs = this.util.readAll(str);
					this.util.createNotePad(str);
					foreach (string str1 in strs)
					{
						if (!str1.Split(new char[] { '\t' }).ElementAt<string>(1).Equals(band.num))
						{
							this.util.writeStream(str, str1);
						}
						else
						{
							Util.getInstance().writeStream(str, string.Concat(new string[] { band.name, "\t", band.num, "\t", this.intListToString(band.postingList), "\t", this.intListToString(band.commentList), "\t", this.intListToString(band.chattingList) }));
						}
					}
					flag = true;
				}
			}
			catch
			{
				flag = true;
			}
			return flag;
		}

		public bool closeChrome()
		{
			if (!this.util.closeChrome())
			{
				return false;
			}
			return true;
		}

		public bool createDirectory(string path)
		{
			bool flag;
			try
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(path);
				if (directoryInfo.Exists)
				{
					flag = false;
				}
				else
				{
					directoryInfo.Create();
					flag = true;
				}
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool deleteDirectory(string path)
		{
			bool flag;
			try
			{
				this.util.deleteDir(path);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public string getAuthCode()
		{
			string text;
			try
			{
				this.util.goToUrl("https://auth.band.us/oauth2/authorize?response_type=code&client_id=229705460&redirect_uri=http://naverband.ecpert.com/getToken.php", 1000);
				this.util.clickByCss("[class='chk _chk _all']", 500, 0);
				this.util.clickByCss("[class='btn btn_agree'] [class='lb_wrap']", 500, 0);
				text = this.util.findElement("[id='code']").Text;
			}
			catch
			{
				text = null;
			}
			return text;
		}

		public BandInfo getBandInfoFromUrl(string url)
		{
			BandInfo bandInfo;
			string str = "";
			try
			{
				str = url.Split(new char[] { '?' }).First<string>().Split(new char[] { '/' }).Last<string>();
				string str1 = string.Concat("https://band.us/band/", str);
				MessageBox.Show(str1);
				string str2 = this.util.requestHTTP(str1);
				string str3 = this.util.split(str2, "<title>", "</title>").ElementAt<string>(0);
				bandInfo = new BandInfo()
				{
					name = str3,
					num = str
				};
			}
			catch
			{
				BandInfo bandInfo1 = new BandInfo();
				bandInfo = new BandInfo()
				{
					name = "비공개 밴드",
					num = str
				};
			}
			return bandInfo;
		}

		public List<BandInfo> getBandList(List<BandInfo> preList)
		{
			List<BandInfo> bandInfos;
			try
			{
				if (!this.util.isOpenChrome())
				{
					this.login();
				}
				if (preList != null)
				{
					preList = new List<BandInfo>();
				}
				List<string> strs = new List<string>();
				foreach (BandInfo bandInfo in preList)
				{
					strs.Add(bandInfo.num);
				}

				if (Global.Instance.isTest)
				{
					this.util.goToUrl("http://127.0.0.1:3000/feed.html", 1000);
				}
				else
				{
					this.util.goToUrl("https://band.us/feed/", 1000);
				}



				int num = 0;
				while (num < 5 && this.util.findElements("[class='bandLogo']").Count<IWebElement>() == 0)
				{
					this.util.delay(3000);
					if (num != 5)
					{
						num++;
					}
					else
					{
						bandInfos = null;
						return bandInfos;
					}
				}
				this.util.delay(3000);
				this.util.clickByCss("[class='myBandMoreView _btnMore']", 1500, 0); // 내 밴드 더보기
				this.util.ScrollDown(100, 500);
				List<BandInfo> bandInfos1 = preList;
				foreach (IWebElement webElement in this.util.findElements("[class='feedMyBandList']"))
				{
					string text = "";
					try
					{
						text = this.util.findElement(webElement, "[class='ellipsis']").Text;
					}
					catch
					{
						continue;
					}
					string str = this.util.findElement(webElement, "a").GetAttribute("href").Split(new char[] { '/' }).Last<string>();
					if (strs.Contains(str))
					{
						continue;
					}
					BandInfo bandInfo1 = new BandInfo()
					{
						name = text,
						num = str
					};
					bandInfos1.Add(bandInfo1);
				}
				bandInfos = bandInfos1;
			}
			catch
			{
				bandInfos = null;
			}
			return bandInfos;
		}

		public List<BandInfo> getBandListFromQuery()
		{
			List<BandInfo> bandInfos;
			try
			{
				this.util.goToUrl(string.Concat("https://band.us/discover/search/", this.band_query), 3000);
				int num = int.Parse(this.util.findElement("[class='result _bandPageCount']").Text);
				num = (num > this.band_findCnt ? this.band_findCnt : num);
				IReadOnlyCollection<IWebElement> webElements = this.util.findElements("[class='cCoverList'] li");
				for (int i = webElements.Count<IWebElement>(); i < num; i = webElements.Count<IWebElement>())
				{
					this.util.ScrollDown(100, 500);
					webElements = this.util.findElements("[class='cCoverList'] li");
				}
				List<BandInfo> bandInfos1 = new List<BandInfo>();
				for (int j = 0; j < num; j++)
				{
					num = (num > this.band_findCnt ? this.band_findCnt : num);
					IWebElement webElement = webElements.ElementAt<IWebElement>(j);
					string text = this.util.findElement(webElement, "[class='name'] [class='text _goBand']").Text;
					string strTarget = this.util.findElement(webElement, "[class='bandLink _goBand']").GetAttribute("href");
					string attribute = Regex.Replace(strTarget, @"\D", "");
					int.Parse(this.util.findElement(webElement, "[class='totalNumber']").Text.Replace(",", ""));
					BandInfo bandInfo = new BandInfo()
					{
						name = text,
						num = attribute
					};
					bandInfos1.Add(bandInfo);
				}
				bandInfos = bandInfos1;
			}
			catch
			{
				bandInfos = null;
			}
			return bandInfos;
		}

		public List<BandInfo> getBandListFromQueryWithMemCnt()
		{
			List<BandInfo> bandInfos;
			try
			{
				this.util.goToUrl(string.Concat("https://band.us/discover/search/", this.band_query), 3000);
				int num = int.Parse(this.util.findElement("[class='result _bandPageCount']").Text);
				num = (this.band_findCnt > num ? num : this.band_findCnt);
				IReadOnlyCollection<IWebElement> webElements = this.util.findElements("[class='cCoverList'] li");
				for (int i = webElements.Count<IWebElement>(); i < num; i = webElements.Count<IWebElement>())
				{
					this.util.ScrollDown(100, 500);
					webElements = this.util.findElements("[class='cCoverList'] li");
				}
				List<BandInfo> bandInfos1 = new List<BandInfo>();
				for (int j = 0; j < num; j++)
				{
					num = (num > this.band_findCnt ? this.band_findCnt : num);
					IWebElement webElement = webElements.ElementAt<IWebElement>(j);
					string text = this.util.findElement(webElement, "[class='name'] [class='text _goBand']").Text;
					string strTarget = this.util.findElement(webElement, "[class='bandLink _goBand']").GetAttribute("href");
					string attribute = Regex.Replace(strTarget, @"\D", "");
					int num1 = int.Parse(this.util.findElement(webElement, "[class='totalNumber']").Text.Replace(",", ""));
					if (num1 > this.band_minMember && num1 < this.band_maxMember || this.band_minMember == 0 || this.band_maxMember == 0)
					{
						BandInfo bandInfo = new BandInfo()
						{
							name = text,
							num = attribute
						};
						bandInfos1.Add(bandInfo);
					}
				}
				bandInfos = bandInfos1;
			}
			catch
			{
				bandInfos = null;
			}
			return bandInfos;
		}

		public List<BandInfo> getBandListInFile()
		{
			List<BandInfo> bandInfos;
			try
			{
				string str = "bandList.txt";
				List<BandInfo> bandInfos1 = new List<BandInfo>();
				List<string> strs = this.util.readAll(str);
				if (strs != null)
				{
					foreach (string str1 in strs)
					{
						string[] strArrays = str1.Split(new char[] { '\t' });
						BandInfo bandInfo = new BandInfo()
						{
							name = strArrays[0],
							num = strArrays[1],
							postingList = this.stringToIntList(strArrays[2], ','),
							commentList = this.stringToIntList(strArrays[3], ','),
							chattingList = this.stringToIntList(strArrays[4], ',')
						};
						bandInfos1.Add(bandInfo);
					}
					bandInfos = bandInfos1;
				}
				else
				{
					bandInfos = bandInfos1;
				}
			}
			catch
			{
				bandInfos = new List<BandInfo>();
			}
			return bandInfos;
		}

		public List<Band> getBandListWithToken(string token)
		{
			List<Band> bandList;
			try
			{
				string requestResult = (new APIDAO()).getRequestResult(string.Concat("https://openapi.band.us/v2.1/bands?access_token=", token));
				if (requestResult == null)
				{
					bandList = null;
				}
				else
				{
					bandList = this.jsonToBandList(requestResult);
				}
			}
			catch
			{
				bandList = null;
			}
			return bandList;
		}

		private string getChattingContent(int postingNum)
		{
			string str;
			try
			{
				str = this.util.readAllToString(string.Concat(new object[] { this.startPath, "AutoDoc/Chatting/chatting_", postingNum, "/contents.txt" }));
			}
			catch
			{
				str = null;
			}
			return str;
		}

		private List<ImageFile> getChattingImages(int postingNum)
		{
			List<ImageFile> imageList;
			try
			{
				imageList = Util.getInstance().getImageList(string.Concat(new object[] { this.startPath, "AutoDoc/Chatting/chatting_", postingNum, "/" }));
			}
			catch
			{
				imageList = null;
			}
			return imageList;
		}

		private string getCommentContent(int postingNum)
		{
			string str;
			try
			{
				str = this.util.readAllToString(string.Concat(new object[] { this.startPath, "AutoDoc/Comment/comment_", postingNum, "/contents.txt" }));
			}
			catch
			{
				str = null;
			}
			return str;
		}

		private List<ImageFile> getCommentImages(int postingNum)
		{
			List<ImageFile> imageList;
			try
			{
				imageList = Util.getInstance().getImageList(string.Concat(new object[] { this.startPath, "AutoDoc/Comment/comment_", postingNum, "/" }));
			}
			catch
			{
				imageList = null;
			}
			return imageList;
		}

		public DirectoryInfo[] getDirectoryList(string path)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			if (!directoryInfo.Exists)
			{
				directoryInfo.Create();
			}
			DirectoryInfo[] directoryList = Util.getInstance().getDirectoryList(path);
			if (directoryList != null)
			{
				return directoryList;
			}
			return null;
		}

		private string getPostingContent(int postingNum)
		{
			string str;
			try
			{
				str = this.util.readAllToString(string.Concat(new object[] { this.startPath, "AutoDoc/Posting/post_", postingNum, "/contents.txt" }));
			}
			catch
			{
				str = null;
			}
			return str;
		}
		private string getPostingCommentContent(int postingNum)
		{
			string str;
			try
			{
				str = this.util.readAllToString(string.Concat(new object[] { this.startPath, "AutoDoc/Posting/post_", postingNum, "/comment_contents.txt" }));
			}
			catch
			{
				str = null;
			}
			return str;
		}
		private List<ImageFile> getPostingImages(int postingNum)
		{
			List<ImageFile> imageList;
			try
			{
				imageList = Util.getInstance().getImageList(string.Concat(new object[] { this.startPath, "AutoDoc/Posting/post_", postingNum, "/" }));
			}
			catch
			{
				imageList = null;
			}
			return imageList;
		}

		private List<ImageFile> getPostingCommentImages(int postingNum)
		{
			List<ImageFile> imageList;
			try
			{
				imageList = Util.getInstance().getImageList(string.Concat(new object[] { this.startPath, "AutoDoc/Posting/post_", postingNum, "/comment_images/" }));
			}
			catch
			{
				imageList = null;
			}
			return imageList;
		}
		public List<Post> getPostingList(string path, string separator)
		{

			//Julian 포스트 리스트를 얻는 부분
			List<Post> posts;
			try
			{
				List<Post> posts1 = new List<Post>();
				DirectoryInfo[] directoryList = this.getDirectoryList(path);
				for (int i = 0; i < (int)directoryList.Length; i++)
				{
					DirectoryInfo directoryInfo = directoryList[i];
					if (directoryInfo.Name.Contains(separator))
					{
						Post post = new Post();

						post.idx = int.Parse(directoryInfo.Name.Substring(separator.Count<char>()));
						post.contents = this.util.readAllToString(string.Concat(new object[] { this.startPath, path, "/", separator, post.idx, "/contents.txt" }));
						post.images = this.util.getImageList(string.Concat(new object[] { this.startPath, path, "/", separator, post.idx, "/" }));

						if (separator == "post_")
						{
							post.comment_contents = this.util.readAllToString(string.Concat(new object[] { this.startPath, path, "/", separator, post.idx, "/comment_contents.txt" }));
							post.comment_images = this.util.getImageList(string.Concat(new object[] { this.startPath, path, "/", separator, post.idx, "/comment_images/" }));
							if (post.comment_contents == null || post.comment_contents.Trim() == "")
							{
								post.has_comment = false;
							}
							else
							{
								post.has_comment = true;

							}
						}


						posts1.Add(post);
					}
				}
				posts = posts1;
			}
			catch
			{
				posts = null;
			}
			return posts;
		}

		public int getPostingNum(string path, string separator)
		{
			int i;
			try
			{
				DirectoryInfo[] directoryList = this.getDirectoryList(path);
				List<int> nums = new List<int>();
				DirectoryInfo[] directoryInfoArray = directoryList;
				for (i = 0; i < (int)directoryInfoArray.Length; i++)
				{
					DirectoryInfo directoryInfo = directoryInfoArray[i];
					if (directoryInfo.Name.Contains(separator))
					{
						nums.Add(int.Parse(directoryInfo.Name.Substring(separator.Count<char>())));
					}
				}
				int num = 1;
				nums.Sort();
				foreach (int num1 in nums)
				{
					if (num != num1)
					{
						break;
					}
					num++;
				}
				i = num;
			}
			catch
			{
				i = -1;
			}
			return i;
		}

		public string getToken(string code, string clientId, string clientSecret)
		{
			string item;
			try
			{
				APIDAO aPIDAO = new APIDAO();
				string authorization = aPIDAO.getAuthorization(clientId, clientSecret);
				item = (string)JObject.Parse(aPIDAO.getToken(code, string.Concat("Basic ", authorization)))["access_token"];
			}
			catch
			{
				item = null;
			}
			return item;
		}

		public string getUrl(string fileName)
		{
			string str;
			Util instance = Util.getInstance();
			try
			{
				str = instance.readALine(fileName);
			}
			catch
			{
				str = null;
			}
			return str;
		}

		public string getUserInfo(string token)
		{
			string requestResult;
			try
			{
				requestResult = (new APIDAO()).getRequestResult(string.Concat("https://openapi.band.us/v2/profile?access_token=", token));
			}
			catch
			{
				requestResult = null;
			}
			return requestResult;
		}

		public bool goToPanel()
		{
			bool flag;
			try
			{
				this.util.goToUrl("https://m.naver.com/panel/", 1000);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool goToUrl(string url)
		{
			bool flag;
			Util instance = Util.getInstance();
			try
			{
				instance.goToUrl(url, 1000);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		private string intListToString(List<int> list)
		{
			string str = "";
			if (list != null)
			{
				foreach (int num in list)
				{
					str = (!str.Equals("") ? string.Concat(str, ", ", num.ToString()) : string.Concat(str, num.ToString()));
				}
			}
			return str;
		}

		public bool isDirectory(string path)
		{
			if (!(new DirectoryInfo(path)).Exists)
			{
				return false;
			}
			return true;
		}

		public List<Band> jsonToBandList(string json)
		{
			JObject jObjects = JObject.Parse(json);
			string item = (string)jObjects["result_code"];
			List<Band> bands = new List<Band>();
			foreach (JToken jTokens in (IEnumerable<JToken>)jObjects["result_data"]["bands"])
			{
				string str = jTokens["name"].ToString();
				string str1 = jTokens["cover"].ToString();
				int num = (int)jTokens["member_count"];
				string str2 = jTokens["band_key"].ToString();
				bands.Add(new Band(str, str1, num, str2));
			}
			return bands;
		}

		public Response jsonToStringList(string json)
		{
			JObject jObjects = JObject.Parse(json);
			bool flag = false;
			if (jObjects["result"].ToString().Contains("success"))
			{
				flag = true;
			}
			return new Response(flag, jObjects["message"].ToString());
		}

		public bool login()
		{
			bool flag;
			string bandID = this.util.getBandID();
			string bandPW = this.util.getBandPW();
			string bandType = this.util.getBandType();
			try
			{
				flag = this.login(bandID, bandPW, bandType, true);
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool login(string id, string pw, string type, bool logout = true)
		{
			bool flag;
			try
			{
				this.util.closeChrome();
				this.util.startChrome(0);

				if (Global.Instance.isTest)
				{
					return true;
				}

				this.util.goToUrl("https://band.us", 300);
				while (this.util.findElement("[class='user _settingRegion']") == null && this.util.findElement("[data-viewname='DIntroHeaderView']") == null)
				{
					this.util.delay(500);
				}
				if (this.util.findElement("[class='user _settingRegion']") != null)
				{
					if (!logout)
					{
						flag = true;
						return flag;
					}
					else
					{
						this.util.clickByCss("[class='user _settingRegion']", 500, 0);
						this.util.clickByCss("[class='_btnLogout']", 500, 0);
						this.util.clickByCss("[class='uButton -confirm _btnLogout']", 500, 0);
					}
				}
				
				this.util.goToUrl("https://auth.band.us", 300);
				this.util.delayNext("[class='uButtonRound -h56 -icoType -phone']", 200);
				if (type.Contains("이메일"))
				{
					this.util.delayNext("[class='loginBtnList'] [class='uButtonRound -h56 -icoType -email']", 200);
					this.util.clickByCss("[class='loginBtnList'] [class='uButtonRound -h56 -icoType -email']", 500, 0);
					this.util.sendKey("[id='input_email']", id, 300);
					this.util.sendKey("[id='input_password']", pw, 1300);
					this.util.sendEnter();
				}
				else if (type.Contains("전화번호"))
				{
					this.util.delayNext("[class='loginBtnList'] [class='uButtonRound -h56 -icoType -phone']", 200);
					this.util.clickByCss("[class='loginBtnList'] [class='uButtonRound -h56 -icoType -phone']", 500, 0);
					this.util.sendKey("[id='input_local_phone_number']", id, 300);
					this.util.sendEnter();
					this.util.sendKey("[id='pw']", pw, 300);
					this.util.sendEnter();
				}
				else if (type.Contains("네이버"))
				{
					this.util.delayNext("[class*='uButtonRound -h56 -icoType -naver']", 200);
					this.util.clickByCss("[class*='uButtonRound -h56 -icoType -naver']", 500, 0);
					this.util.delayNext("[id='id']", 200);
					this.util.sendKey("[id='id']", id, 300);
					this.util.sendKey("[id='pw']", pw, 300);
					this.util.sendEnter();
					this.util.clickByCss("[class='btn_unit_on']", 0, 0);
				}
				

				int num = 0;
				while (num < 200)
				{
					this.util.delay(3000);
					if (!this.util.isOpenChrome())
					{
						flag = false;
						return flag;
					}
					else if (this.util.findElements("[class='user _settingRegion']").Count<IWebElement>() <= 0)
					{
						num++;
					}
					else
					{
						flag = true;
						return flag;
					}
				}
				flag = false;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool logout()
		{
			return (new NaverMobile()).logout();
		}

		public bool naverLogin(AccountInfo accountInfo)
		{
			bool flag;
			NaverMobile naverMobile = new NaverMobile();
			try
			{
				flag = naverMobile.login(accountInfo.getID(), accountInfo.getPW());
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool randomScroll(int timeSec)
		{
			bool flag;
			Util instance = Util.getInstance();
			try
			{
				int num = 0;
				while (num < timeSec)
				{
					if (instance.scrollDown(0.05, 0.2, 300, 500))
					{
						num++;
					}
					else
					{
						flag = false;
						return flag;
					}
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public AccountInfo readAccountInfo(string fileName)
		{
			AccountInfo accountInfo;
			try
			{
				string[] strArrays = this.util.readALine(fileName).Split(new char[] { '/' });
				accountInfo = new AccountInfo(strArrays.ElementAt<string>(0), strArrays.ElementAt<string>(1));
			}
			catch
			{
				accountInfo = null;
			}
			return accountInfo;
		}

		public List<BandInfo> removeBand(List<int> indexList)
		{
			List<BandInfo> bandInfos;
			try
			{
				string str = "bandList.txt";
				List<BandInfo> bandInfos1 = new List<BandInfo>();
				List<string> strs = this.util.readAll(str);
				this.util.createNotePad(str);
				int num = 0;
				foreach (string str1 in strs)
				{
					if (!indexList.Contains(num))
					{
						string[] strArrays = str1.Split(new char[] { '\t' });
						BandInfo bandInfo = new BandInfo()
						{
							name = strArrays[0],
							num = strArrays[1],
							postingList = this.stringToIntList(strArrays[2], ','),
							commentList = this.stringToIntList(strArrays[3], ','),
							chattingList = this.stringToIntList(strArrays[4], ',')
						};
						bandInfos1.Add(bandInfo);
						this.util.writeStream(str, string.Concat(new string[] { bandInfo.name, "\t", bandInfo.num, "\t", this.intListToString(bandInfo.postingList), "\t", this.intListToString(bandInfo.commentList), "\t", this.intListToString(bandInfo.chattingList) }));
					}
					num++;
				}
				bandInfos = bandInfos1;
			}
			catch
			{
				bandInfos = null;
			}
			return bandInfos;
		}

		private bool saveImageFile(string targetFile, string newDir, string fileName)
		{
			bool flag;
			try
			{
				File.Copy(targetFile, string.Concat(newDir, fileName), true);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool savePan()
		{
			bool flag;
			try
			{
				flag = this.util.clickByCss("[class='ms_btn_save']", 1000, 0);
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool savePosting(string newDir, string contents, List<ImageFile> imageFileList)
		{
			bool flag;
			try
			{
				this.createDirectory(newDir);
				Util.getInstance().createNotePad(string.Concat(newDir, "contents.txt"));
				Util.getInstance().writeStream(string.Concat(newDir, "contents.txt"), contents);
				List<ImageFile> imageList = Util.getInstance().getImageList(newDir);
				List<string> strs = new List<string>();
				foreach (ImageFile imageFile in imageFileList)
				{
					this.saveImageFile(string.Concat(imageFile.getPath(), imageFile.getFileName()), newDir, imageFile.getFileName());
					strs.Add(imageFile.getFileName());
				}
				foreach (ImageFile imageFile1 in imageList)
				{
					if (strs.Contains(imageFile1.getFileName()) || !File.Exists(string.Concat(newDir, imageFile1.getFileName())))
					{
						continue;
					}
					try
					{
						File.Delete(string.Concat(newDir, imageFile1.getFileName()));
					}
					catch (IOException oException)
					{
						flag = false;
						return flag;
					}
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}
		public bool savePostingWithComment(string newDir, string contents, List<ImageFile> imageFileList, string commentContents, List<ImageFile> commentImageFileList)
		{
			//Julian 포스트자료를 저장하는 부분
			bool flag;
			try
			{
				this.createDirectory(newDir);
				this.createDirectory(newDir + "comment_images//");
				Util.getInstance().createNotePad(string.Concat(newDir, "contents.txt"));
				Util.getInstance().writeStream(string.Concat(newDir, "contents.txt"), contents);


				Util.getInstance().createNotePad(string.Concat(newDir, "comment_contents.txt"));
				Util.getInstance().writeStream(string.Concat(newDir, "comment_contents.txt"), commentContents);




				List<ImageFile> imageList1 = Util.getInstance().getImageList(newDir + "comment_images//");
				List<string> strs1 = new List<string>();
				foreach (ImageFile imageFile in commentImageFileList)
				{
					this.saveImageFile(string.Concat(imageFile.getPath(), imageFile.getFileName()), newDir + "comment_images//", imageFile.getFileName());
					strs1.Add(imageFile.getFileName());
				}
				foreach (ImageFile imageFile1 in imageList1)
				{
					if (strs1.Contains(imageFile1.getFileName()) || !File.Exists(string.Concat(newDir + "comment_images//", imageFile1.getFileName())))
					{
						continue;
					}
					try
					{
						File.Delete(string.Concat(newDir + "comment_images//", imageFile1.getFileName()));
					}
					catch (IOException oException)
					{
						flag = false;
						return flag;
					}
				}






				List<ImageFile> imageList = Util.getInstance().getImageList(newDir);
				List<string> strs = new List<string>();
				foreach (ImageFile imageFile in imageFileList)
				{
					this.saveImageFile(string.Concat(imageFile.getPath(), imageFile.getFileName()), newDir, imageFile.getFileName());
					strs.Add(imageFile.getFileName());
				}
				foreach (ImageFile imageFile1 in imageList)
				{
					if (strs.Contains(imageFile1.getFileName()) || !File.Exists(string.Concat(newDir, imageFile1.getFileName())))
					{
						continue;
					}
					try
					{
						File.Delete(string.Concat(newDir, imageFile1.getFileName()));
					}
					catch (IOException oException)
					{
						flag = false;
						return flag;
					}
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}
		public bool selectedPan(string itemName)
		{
			bool flag;
			try
			{
				foreach (IWebElement webElement in this.util.findElements("[class='ma_list'] li img"))
				{
					if (!webElement.GetAttribute("alt").Contains(itemName))
					{
						continue;
					}
					flag = this.util.clickByElement(webElement, 500, 0);
					return flag;
				}
				this.util.delay(2000);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public void setBandListFromQuery(string query, int findCnt, int minMember, int maxMember)
		{
			this.band_query = query;
			this.band_findCnt = findCnt;
			this.band_minMember = minMember;
			this.band_maxMember = maxMember;
		}

		public void setChattingParam(int type, int betweenWork, bool reservedCheck, bool pasteCheck, int reserveHour, int reserveMin, int recCnt, int recBetweenWork)
		{
			this.chatting_type = type;
			this.chatting_betweenWork = betweenWork;
			this.chatting_reservedCheck = reservedCheck;
			this.chatting_pasteCheck = pasteCheck;
			this.chatting_reserveHour = reserveHour;
			this.chatting_reserveMin = reserveMin;
			this.chatting_recCnt = recCnt;
			this.chatting_recBetweenWork = recBetweenWork;
		}

		public void setCommentParam(int type, int betweenWork, bool reservedCheck, bool pastCheck, int reserveHour, int reserveMin, int recCnt, int recBetweenWork)
		{
			this.comment_type = type;
			this.comment_betweenWork = betweenWork;
			this.comment_reservedCheck = reservedCheck;
			this.comment_pastCheck = pastCheck;
			this.comment_reserveHour = reserveHour;
			this.comment_reserveMin = reserveMin;
			this.comment_recCnt = recCnt;
			this.comment_recBetweenWork = recBetweenWork;
		}

		public void setPostingParam(int type, int betweenWork, bool reservedCheck, bool pasteCheck, int reserveHour, int reserveMin, int recCnt, int recBetweenWork)
		{
			this.post_type = type;
			this.post_betweenWork = betweenWork;
			this.post_reservedCheck = reservedCheck;
			this.post_pasteCheck = pasteCheck;
			this.post_reserveHour = reserveHour;
			this.post_reserveMin = reserveMin;
			this.post_recCnt = recCnt;
			this.post_recBetweenWork = recBetweenWork;
		}

		public ImageFile showFileOpenDialog()
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

		public string signupBand(BandInfo band, string nickName)
		{
			string str;
			try
			{
				this.util.goToUrl(string.Concat("https://band.us/band/", band.num), 3000);
				if (this.util.acceptAlert())
				{
					this.util.acceptAlert();
					this.util.closeCurrent();
				}
				IWebElement webElement = this.util.findElement("[class='uButton -sizeL -confirm _btnJoinBand']");
				if (!this.util.clickByElement(webElement, 500, 0))
				{
					str = "이미 가입한 밴드입니다.";
				}
				else
				{
					string alertText = this.util.getAlertText();
					if (alertText != null)
					{
						str = alertText;
					}
					else if (!webElement.Text.Contains("가입신청 중"))
					{
						if (this.util.findElement("[class='uProfile -size30 -border']") != null)
						{
							this.util.sendKey("[class='_joinAnswer _use_keyup_event']", "네^^", 500);
							this.util.clickByCss("[class='uButton _confirmBtn -confirm']", 500, 0);
						}
						else if (this.util.findElement("[class='main']") == null)
						{
							string alertText1 = this.util.getAlertText();
							this.util.acceptAlert();
							str = alertText1;
							return str;
						}
						this.util.clickByCss("[class='addProfile _newProfileBtn']", 500, 0);
						this.util.sendKey("[class='_nameInput']", nickName, 500);
						this.util.clickByCss("[class='uButton -confirm -sizeL _confirmBtn']", 500, 0);
						for (int i = 0; i < 100 && !this.util.acceptAlert(); i++)
						{
							this.util.delay(50);
						}
						this.util.acceptAlert();
						str = string.Concat(band.name, " 밴드 가입 성공");
					}
					else
					{
						str = "가입 신청 중인 밴드입니다.";
					}
				}
			}
			catch
			{
				str = "가입 실패";
			}
			return str;
		}

		public bool startChatting(FunctionList.Del_PrintLog printLog, FunctionList.Del_PrintLogLeft printLogLeft)
		{
			bool flag;
			try
			{
				for (int i = 0; i < this.chatting_recCnt || this.chatting_recCnt == 0; i++)
				{
					if (i != 0)
					{
						this.util.delay(1000 * this.chatting_recBetweenWork);
					}
					List<Post> postingList = this.getPostingList("AutoDoc/Chatting", "chatting_");
					List<int> nums = new List<int>();
					foreach (Post post in postingList)
					{
						nums.Add(post.idx);
					}
					nums.Sort();
					if (this.chatting_reservedCheck)
					{
						DateTime now = DateTime.Now;
						int num = int.Parse(now.ToString("%H"));
						int num1 = int.Parse(now.ToString("%m"));
						while (num != this.chatting_reserveHour || num1 != this.chatting_reserveMin)
						{
							now = DateTime.Now;
							num = int.Parse(now.ToString("%H"));
							num1 = int.Parse(now.ToString("%m"));
							this.util.delay(5000);
						}
					}
					if (!this.util.isOpenChrome())
					{
						printLogLeft("로그인 중입니다.");
						this.login();
					}
					if (this.chatting_recCnt == 0)
					{
						i = 0;
					}
					int num2 = 0;
					bool flag1 = true;
					foreach (BandInfo bandListInFile in this.getBandListInFile())
					{
						try
						{
							this.util.acceptAlert();
							this.util.closeCurrent();
							bool flag2 = true;
							if (this.chatting_type == 0)
							{
								flag2 = (bandListInFile.chattingList == null ? false : bandListInFile.chattingList.Count > 0);
							}
							else if (this.chatting_type == 1)
							{
								bandListInFile.chattingList.Clear();
								int num3 = (new Random()).Next(0, nums.Count<int>());
								bandListInFile.chattingList.Add(nums[num3]);
							}
							else if (this.chatting_type == 2)
							{
								bandListInFile.chattingList.Clear();
								bandListInFile.chattingList.Add(nums[num2]);
								num2++;
								if (nums.Count<int>() <= num2)
								{
									num2 = 0;
								}
							}
							if (flag2)
							{
								if (!flag1)
								{
									this.util.delay(1000 * this.chatting_betweenWork);
								}
								else
								{
									flag1 = false;
								}
								printLogLeft("url 이동");
								string chattingContent = this.getChattingContent(bandListInFile.chattingList.ElementAt<int>(0));
								this.util.goToUrl(string.Concat("https://band.us/band/", bandListInFile.num), 1000);
								if (this.util.acceptAlert())
								{
									printLog(string.Concat("https://band.us/band/", bandListInFile.num, " - 작업 불가능한 밴드입니다."));
									continue;
								}
								else if (this.util.findElementWithRec("[class='uButton -sizeL -confirm _btnJoinBand']", 3) == null)
								{
									int count = this.util.findElements("[class='chat _bandChattingChannelList'] li [class='_btnOpenChat']").Count;
									if (count <= 0)
									{
										printLogLeft("채팅방에 들어가는 중입니다.");
										this.util.ScrollRight(100, 300);
										this.util.ScrollDown(5, 300);
										this.util.ScrollUp(3000, 300);
										this.util.ScrollUp(3000, 300);
										IWebElement webElement = this.util.findElement("[class='newChat sf_color _btnNewChat']");
										int j = 0;
										for (j = 0; j < 5 && webElement == null; j++)
										{
											this.util.ScrollRight(100, 300);
											this.util.ScrollDown(5, 300);
											this.util.ScrollTo(-10, 500);
											this.util.ScrollUp(3000, 300);
											if (webElement != null)
											{
												break;
											}
											webElement = this.util.findElement("[class='newChat sf_color _btnNewChat']");
										}
										if (j != 5)
										{
											this.util.clickByElementNoScroll(webElement, 300, 3);
											this.util.delay(1500);
											IWebElement webElement1 = this.util.findElementWithRec("[class='lyMenu _lyChatTypes'][style='min-width: 145px; display: block;'] [class='_linkNewPrivateChat']", 2);
											if (webElement1 != null)
											{
												this.util.clickByElementNoScroll(webElement1, 500, 1);
											}
											if (this.util.findElementWithRec("[class='main -tSpaceNone']", 5) != null)
											{
												printLogLeft("멤버 초대 중입니다.");
												this.util.findElementWithRec("[class='listLine']", 10);
												IReadOnlyCollection<IWebElement> webElements = this.util.findElements("[class='listLine'] li");
												if (webElements.Count != 0)
												{
													foreach (IWebElement webElement2 in webElements)
													{
														this.util.clickByElement(webElement2, 0, 0);
													}
													IWebElement webElement3 = this.util.findElementWithRec("[class='uButton -confirm']", 5);
													this.util.clickByElement(webElement3, 1000, 0);
													IWebElement webElement4 = this.util.findElementWithRec("[class='uButton -confirm _btnConfirm']", 5);
													if (webElement3 == null || webElement3 == null)
													{
														printLog("채팅방 개설에 실패하였습니다.");
														continue;
													}
													else
													{
														this.util.clickByElement(webElement4, 1000, 0);
														this.util.switchToLast();
														this.util.findElementWithRec("[class='commentWrite _use_keyup_event']", 10);
														printLogLeft("내용 입력 중입니다.");
														if (!this.chatting_pasteCheck)
														{
															this.util.sendKeyNoDelay("[class='commentWrite _use_keyup_event']", chattingContent, 500);
														}
														else
														{
															this.util.sendKeyPaste("[class='commentWrite _use_keyup_event']", chattingContent);
														}
														this.util.delay(500);
														this.util.sendEnter();
														List<ImageFile> chattingImages = this.getChattingImages(bandListInFile.chattingList.ElementAt<int>(0));
														this.util.delay(500);
														IWebElement webElement5 = this.util.findElementWithRec("[data-uiselector='imageUploadArea'] [type='file']", 5);
														if (webElement5 != null)
														{
															bool flag3 = false;
															foreach (ImageFile chattingImage in chattingImages)
															{
																printLogLeft("이미지 업로드 중입니다.");
																string str = string.Concat(chattingImage.getPath(), chattingImage.getFileName());
																webElement5.SendKeys(str.Replace('/', '\\'));
															}
															this.util.delay(2000);
															int k = 0;
															for (k = 0; k < 100 && this.util.findElement("[data-uiselector='imageUploadButton'][aria-disabled='true']") != null; k++)
															{
																this.util.delay(500);
															}
															if (k < 60)
															{
																this.util.delay(1500);
																if (!flag3)
																{
																	this.util.closeCurrent();
																	if (this.chatting_type == 0)
																	{
																		this.backToEndWithChattingList(bandListInFile);
																	}
																}
																else
																{
																	printLog("업로드가 너무 오래걸립니다.");
																	this.util.closeCurrent();
																	continue;
																}
															}
															else
															{
																flag3 = true;
																break;
															}
														}
														else
														{
															printLog("이미지 업로드에 실패하였습니다.");
															this.util.closeCurrent();
															continue;
														}
													}
												}
												else
												{
													printLog("멤버 없음");
													continue;
												}
											}
											else
											{
												printLog("채팅방을 생성할 수 없습니다.");
												continue;
											}
										}
										else
										{
											printLog("새채팅 버튼을 발견하지 못했습니다.");
											continue;
										}
									}
									else
									{
										for (int l = 0; l < count; l++)
										{
											printLogLeft("채팅방에 들어가는 중입니다.");
											this.util.switchToLast();
											IWebElement webElement6 = this.util.findElements("[class='chat _bandChattingChannelList'] li [class='_btnOpenChat']").ElementAt<IWebElement>(l);
											this.util.clickByElement(webElement6, 1500, 0);
											this.util.clickByElement(webElement6, 1500, 0);
											this.util.clickByElement(webElement6, 1500, 0);
											this.util.switchToLast();
											if (this.util.findElementWithRec("[class='commentWrite _use_keyup_event']", 10) != null)
											{
												printLogLeft("내용 입력 중입니다.");
												if (!this.chatting_pasteCheck)
												{
													this.util.sendKeyNoDelay("[class='commentWrite _use_keyup_event']", chattingContent, 500);
												}
												else
												{
													this.util.sendKeyPaste("[class='commentWrite _use_keyup_event']", chattingContent);
												}
												this.util.delay(500);
												this.util.sendEnter();
												List<ImageFile> imageFiles = this.getChattingImages(bandListInFile.chattingList.ElementAt<int>(0));
												this.util.delay(500);
												IWebElement webElement7 = this.util.findElementWithRec("[data-uiselector='imageUploadArea'] [type='file']", 5);
												if (webElement7 != null)
												{
													bool flag4 = false;
													foreach (ImageFile imageFile in imageFiles)
													{
														printLogLeft("이미지 업로드 중입니다.");
														string str1 = string.Concat(imageFile.getPath(), imageFile.getFileName());
														webElement7.SendKeys(str1.Replace('/', '\\'));
													}
													this.util.delay(2000);
													int m = 0;
													for (m = 0; m < 100 && this.util.findElement("[data-uiselector='imageUploadButton'][aria-disabled='true']") != null; m++)
													{
														this.util.delay(500);
													}
													if (m < 60)
													{
														this.util.delay(1500);
														if (!flag4)
														{
															this.util.closeCurrent();
															if (this.chatting_type == 0)
															{
																this.backToEndWithChattingList(bandListInFile);
															}
														}
														else
														{
															printLogLeft("업로드에 실패하였습니다.");
															printLog("업로드가 너무 오래걸립니다.");
															this.util.closeCurrent();
														}
													}
													else
													{
														flag4 = true;
														goto Label0;
													}
												}
												else
												{
													printLog("이미지 업로드에 실패하였습니다.");
													this.util.closeCurrent();
												}
											}
											else
											{
												printLog("채팅방을 찾을 수 없습니다.");
											}
										}
									}
								Label0:
									printLog(string.Concat("http://band.us/band/", bandListInFile.num, " - 채팅 성공"));
								}
								else
								{
									printLog("가입이 되어있지 않습니다.");
									continue;
								}
							}
						}
						catch
						{
							printLog("예외상황이 발생하여 다음 작업으로 넘어갑니다.");
						}
					}
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool startChrome(int type)
		{
			if (this.util.startChrome(type) == null)
			{
				return false;
			}
			return true;
		}

		public bool startComment(FunctionList.Del_PrintLog printLog, FunctionList.Del_PrintLogLeft printLogLeft)
		{
			bool flag;
			try
			{
				for (int i = 0; i < this.comment_recCnt || this.comment_recCnt == 0; i++)
				{
					if (i != 0)
					{
						this.util.delay(1000 * this.comment_betweenWork);
					}
					List<Post> postingList = this.getPostingList("AutoDoc/Comment", "comment_");
					List<int> nums = new List<int>();
					foreach (Post post in postingList)
					{
						nums.Add(post.idx);
					}
					nums.Sort();
					if (this.comment_reservedCheck)
					{
						DateTime now = DateTime.Now;
						int num = int.Parse(now.ToString("%H"));
						int num1 = int.Parse(now.ToString("%m"));
						while (num != this.comment_reserveHour || num1 != this.comment_reserveMin)
						{
							now = DateTime.Now;
							num = int.Parse(now.ToString("%H"));
							num1 = int.Parse(now.ToString("%m"));
							this.util.delay(5000);
						}
					}
					if (!this.util.isOpenChrome())
					{
						printLogLeft("로그인 중입니다.");
						this.login();
					}
					if (this.comment_recCnt == 0)
					{
						i = 0;
					}
					int num2 = 0;
					bool flag1 = true;
					foreach (BandInfo bandListInFile in this.getBandListInFile())
					{
						try
						{
							this.util.acceptAlert();
							this.util.closeCurrent();
							bool flag2 = true;
							if (this.comment_type == 0)
							{
								flag2 = (bandListInFile.commentList == null ? false : bandListInFile.commentList.Count > 0);
							}
							else if (this.comment_type == 1)
							{
								bandListInFile.commentList.Clear();
								int num3 = (new Random()).Next(0, nums.Count<int>());
								bandListInFile.commentList.Add(nums[num3]);
							}
							else if (this.comment_type == 2)
							{
								bandListInFile.commentList.Clear();
								bandListInFile.commentList.Add(nums[num2]);
								num2++;
								if (nums.Count<int>() <= num2)
								{
									num2 = 0;
								}
							}
							if (flag2)
							{
								if (!flag1)
								{
									this.util.delay(1000 * this.comment_betweenWork);
								}
								else
								{
									flag1 = false;
								}
								string commentContent = this.getCommentContent(bandListInFile.commentList.ElementAt<int>(0));
								printLogLeft("url 이동");
								this.util.goToUrl(string.Concat("https://band.us/band/", bandListInFile.num), 3000);
								if (this.util.acceptAlert())
								{
									printLog(string.Concat("https://band.us/band/", bandListInFile.num, " - 작업 불가능한 밴드입니다."));
									continue;
								}
								else if (this.util.findElementWithRec("[class='uButton -sizeL -confirm _btnJoinBand']", 3) != null)
								{
									printLog("가입이 되어있지 않습니다.");
									continue;
								}
								else if (this.util.clickByCss("[class='postWrap'] [data-viewname='DPostLayoutView'] [class='_commentMainBtn addStatus -comment']", 1000, 0))
								{
									printLogLeft("내용 입력 중입니다.");
									if (!this.comment_pastCheck)
									{
										this.util.sendKeyNoDelay("[class='commentWrite _use_keyup_event _messageTextArea']", commentContent, 0);
									}
									else
									{
										this.util.sendKeyPaste("[class='commentWrite _use_keyup_event _messageTextArea']", commentContent);
									}
									this.util.delay(500);
									List<ImageFile> commentImages = this.getCommentImages(bandListInFile.commentList.ElementAt<int>(0));
									if (commentImages != null && commentImages.Count<ImageFile>() > 0)
									{
										printLogLeft("이미지 업로드 중입니다.");
										this.util.clickByCss("[class='btnUpload _btnUpload']", 300, 0);
										IWebElement webElement = this.util.findElement("[class='_imageUploadArea js-fileapi-wrapper'] [class='_imageUploadButton']");
										int num4 = (new Random()).Next(0, commentImages.Count<ImageFile>());
										ImageFile imageFile = commentImages.ElementAt<ImageFile>(num4);
										string str = string.Concat(imageFile.getPath(), imageFile.getFileName());
										webElement.SendKeys(str.Replace('/', '\\'));
										while (!this.util.findElement("[class='loading _loadingImage']").GetAttribute("style").Contains("display: none;"))
										{
											this.util.delay(100);
										}
									}
									this.util.delay(1000);
									this.util.clickByCss("[class='writeSubmit uButton _sendMessageButton -active']", 500, 0);
									printLog(string.Concat("http://band.us/band/", bandListInFile.num, " - 댓글 성공"));
									if (this.comment_type == 0)
									{
										this.backToEndWithCommnetList(bandListInFile);
									}
								}
								else
								{
									printLog("댓글을 입력할 권한이 없습니다.");
									continue;
								}
							}
						}
						catch
						{
							printLog("예외상황이 발생하여 다음 작업으로 넘어갑니다.");
						}
					}
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool startPosting(FunctionList.Del_PrintLog printLog, FunctionList.Del_PrintLogLeft printLogLeft)
		{
			bool flag;
			try
			{
				List<Post> postingList = this.getPostingList("AutoDoc/Posting", "post_");
				List<int> nums = new List<int>();
				foreach (Post post in postingList)
				{
					nums.Add(post.idx);
				}
				nums.Sort();
				for (int i = 0; i < this.post_recCnt || this.post_recCnt == 0; i++)
				{
					if (i != 0)
					{
						this.util.delay(1000 * this.post_recBetweenWork);
					}
					if (this.post_recCnt == 0)
					{
						i = 0;
					}
					int num = 0;
					bool flag1 = true;
					int counttest = this.getBandListInFile().Count;
					int tempcounter = 0;
					if (this.post_type == 2) // 모든 밴드의 모든 포스트라면
					{
						foreach (Post p in postingList)
						{
							foreach (BandInfo bandListInFile in this.getBandListInFile())
							{
								try
								{
									this.util.acceptAlert();
									this.util.closeCurrent();
									if (!this.util.isOpenChrome())
									{
										printLogLeft("로그인 중입니다.");
										this.login();
									}
									if (this.post_reservedCheck)
									{
										printLogLeft("예약 대기 중입니다.");
										DateTime now = DateTime.Now;
										int num1 = int.Parse(now.ToString("%H"));
										int num2 = int.Parse(now.ToString("%m"));
										while ((num1 != this.post_reserveHour || num2 != this.post_reserveMin) && this.post_reservedCheck)
										{
											now = DateTime.Now;
											num1 = int.Parse(now.ToString("%H"));
											num2 = int.Parse(now.ToString("%m"));
											this.util.delay(5000);
										}
									}
									bool flag2 = true;

									if (flag2)
									{
										if (!flag1)
										{
											this.util.delay(1000 * this.post_betweenWork);
										}
										else
										{
											flag1 = false;
										}
										try
										{
											string postingContent = p.contents;
											printLogLeft("url 이동");
											this.util.goToUrl(string.Concat("https://band.us/band/", bandListInFile.num), 3000);


											if (this.util.acceptAlert())
											{
												printLog(string.Concat("https://band.us/band/", bandListInFile.num, " - 작업 불가능한 밴드입니다."));
												continue;
											}
											else if (this.util.findElementWithRec("[class='uButton -sizeL -confirm _btnJoinBand']", 3) == null)
											{
												printLogLeft("포스팅 내용작성 중입니다.");

												//새로운 소식을 남겨보세요.. 클릭
												if (this.util.clickByCSS_New("[class='cPostWriteEventWrapper _btnOpenWriteLayer']", 1000, 0))
												{
													//에디터 클릭
													this.util.clickByCss("[class='contentEditor _richEditor skin8 cke_editable cke_editable_inline cke_contents_ltr']", 500, 0);
													if (!this.post_pasteCheck)
													{
														this.util.sendKeyNoDelay("[contenteditable='true']", postingContent, 0);
													}
													else
													{
														this.util.sendKeyPaste("[contenteditable='true']", postingContent);
													}
													this.util.delay(500);
													List<ImageFile> postingImages = p.images;//this.getPostingImages(bandListInFile.postingList.ElementAt<int>(0));

													if (postingImages != null && postingImages.Count<ImageFile>() > 0)
													{

														printLogLeft("이미지 업로드 중입니다.");

														Console.WriteLine("이미지 업로드 중입니다.");
														IWebElement webElement = this.util.findElement("[class='photo _btnAttachPhoto _attachWidgetBtn js-fileapi-wrapper'] [type='file']");
														string str = "";
														//이미지업로드버튼
														foreach (ImageFile postingImage in postingImages)
														{
															str += string.Concat(postingImage.getPath(), postingImage.getFileName());
															str += " \n ";
														}
														str = str.Substring(0, str.Length - 3);
														webElement.SendKeys(str);
														
														//webElement.SendKeys(str.Replace('/', '\\'));

														this.util.delay(500);
														while (!this.util.getPageSource().Contains("uButton -confirm _submitBtn"))
														{
															this.util.delay(100);
														}
														this.util.delay(500);
														this.util.clickByCSS_New("[class='modalFooter'] [class='uButton -confirm _submitBtn']", 500, 0);
														

														while (this.util.findElement("[class='uButton -sizeM _btnSubmitPost -confirm']") == null)
														{
															this.util.delay(100);
														}
													}
													this.util.delay(2000);
													//this.util.clickByCss("[class='uButton -sizeM _btnSubmitPost -confirm']", 500, 0);
													this.util.clickByCSS_New("[class='uButton -sizeM _btnSubmitPost -confirm']", 500, 0);

													this.util.delay(2000);
													if (this.post_type == 0)
													{

														this.backToEndWithPostingList(bandListInFile);
													}
													printLog(string.Concat("http://band.us/band/", bandListInFile.num, " - 포스팅 성공"));
												}
												else
												{
													printLog(string.Concat("https://band.us/band/", bandListInFile.num, " - 포스팅을 작성할 권한이 없습니다."));
													continue;
												}
											}
											else
											{
												printLog("가입이 되어있지 않습니다.");
												continue;
											}






											// ===========================================


											//포스트 댓글 얻기 1 

											string postingCommentContent = p.comment_contents;

											if (!string.IsNullOrWhiteSpace(postingCommentContent))
											{

												printLogLeft("댓글 준비중입니다.");
												this.util.delay(2000);

												//댓글 포스트

												//if (this.util.clickByCss("[class='postWrap'] [data-viewname='DPostLayoutView'] [class='_commentMainBtn addStatus -comment']", 1000, 0))
												if (this.util.clickByCSS_New("[class='_commentMainBtn addStatus -comment']", 1000, 0))
												{
													printLogLeft("댓글내용 입력 중입니다.");
													if (!this.post_pasteCheck)
													{
														this.util.sendKeyNoDelay("[class='commentWrite _use_keyup_event _messageTextArea']", postingCommentContent, 0);
													}
													else
													{
														this.util.sendKeyPaste("[class='commentWrite _use_keyup_event _messageTextArea']", postingCommentContent);
													}
													this.util.delay(500);
													List<ImageFile> commentImages = this.getPostingCommentImages(p.idx);
													if (commentImages != null && commentImages.Count<ImageFile>() > 0)
													{


														printLogLeft("이미지 업로드 중입니다.");

														this.util.clickByCSS_New("[class='btnUpload _btnUpload']", 300, 0);
														IWebElement webElement = this.util.findElement("[class='inputUploadFile _imageUploadButton']");
														string str = "";
														//이미지업로드버튼
														foreach (ImageFile comImage in commentImages)
														{
															str += string.Concat(comImage.getPath(), comImage.getFileName());
															str += " \n ";
														}
														str = str.Substring(0, str.Length - 3);
														webElement.SendKeys(str);


													}
													this.util.delay(1000);
													this.util.clickByCSS_New("[class='writeSubmit uButton _sendMessageButton -active']", 500, 0);


													printLog(string.Concat("http://band.us/band/", bandListInFile.num, " - 댓글 성공"));
													if (this.comment_type == 0)
													{
														this.backToEndWithCommnetList(bandListInFile);
													}
												}
											}


											// ============================================





										}
										catch (Exception ex)
										{
											//MessageBox.Show(ex.ToString());
											printLog(string.Concat("https://band.us/band/", bandListInFile.num, " - 작업 불가능한 밴드입니다."));
											this.util.acceptAlert();
											this.util.closeCurrent();
											continue;
										}
									}
								}
								catch
								{
									printLog("예외상황이 발생하여 다음 작업으로 넘어갑니다.");
								}
							}
						}
					}
					else
					{
						foreach (BandInfo bandListInFile in this.getBandListInFile())
						{
							try
							{
								this.util.acceptAlert();
								this.util.closeCurrent();
								if (!this.util.isOpenChrome())
								{
									printLogLeft("로그인 중입니다.");
									this.login();
								}
								if (this.post_reservedCheck)
								{
									printLogLeft("예약 대기 중입니다.");
									DateTime now = DateTime.Now;
									int num1 = int.Parse(now.ToString("%H"));
									int num2 = int.Parse(now.ToString("%m"));
									while ((num1 != this.post_reserveHour || num2 != this.post_reserveMin) && this.post_reservedCheck)
									{
										now = DateTime.Now;
										num1 = int.Parse(now.ToString("%H"));
										num2 = int.Parse(now.ToString("%m"));
										this.util.delay(5000);
									}
								}
								bool flag2 = true;
								if (this.post_type == 0)
								{
									flag2 = (bandListInFile.postingList == null ? false : bandListInFile.postingList.Count > 0);
								}
								else if (this.post_type == 1)
								{
									bandListInFile.postingList.Clear();
									int num3 = (new Random()).Next(0, nums.Count<int>());
									bandListInFile.postingList.Add(nums[num3]);
								}
								//else if (this.post_type == 2)
								//{
								//    //tempcounter++;
								//    //if (tempcounter >= counttest)
								//    //{
								//    //    bandListInFile.postingList.Clear();
								//    //    bandListInFile.postingList.Add(nums[num]);
								//    //    num++;
								//    //    if (nums.Count<int>() <= num)
								//    //    {
								//    //        num = 0;
								//    //    }
								//    //}
								//}
								if (flag2)
								{
									if (!flag1)
									{
										this.util.delay(1000 * this.post_betweenWork);
									}
									else
									{
										flag1 = false;
									}
									try
									{
										//포스트 내용얻기
										string postingContent = this.getPostingContent(bandListInFile.postingList.ElementAt<int>(0));

										//포스트 댓글 얻기
										string postingCommentContent = this.getPostingCommentContent(bandListInFile.postingList.ElementAt<int>(0));


										printLogLeft("url 이동");
										this.util.goToUrl(string.Concat("https://band.us/band/", bandListInFile.num), 3000);


										if (this.util.acceptAlert())
										{
											printLog(string.Concat("https://band.us/band/", bandListInFile.num, " - 작업 불가능한 밴드입니다."));
											continue;
										}
										else if (this.util.findElementWithRec("[class='uButton -sizeL -confirm _btnJoinBand']", 3) == null)
										{
											printLogLeft("포스팅 내용작성 중입니다.");
											if (this.util.clickByCSS_New("[class='cPostWriteEventWrapper _btnOpenWriteLayer']", 1000, 0))
											{
												this.util.clickByCss("[class='contentEditor _richEditor skin8 cke_editable cke_editable_inline cke_contents_ltr']", 500, 0);
												if (!this.post_pasteCheck)
												{
													this.util.sendKeyNoDelay("[contenteditable='true']", postingContent, 0);
												}
												else
												{
													this.util.sendKeyPaste("[contenteditable='true']", postingContent);
												}
												this.util.delay(500);
												List<ImageFile> postingImages = this.getPostingImages(bandListInFile.postingList.ElementAt<int>(0));

												if (postingImages != null && postingImages.Count<ImageFile>() > 0)
												{
													printLogLeft("이미지 업로드 중입니다.");
													IWebElement webElement = this.util.findElement("[class='photo _btnAttachPhoto _attachWidgetBtn js-fileapi-wrapper'] [type='file']");
													//foreach (ImageFile postingImage in postingImages)
													//{
													//	string str = string.Concat(postingImage.getPath(), postingImage.getFileName());
													//	webElement.SendKeys(str.Replace('/', '\\'));
													//}



													string str = "";
													//이미지업로드버튼
													foreach (ImageFile postingImage in postingImages)
													{
														str += string.Concat(postingImage.getPath(), postingImage.getFileName());
														str += " \n ";
													}
													str = str.Substring(0, str.Length - 3);
													webElement.SendKeys(str);




													while (!this.util.getPageSource().Contains("uButton -confirm _submitBtn"))
													{
														this.util.delay(100);
													}
													this.util.delay(500);
													this.util.clickByCss("[class='modalFooter'] [class='uButton -confirm _submitBtn']", 500, 0);
													while (this.util.findElement("[class='uButton -sizeM _btnSubmitPost -confirm']") == null)
													{
														this.util.delay(100);
													}
												}
												this.util.delay(2000);
												//this.util.clickByCss("[class='uButton -sizeM _btnSubmitPost -confirm']", 500, 0);

												this.util.clickByCSS_New("[class='uButton -sizeM _btnSubmitPost -confirm']", 1000, 0);

												if (this.post_type == 0)
												{

													this.backToEndWithPostingList(bandListInFile);
												}
												printLog(string.Concat("http://band.us/band/", bandListInFile.num, " - 포스팅 성공"));
											}
											else
											{
												printLog(string.Concat("https://band.us/band/", bandListInFile.num, " - 포스팅을 작성할 권한이 없습니다."));
												continue;
											}
										}
										else
										{
											printLog("가입이 되어있지 않습니다.");
											continue;
										}


										if (!string.IsNullOrWhiteSpace(postingCommentContent))
										{
											printLogLeft("댓글 준비중입니다.");
											this.util.delay(2000);

											//댓글 포스트

											//if (this.util.clickByCss("[class='postWrap'] [data-viewname='DPostLayoutView'] [class='_commentMainBtn addStatus -comment']", 1000, 0))
											if (this.util.clickByCSS_New("[class='_commentMainBtn addStatus -comment']", 1000, 0))
											{
												printLogLeft("댓글내용 입력 중입니다.");
												if (!this.post_pasteCheck)
												{
													this.util.sendKeyNoDelay("[class='commentWrite _use_keyup_event _messageTextArea']", postingCommentContent, 0);
												}
												else
												{
													this.util.sendKeyPaste("[class='commentWrite _use_keyup_event _messageTextArea']", postingCommentContent);
												}
												this.util.delay(500);
												List<ImageFile> commentImages = this.getPostingCommentImages(bandListInFile.commentList.ElementAt<int>(0));
												if (commentImages != null && commentImages.Count<ImageFile>() > 0)
												{
													printLogLeft("이미지 업로드 중입니다.");
													this.util.clickByCss("[class='btnUpload _btnUpload']", 300, 0);
													IWebElement webElement = this.util.findElement("[class='inputUploadFile _imageUploadButton']");
													int num4 = (new Random()).Next(0, commentImages.Count<ImageFile>());
													ImageFile imageFile = commentImages.ElementAt<ImageFile>(num4);
													string str = string.Concat(imageFile.getPath(), imageFile.getFileName());
													webElement.SendKeys(str.Replace('/', '\\'));
													while (!this.util.findElement("[class='loading _loadingImage']").GetAttribute("style").Contains("display: none;"))
													{
														this.util.delay(100);
													}
												}
												this.util.delay(1000);
												this.util.clickByCSS_New("[class='writeSubmit uButton _sendMessageButton -active']", 500, 0);


												printLog(string.Concat("http://band.us/band/", bandListInFile.num, " - 댓글 성공"));
												if (this.comment_type == 0)
												{
													this.backToEndWithCommnetList(bandListInFile);
												}
											}
										}

									}
									catch (Exception ex)
									{
										printLog(string.Concat("https://band.us/band/", bandListInFile.num, " - 작업 불가능한 밴드입니다."));
										this.util.acceptAlert();
										this.util.closeCurrent();
										continue;
									}
								}
							}
							catch
							{
								printLog("예외상황이 발생하여 다음 작업으로 넘어갑니다.");
							}
						}
					}
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		private List<int> stringToIntList(string str, char sep)
		{
			if (str == null)
			{
				return null;
			}
			string[] strArrays = str.Split(new char[] { sep });
			List<int> nums = new List<int>();
			string[] strArrays1 = strArrays;
			for (int i = 0; i < (int)strArrays1.Length; i++)
			{
				string str1 = strArrays1[i];
				try
				{
					nums.Add(int.Parse(str1));
				}
				catch
				{
				}
			}
			return nums;
		}

		public delegate void Del_PrintLog(string Msg);

		public delegate void Del_PrintLogLeft(string Msg);
	}
}