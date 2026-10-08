using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BandProgram
{
	public class Naver
	{
		private Util util = Util.getInstance();

		public Naver()
		{
		}

		public void cafeSignUp(string url)
		{
			this.util.goToUrl(url, 1000);
			this.util.delay(1500);
			IReadOnlyCollection<IWebElement> webElements = this.util.findElements("[class=\"btn_join\"]");
			if (webElements.Count<IWebElement>() <= 0)
			{
				return;
			}
			webElements.ElementAt<IWebElement>(0).Click();
			this.util.delay(1500);
			IReadOnlyCollection<IWebElement> webElements1 = this.util.findElementsWithXPath("//DIV[@class='cafe_join_form']//INPUT[@id!='nickname']");
			if (webElements1.Count<IWebElement>() <= 0)
			{
				return;
			}
			foreach (IWebElement webElement in webElements1)
			{
				((IJavaScriptExecutor)this.util.getDriver()).ExecuteScript("arguments[0].scrollIntoView();", new object[] { webElement });
				webElement.SendKeys("네^^");
			}
			this.util.delay(1500);
			this.util.findElement("[id=\"submitBtn\"]").Click();
		}

		public string getQuery()
		{
			string text = "";
			string str = this.util.findElement("[class='title_text']").Text;
			text = this.util.findElement("[class='_endContentsText']").Text;
			if (this.util.findElements("[class='_endContentsText']").Count > 0)
			{
				string text1 = this.util.findElements("[class='_endContentsText']").ElementAt<IWebElement>(1).Text;
			}
			return string.Concat(str, "\t", text);
		}

		public void goNaver()
		{
			this.util.goToUrl("http://naver.com", 1000);
			this.util.delay(2000);
		}

		public void goToKin()
		{
			IReadOnlyCollection<IWebElement> webElements = this.util.findElements("[class='lnb5']");
			if (webElements.ElementAt<IWebElement>(0).Text != "")
			{
				webElements.ElementAt<IWebElement>(0).Click();
			}
			else
			{
				this.util.findElement("[class='lnb_more']").Click();
				this.util.findElement("[class='lnb5']").Click();
			}
			this.util.delay(1000);
		}

		public bool login(string id, string pw)
		{
			bool flag;
			try
			{
				this.goNaver();
				IReadOnlyCollection<IWebElement> webElements = this.util.findElements("[class=\"sprh sprh_sch_ham\"]");
				if (webElements.Count <= 0)
				{
					this.util.delay(1500);
					this.util.clickByCss("[class=\"sprh sprh_sch_ham\"]", 0, 0);
				}
				else
				{
					webElements.ElementAt<IWebElement>(0).Click();
				}
				this.util.delay(2000);
				if (this.util.findElements("em").Count<IWebElement>() > 0)
				{
					this.util.findElementsWithXPath("/html/body/div[@id='ct']/div[@id='aside']/div[@class='footer']/ul[@class='section_aside_bottom']/li[@class='sab_item'][1]/a[@class='btn_bottom']").ElementAt<IWebElement>(0).Click();
					this.util.delay(1000);
					this.util.acceptAlert();
					this.util.delay(1000);
					flag = this.login(id, pw);
				}
				else
				{
					this.util.delay(1000);
					this.util.findElement("[class=\"user_nick\"]").Click();
					this.util.delay(1000);
					this.util.sendKey("[id=\"id\"]", id, 300);
					this.util.sendKey("[id=\"pw\"]", pw, 300);
					this.util.clickByCss("[id=\"login_submit\"]", 0, 0);
					this.util.delay(1000);
					if (this.util.findElements("[class=\"user_email\"]").Count > 0)
					{
						this.util.delay(2000);
						return true;
					}
					else
					{
						flag = false;
					}
				}
			}
			catch (Exception exception)
			{
				flag = false;
			}
			return flag;
		}

		public void randomPaging()
		{
			this.util.ScrollDown(1000, 1000);
			this.util.delay(1000);
			int num = (new Random()).Next(0, 4);
			this.util.findElements(".paging a").ElementAt<IWebElement>(num).Click();
			this.util.delay(1000);
		}

		public void randomQueryList()
		{
			IReadOnlyCollection<IWebElement> webElements = this.util.findElements(".type01 li");
			Console.WriteLine("queryListCount");
			Console.WriteLine(webElements.Count);
			int num = (new Random()).Next(0, webElements.Count);
			Console.WriteLine(webElements.ElementAt<IWebElement>(num).FindElement(By.TagName("a")).GetAttribute("href"));
			string attribute = webElements.ElementAt<IWebElement>(num).FindElement(By.TagName("a")).GetAttribute("href");
			this.util.goToUrl(attribute, 1000);
			this.util.delay(1000);
		}

		public void search(string keyword)
		{
			this.goNaver();
			this.util.findElement("input[id = 'query']").Click();
			this.util.delay(500);
			this.util.sendKey("input[id = 'query']", keyword, 300);
			this.util.delay(500);
			this.util.findElement("button[id='search_btn']").Click();
			this.util.delay(1000);
		}

		public bool writeToBoard(string url)
		{
			bool flag;
			try
			{
				this.util.goToUrl(url, 1000);
				this.util.delay(1500);
				this.util.findElement("[class=\"btn_write\"]").Click();
				this.util.delay(1500);
				(new SelectElement(this.util.findElement("[name='menuid']"))).SelectByValue("1");
				Random random = new Random();
				int num = random.Next(0, 999999);
				string str = num.ToString();
				this.util.sendKey("[id=\"subject\"]", str, 300);
				this.util.enterFrame("[id=\"frame\"]");
				num = random.Next(0, 999999);
				string str1 = num.ToString();
				this.util.findElement("[id = \"body\"]").SendKeys(str1);
				this.util.escapeFrame();
				this.util.findElements("[class=\"btn_cell\"]").ElementAt<IWebElement>(1).Click();
				return true;
			}
			catch (Exception exception)
			{
				flag = false;
			}
			return flag;
		}
	}
}