using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace BandProgram
{
	public class Util
	{
		private string bandid;

		private string bandpw;

		private string bandtype;

		private string leftDate;

		private static Util instance;

		// Windows는 WinForms Program이 WinFormsClipboard를 넣는다.
		public static IClipboard Clipboard { get; set; } = OperatingSystem.IsMacOS() ? new PbcopyClipboard() : null;

		private ChromeDriver driver;
		IJavaScriptExecutor executor;

		private Random rd = new Random();

		private static readonly HttpClient http = new HttpClient();

		public Util()
		{
		}

		public bool acceptAlert()
		{
			bool flag;
			try
			{
				this.driver.SwitchTo().Alert().Accept();
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public string Base64Decoding(string DecodingText, Encoding oEncoding = null)
		{
			if (DecodingText == null)
			{
				return null;
			}
			if (oEncoding == null)
			{
				oEncoding = Encoding.UTF8;
			}
			return oEncoding.GetString(Convert.FromBase64String(DecodingText));
		}

		public string Base64Encoding(string EncodingText, Encoding oEncoding = null)
		{
			if (oEncoding == null)
			{
				oEncoding = Encoding.UTF8;
			}
			return Convert.ToBase64String(oEncoding.GetBytes(EncodingText));
		}

		public int calculateTime(string time)
		{
			int num = 0;
			if (time.Contains("초"))
			{
				num = int.Parse(time.Replace("초", ""));
			}
			else if (time.Contains("분"))
			{
				num = int.Parse(time.Replace("분", ""));
				num *= 60;
			}
			else if (time.Contains("시간"))
			{
				num = int.Parse(time.Replace("시간", ""));
				num *= 3600;
			}
			return num;
		}

		public string changeIP(string appID)
		{
			string str;
			try
			{
				str = this.changeIPwithVM(this.getMyIp(), appID);
			}
			catch
			{
				str = null;
			}
			return str;
		}

		public string changeIP(string preIP, string appId)
		{
			ADB instnace = ADB.getInstnace();
			instnace.changeIpWithADB(instnace.getDeivces()[0]);
			Util.getInstance().delay(2000);
			string myIp = this.getMyIp();
			if (!preIP.Equals(myIp) && myIp != "")
			{
				return this.getMyIp();
			}
			return this.changeIP(preIP, appId);
		}

		private string changeIPwithVM(string preIP, string appId)
		{
			string str;
			try
			{
				this.requestHTTP(string.Concat("http://110.10.189.224/ipChange/change.php?id=", appId));
				int num = 6;
				int num1 = 0;
				while (num1 < num)
				{
					this.delay(2000);
					string myIp = this.getMyIp();
					if (preIP.Equals(myIp) || !(myIp != ""))
					{
						num1++;
					}
					else
					{
						str = string.Concat(preIP, "->", myIp);
						return str;
					}
				}
				str = this.changeIPwithVM(preIP, appId);
			}
			catch
			{
				str = null;
			}
			return str;
		}

		public bool clickByCss(string css, int sleepTimeMS = 0, int failCnt = 0)
		{
			bool flag;
			try
			{
				if (failCnt <= 5)
				{
					IWebElement webElement = this.findElement(css);
					if (webElement == null)
					{
						flag = this.clickByCss(css, sleepTimeMS, failCnt + 1);
					}
					else
					{
						this.scrollTo(webElement);
						this.randomClick(webElement, sleepTimeMS, 1);
						flag = true;
					}
				}
				else
				{
					flag = false;
				}
			}
			catch
			{
				flag = this.clickByCss(css, sleepTimeMS, failCnt + 1);
			}
			return flag;
		}

		public bool clickByElement(IWebElement element, int sleepTimeMS = 0, int failCnt = 0)
		{
			bool flag;
			try
			{
				if (failCnt <= 5)
				{
					this.scrollTo(element);
					this.randomClick(element, sleepTimeMS, 1);
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
			catch
			{
				flag = this.clickByElement(element, sleepTimeMS, failCnt + 1);
			}
			return flag;
		}


		public bool clickByCSS_New(string css, int timeDelayMS, int test)
        {
			IWebElement webElement = this.findElement(css);
			executor.ExecuteScript("arguments[0].click();", webElement);
			delay(timeDelayMS);
			return true;
		}
		public bool clickByElementNoScroll(IWebElement element, int sleepTimeMS = 0, int rec_cnt = 1)
		{
			bool flag;
			try
			{
				this.randomClick(element, sleepTimeMS, rec_cnt);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool closeChrome()
		{
			bool flag;
			if (this.driver == null)
			{
				return true;
			}
			try
			{
				if (this.driver.WindowHandles != null)
				{
					foreach (string windowHandle in this.driver.WindowHandles)
					{
						this.driver.SwitchTo().Window(windowHandle);
						this.driver.Close();
					}
					this.driver.Quit();
				}
				Process[] processesByName = Process.GetProcessesByName("chromedriver");
				for (int i = 0; i < (int)processesByName.Length; i++)
				{
					processesByName[i].Kill();
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool closeCurrent()
		{
			bool flag;
			try
			{
				if (this.driver.WindowHandles.Count<string>() > 1)
				{
					this.driver.Close();
					this.switchToLast();
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public void createNotePad(string fileName)
		{
			FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
			StreamWriter streamWriter = new StreamWriter(fileStream, AppText.Encoding);
			streamWriter.Flush();
			streamWriter.Close();
			fileStream.Close();
		}

		public string currentUrl()
		{
			return this.driver.Url;
		}

		public DateTime delay(int MS)
		{
			WorkControl.Checkpoint();
			int remaining = MS;
			while (remaining > 0)
			{
				int slice = Math.Min(remaining, WorkControl.SliceMs);
				Thread.Sleep(slice);
				remaining -= slice;
				WorkControl.Checkpoint();
			}
			return DateTime.Now;
		}

		public bool delayNext(string query, int sleepTimeSec)
		{
			bool flag;
			try
			{
				for (int i = 0; i < sleepTimeSec && this.findElement(query) == null; i++)
				{
					this.delay(1000);
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public void deleteDir(string Path)
		{
			try
			{
				FileInfo[] files = (new DirectoryInfo(Path)).GetFiles("*.*", SearchOption.AllDirectories);
				for (int i = 0; i < (int)files.Length; i++)
				{
					files[i].Attributes = FileAttributes.Normal;
				}
				Directory.Delete(Path, true);
			}
			catch
			{
			}
		}

		public bool dismissAlert()
		{
			bool flag;
			try
			{
				this.driver.SwitchTo().Alert().Dismiss();
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public void enterFrame(string str)
		{
			this.driver.SwitchTo().Frame(this.findElement(str));
		}

		public void escapeFrame()
		{
			this.driver.SwitchTo().ParentFrame();
		}

		private void executeJS(string text)
		{
			((IJavaScriptExecutor)this.driver).ExecuteScript(text, new object[0]);
		}

		private void executeJS(string text, object[] parameters = null)
		{
			((IJavaScriptExecutor)this.driver).ExecuteScript(text, parameters);
		}

		public IWebElement findElement(string str)
		{
			IWebElement webElement;
			try
			{
				webElement = this.driver.FindElement(By.CssSelector(str));
			}
			catch
			{
				webElement = null;
				SelectorTrace.Miss(str, this.driver);
			}
			return webElement;
		}

		public IWebElement findElement(IWebElement element, string str)
		{
			IWebElement webElement;
			try
			{
				webElement = element.FindElement(By.CssSelector(str));
			}
			catch
			{
				webElement = null;
				SelectorTrace.Miss(str, this.driver);
			}
			return webElement;
		}

		public IReadOnlyCollection<IWebElement> findElements(string str)
		{
			IReadOnlyCollection<IWebElement> webElements = this.driver.FindElements(By.CssSelector(str));
			if (webElements.Count == 0)
			{
				SelectorTrace.Miss(str, this.driver);
			}
			return webElements;
		}

		public IReadOnlyCollection<IWebElement> findElementsWithXPath(string str)
		{
			IReadOnlyCollection<IWebElement> webElements = this.driver.FindElements(By.XPath(str));
			if (webElements.Count == 0)
			{
				SelectorTrace.Miss(str, this.driver);
			}
			return webElements;
		}

		public IWebElement findElementByID(string str)
        {
			return this.driver.FindElement(By.Id(str));
		}
		public IWebElement findElementWithRec(string css, int recCnt)
		{
			IWebElement webElement;
			try
			{
				IWebElement webElement1 = this.findElement(css);
				for (int i = 0; webElement1 == null && i < recCnt; i++)
				{
					this.delay(500);
				}
				this.delay(500);
				webElement = webElement1;
			}
			catch
			{
				webElement = null;
			}
			return webElement;
		}

		public void firstLineToBack(string fileName)
		{
			List<string> strs = this.readAll(fileName);
			FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
			StreamWriter streamWriter = new StreamWriter(fileStream, AppText.Encoding);
			int num = strs.Count<string>();
			for (int i = 1; i < num; i++)
			{
				streamWriter.WriteLine(strs[i]);
			}
			streamWriter.WriteLine(strs[0]);
			streamWriter.Flush();
			streamWriter.Close();
			fileStream.Close();
		}

		public string getAlertText()
		{
			string text;
			try
			{
				text = this.driver.SwitchTo().Alert().Text;
			}
			catch
			{
				text = null;
			}
			return text;
		}

		public string getBandID()
		{
			return this.bandid;
		}

		public string getBandPW()
		{
			return this.bandpw;
		}

		public string getBandType()
		{
			return this.bandtype;
		}

		public DirectoryInfo[] getDirectoryList(string path)
		{
			DirectoryInfo[] directories;
			try
			{
				directories = (new DirectoryInfo(path)).GetDirectories();
			}
			catch
			{
				directories = null;
			}
			return directories;
		}

		public ChromeDriver getDriver()
		{
			return this.driver;
		}

		public List<ImageFile> getImageList(string path)
		{
			List<ImageFile> imageFiles;
			try
			{
				List<ImageFile> imageFiles1 = new List<ImageFile>();
				FileInfo[] files = (new DirectoryInfo(path)).GetFiles();
				for (int i = 0; i < (int)files.Length; i++)
				{
					FileInfo fileInfo = files[i];
					if (fileInfo.Extension.ToLower() == ".jpg" || fileInfo.Extension.ToLower() == ".jpeg" || fileInfo.Extension.ToLower() == ".gif" || fileInfo.Extension.ToLower() == ".bmp" || fileInfo.Extension.ToLower() == ".png")
					{
						long length = (new FileInfo(fileInfo.FullName)).Length;
						imageFiles1.Add(new ImageFile(fileInfo.Name, path, length));
					}
				}
				imageFiles = imageFiles1;
			}
			catch
			{
				imageFiles = null;
			}
			return imageFiles;
		}

		public static Util getInstance()
		{
			if (Util.instance == null)
			{
				Util.instance = new Util();
			}
			return Util.instance;
		}

		public string getMyIp()
		{
			return this.requestHTTP("http://ektsn.dothome.co.kr/myip.php");
		}

		public string getPageSource()
		{
			return this.driver.PageSource;
		}

		public List<string> getTextList(string path)
		{
			List<string> strs;
			try
			{
				List<string> strs1 = new List<string>();
				FileInfo[] files = (new DirectoryInfo(path)).GetFiles();
				for (int i = 0; i < (int)files.Length; i++)
				{
					FileInfo fileInfo = files[i];
					if (fileInfo.Extension.ToLower() == ".txt")
					{
						strs1.Add(fileInfo.Name);
					}
				}
				strs = strs1;
			}
			catch
			{
				strs = null;
			}
			return strs;
		}

		public void goToUrl(string url, int sleepTimeMS = 1000)
		{
			this.driver.Navigate().GoToUrl(url);
			this.writeLog("band", url);
			this.delay(sleepTimeMS);
		}

		public bool isOpenChrome()
		{
			bool flag;
			try
			{
				this.switchToLast();
				flag = (this.driver.WindowHandles.Count<string>() <= 0 ? false : true);
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool mouseHover(string css, int sleepTimeSec)
		{
			bool flag;
			try
			{
				IWebElement webElement = this.findElement(css);
				this.scrollTo(webElement);
				this.randomHover(webElement, sleepTimeSec);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public void navBack(int SleepTime = 0)
		{
			this.driver.Navigate().Back();
			this.delay(SleepTime * 1000);
		}

		public void navFoward(int SleepTime = 0)
		{
			this.driver.Navigate().Forward();
			this.delay(SleepTime * 1000);
		}

		private bool randomClick(IWebElement element, int sleepTimeMS, int rec_cnt = 1)
		{
			bool flag;
			try
			{
				Actions action = new Actions(this.driver);
				Random random = new Random();
				Size size = element.Size;
				int width = (int)((double)size.Width * 0.4);
				size = element.Size;
				int height = (int)((double)size.Height * 0.4);
				size = element.Size;
				int num = (int)((double)size.Width * 0.7);
				size = element.Size;
				int height1 = (int)((double)size.Height * 0.7);
				action.MoveToElement(element, random.Next(width, num), random.Next(height, height1));
				action.Click();
				action.Build();
				for (int i = 0; i < rec_cnt; i++)
				{
					action.Perform();
					this.delay(sleepTimeMS);
				}
				flag = true;
			}
			catch (NoSuchElementException noSuchElementException)
			{
				flag = false;
			}
			return flag;
		}

		private bool randomHover(IWebElement element, int sleepTimeMS = 0)
		{
			bool flag;
			try
			{
				((IJavaScriptExecutor)this.driver).ExecuteScript("arguments[0].scrollIntoView();", new object[] { element });
				this.delay(500);
				Actions action = new Actions(this.driver);
				Random random = new Random();
				Size size = element.Size;
				int width = (int)((double)size.Width * 0.4);
				size = element.Size;
				int height = (int)((double)size.Height * 0.4);
				size = element.Size;
				int num = (int)((double)size.Width * 0.7);
				size = element.Size;
				int height1 = (int)((double)size.Height * 0.7);
				action.MoveToElement(element, random.Next(width, num), random.Next(height, height1));
				action.Build();
				action.Perform();
				this.delay(sleepTimeMS);
				flag = true;
			}
			catch (NoSuchElementException noSuchElementException)
			{
				flag = false;
			}
			return flag;
		}

		public string randomString(int length)
		{
			string str;
			try
			{
				string str1 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
				int num = str1.Count<char>();
				string str2 = "";
				for (int i = 0; i < length; i++)
				{
					int num1 = this.rd.Next(0, num);
					char chr = str1.ElementAt<char>(num1);
					str2 = string.Concat(str2, chr.ToString());
				}
				str = str2;
			}
			catch
			{
				str = null;
			}
			return str;
		}

		public string readALine(string fileName)
		{
			string str;
			try
			{
				string str1 = "";
				StreamReader streamReader = new StreamReader(fileName, AppText.Encoding);
				str1 = streamReader.ReadLine();
				streamReader.Close();
				str = str1;
			}
			catch
			{
				str = null;
			}
			return str;
		}

		public List<string> readAll(string fileName)
		{
			List<string> strs;
			try
			{
				List<string> strs1 = new List<string>();
				string str = "";
				StreamReader streamReader = new StreamReader(fileName, AppText.Encoding);
				while (str != null)
				{
					str = streamReader.ReadLine();
					if (!(str != "") || str == null)
					{
						continue;
					}
					strs1.Add(str);
				}
				streamReader.Close();
				strs = strs1;
			}
			catch (Exception exception)
			{
				strs = null;
			}
			return strs;
		}

		public string readAllToString(string fileName)
		{
			string str;
			try
			{
				string str1 = "";
				string str2 = "";
				StreamReader streamReader = new StreamReader(fileName, AppText.Encoding);
				while (str2 != null)
				{
					str2 = streamReader.ReadLine();
					if (str2 == null)
					{
						continue;
					}
					str1 = string.Concat(str1, str2, Environment.NewLine);
				}
				streamReader.Close();
				str = str1;
			}
			catch (Exception exception)
			{
				str = null;
			}
			return str;
		}

		public void removeALine(string fileName, int idx)
		{
			List<string> strs = this.readAll(fileName);
			this.createNotePad(fileName);
			int num = 0;
			foreach (string str in strs)
			{
				if (num != idx)
				{
					this.writeStream(fileName, str);
					num++;
				}
				else
				{
					num++;
				}
			}
		}

		public string requestHTTP(string url)
		{
			using (HttpResponseMessage response = http.GetAsync(url).GetAwaiter().GetResult())
			{
				return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			}
		}

		public string requestHTTP(string url, Dictionary<string, string> parameters)
		{
			string str = "";
			bool flag = true;
			foreach (KeyValuePair<string, string> parameter in parameters)
			{
				if (!flag)
				{
					str = string.Concat(new string[] { str, "&", parameter.Key, "=", parameter.Value });
				}
				else
				{
					str = string.Concat(str, parameter.Key, "=", parameter.Value);
					flag = false;
				}
			}
			return this.requestHTTP(string.Concat(url, "?", str));
		}

		public bool scrollDown(double minPer, double maxPer, int sleepMinMS, int sleepMaxMS)
		{
			bool flag;
			try
			{
				Random random = new Random();
				minPer *= 100;
				maxPer *= 100;
				int num = random.Next((int)minPer, (int)maxPer);
				this.executeJS(string.Concat("window.scrollBy(0, document.body.scrollHeight * ", (double)num / 100, ");"));
				this.delay(random.Next(sleepMinMS, sleepMaxMS));
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool ScrollDown(int per, int sleepTimeMS)
		{
			bool flag;
			try
			{
				this.executeJS(string.Concat("window.scrollBy(0, document.body.scrollHeight * ", (double)per / 100, ");"));
				this.delay(sleepTimeMS);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool ScrollRight(int per, int sleepTimeMS)
		{
			bool flag;
			try
			{
				this.executeJS(string.Concat("window.scrollBy(document.body.scrollWidth * ", (double)per / 100, ", 0);"));
				this.delay(sleepTimeMS);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		private void scrollTo(IWebElement element)
		{
			Point location = element.Location;
			location.Y = location.Y - (new Random()).Next(100, 200);
			this.executeJS(string.Concat("window.scrollTo(0,", location.Y, ");"));
		}

		public bool ScrollTo(int px, int sleepTimeMS)
		{
			bool flag;
			try
			{
				this.executeJS(string.Concat("window.scrollTo(", px, ", 0);"));
				this.delay(sleepTimeMS);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool ScrollUp(int per, int sleepTimeMS)
		{
			bool flag;
			try
			{
				this.executeJS(string.Concat("window.scrollBy(0, -", 100, ");"));
				this.delay(sleepTimeMS);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public IWebElement selectElement(string css, int idx)
		{
			IWebElement selectedOption;
			try
			{
				SelectElement selectElement = new SelectElement(this.findElement(css));
				selectElement.SelectByIndex(idx);
				selectedOption = selectElement.SelectedOption;
			}
			catch
			{
				selectedOption = null;
			}
			return selectedOption;
		}

		public bool sendEnter()
		{
			bool flag;
			try
			{
				Actions action = new Actions(this.driver);
				action.SendKeys(OpenQA.Selenium.Keys.Return);
				action.Click();
				action.Build();
				action.Perform();
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool sendEsc()
		{
			bool flag;
			try
			{
				Actions action = new Actions(this.driver);
				action.SendKeys(OpenQA.Selenium.Keys.Escape);
				action.Click();
				action.Build();
				action.Perform();
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool sendKey(string css, string query, int sleepTimeMS = 0)
		{
			bool flag;
			try
			{
				IWebElement webElement = this.findElement(css);
				this.scrollTo(webElement);
				int length = query.Length;
				for (int i = 0; i < length; i++)
				{
					char chr = query.ElementAt<char>(i);
					this.delay(this.rd.Next(10, 40));
					webElement.SendKeys(chr.ToString());
				}
				this.delay(sleepTimeMS);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool sendKey(string str, int sleepTimeMS = 0)
		{
			bool flag;
			try
			{
				int length = str.Length;
				for (int i = 0; i < length; i++)
				{
					this.delay(this.rd.Next(30, 60));
					Actions action = new Actions(this.driver);
					char chr = str.ElementAt<char>(i);
					action.SendKeys(chr.ToString());
					action.Click();
					action.Build();
					action.Perform();
				}
				this.delay(sleepTimeMS);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool sendKeyNoDelay(string css, string query, int sleepTimeMS = 0)
		{
			bool flag;
			try
			{
				this.findElement(css).SendKeys(query);
				this.delay(sleepTimeMS);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		public bool sendKeyPaste(string css, string msg)
		{
			bool flag;
			try
			{
				Util.Clipboard.SetText(msg);
				this.delay(500);
				if (OperatingSystem.IsMacOS())
				{
					IWebElement element = this.findElement(css);
					((IJavaScriptExecutor)this.driver).ExecuteScript("arguments[0].focus();", new object[] { element });
					new Actions(this.driver).KeyDown(OpenQA.Selenium.Keys.Command).SendKeys("v").KeyUp(OpenQA.Selenium.Keys.Command).Perform();
					this.delay(100);
				}
				else
				{
					this.findElement(css).SendKeys(string.Concat(OpenQA.Selenium.Keys.LeftControl, "v"));
					this.delay(100);
					this.findElement(css).SendKeys(OpenQA.Selenium.Keys.LeftControl);
					this.delay(100);
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}


		public void setBandAccount(string bandid, string bandpw, string bandtype)
		{
			this.bandid = bandid;
			this.bandpw = bandpw;
			this.bandtype = bandtype;
		}

		public string[] split(string str, string begin, string end)
		{
			int num = 0;
			string[] strArrays = new string[9999];
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}
			while (str.IndexOf(begin) > -1)
			{
				str = str.Substring(str.IndexOf(begin) + begin.Length);
				if (str.IndexOf(end) <= -1)
				{
					continue;
				}
				strArrays[num] = str.Substring(0, str.IndexOf(end));
				str = str.Substring(str.IndexOf(end) + end.Length);
				num++;
			}
			Array.Resize<string>(ref strArrays, num);
			return strArrays;
		}

		public static List<string> Split(string str, string begin, string end)
		{
			int num = 0;
			List<string> strs = new List<string>();
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}
			while (str.IndexOf(begin) > -1)
			{
				str = str.Substring(str.IndexOf(begin) + begin.Length);
				if (str.IndexOf(end) <= -1)
				{
					continue;
				}
				strs[num] = str.Substring(0, str.IndexOf(end));
				str = str.Substring(str.IndexOf(end) + end.Length);
				num++;
			}
			return strs;
		}

		public ChromeDriver startChrome(int type)
		{
			ChromeDriver chromeDriver;
			try
			{
				ChromeOptions chromeOption = new ChromeOptions();
				ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
				chromeDriverService.HideCommandPromptWindow = true;
				chromeOption.AddArgument("window-postion=100,0");
				chromeOption.AddArgument("window-size=1050,720");
                chromeOption.AddArgument("user-data-dir=" + Path.Combine(AppPaths.DataDir, "chromedata"));
                //chromeOption.AddArgument("incognito");
                chromeOption.AddArgument("disable-gpu");
				chromeOption.AddArgument("--disk-cache-dir=" + Path.Combine(AppPaths.DataDir, "Cache"));
				this.driver = new ChromeDriver(chromeDriverService, chromeOption);
				this.driver.Manage().Timeouts().PageLoad = new TimeSpan(0, 0, 3, 0);
				chromeDriver = this.driver;

				executor = (IJavaScriptExecutor)driver;
			}
			catch
			{
				chromeDriver = null;
			}
			return chromeDriver;
		}

        public void RefreshPage()
        {
            this.driver.Navigate().Refresh();
        }
		public void switchToLast()
		{
			this.driver.SwitchTo().Window(this.driver.WindowHandles.Last<string>());
		}

		private void writeLog(string type, string url)
		{
			Dictionary<string, string> strs = new Dictionary<string, string>()
			{
				{ "type", type },
				{ "url", url }
			};
			//this.requestHTTP("http://band.ecpert.com/insertLog.php", strs);
		}

		public void writeStream(string fileName, string str)
		{
			FileStream fileStream = new FileStream(fileName, FileMode.Append, FileAccess.Write);
			StreamWriter streamWriter = new StreamWriter(fileStream, AppText.Encoding);
			streamWriter.WriteLine(str);
			streamWriter.Flush();
			streamWriter.Close();
			fileStream.Close();
		}
	}
}