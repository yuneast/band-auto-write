using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BandProgramAvalonia.Models;

namespace BandProgramAvalonia.Services;

public class Util
{
    private string? bandId;
    private string? bandPw;
    private string? bandType;
    private static Util? instance;
    private ChromeDriver? driver;
    private IJavaScriptExecutor? executor;
    private readonly Random rd = new();
    private static readonly HttpClient httpClient = new();

    private static readonly string StartupPath = AppDomain.CurrentDomain.BaseDirectory;

    public static Util GetInstance()
    {
        instance ??= new Util();
        return instance;
    }

    public bool AcceptAlert()
    {
        try
        {
            driver?.SwitchTo().Alert().Accept();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string Base64Decoding(string decodingText, Encoding? encoding = null)
    {
        if (decodingText == null) return string.Empty;
        encoding ??= Encoding.UTF8;
        return encoding.GetString(Convert.FromBase64String(decodingText));
    }

    public string Base64Encoding(string encodingText, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return Convert.ToBase64String(encoding.GetBytes(encodingText));
    }

    public bool ClickByCss(string css, int sleepTimeMs = 0, int failCnt = 0)
    {
        try
        {
            if (failCnt > 5) return false;

            var element = FindElement(css);
            if (element == null)
                return ClickByCss(css, sleepTimeMs, failCnt + 1);

            ScrollTo(element);
            RandomClick(element, sleepTimeMs, 1);
            return true;
        }
        catch
        {
            return ClickByCss(css, sleepTimeMs, failCnt + 1);
        }
    }

    public bool ClickByElement(IWebElement element, int sleepTimeMs = 0, int failCnt = 0)
    {
        try
        {
            if (failCnt > 5) return false;
            ScrollTo(element);
            RandomClick(element, sleepTimeMs, 1);
            return true;
        }
        catch
        {
            return ClickByElement(element, sleepTimeMs, failCnt + 1);
        }
    }

    public bool ClickByCssNew(string css, int timeDelayMs, int test)
    {
        var element = FindElement(css);
        if (element == null || executor == null) return false;
        executor.ExecuteScript("arguments[0].click();", element);
        Delay(timeDelayMs);
        return true;
    }

    public bool ClickByElementNoScroll(IWebElement element, int sleepTimeMs = 0, int recCnt = 1)
    {
        try
        {
            RandomClick(element, sleepTimeMs, recCnt);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool CloseChrome()
    {
        if (driver == null) return true;
        try
        {
            if (driver.WindowHandles != null)
            {
                foreach (var handle in driver.WindowHandles)
                {
                    driver.SwitchTo().Window(handle);
                    driver.Close();
                }
                driver.Quit();
            }

            // Kill chromedriver processes
            foreach (var process in Process.GetProcessesByName("chromedriver"))
            {
                try { process.Kill(); } catch { }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool CloseCurrent()
    {
        try
        {
            if (driver?.WindowHandles.Count > 1)
            {
                driver.Close();
                SwitchToLast();
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void CreateNotePad(string fileName)
    {
        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        using var sw = new StreamWriter(fs, Encoding.UTF8);
        sw.Flush();
    }

    public string CurrentUrl() => driver?.Url ?? string.Empty;

    public DateTime Delay(int ms)
    {
        Thread.Sleep(ms);
        return DateTime.Now;
    }

    public bool DelayNext(string query, int sleepTimeSec)
    {
        try
        {
            for (int i = 0; i < sleepTimeSec && FindElement(query) == null; i++)
            {
                Delay(1000);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void DeleteDir(string path)
    {
        try
        {
            var files = new DirectoryInfo(path).GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                file.Attributes = FileAttributes.Normal;
            }
            Directory.Delete(path, true);
        }
        catch { }
    }

    public bool DismissAlert()
    {
        try
        {
            driver?.SwitchTo().Alert().Dismiss();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void EnterFrame(string str)
    {
        var element = FindElement(str);
        if (element != null)
            driver?.SwitchTo().Frame(element);
    }

    public void EscapeFrame()
    {
        driver?.SwitchTo().ParentFrame();
    }

    private void ExecuteJs(string text)
    {
        ((IJavaScriptExecutor?)driver)?.ExecuteScript(text);
    }

    private void ExecuteJs(string text, params object[] parameters)
    {
        ((IJavaScriptExecutor?)driver)?.ExecuteScript(text, parameters);
    }

    public IWebElement? FindElement(string str)
    {
        try
        {
            return driver?.FindElement(By.CssSelector(str));
        }
        catch
        {
            return null;
        }
    }

    public IWebElement? FindElement(IWebElement element, string str)
    {
        try
        {
            return element.FindElement(By.CssSelector(str));
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyCollection<IWebElement> FindElements(string str)
    {
        return driver?.FindElements(By.CssSelector(str)) ?? new List<IWebElement>().AsReadOnly();
    }

    public IWebElement? FindElementWithRec(string css, int recCnt)
    {
        try
        {
            var element = FindElement(css);
            for (int i = 0; element == null && i < recCnt; i++)
            {
                Delay(500);
                element = FindElement(css);
            }
            Delay(500);
            return element;
        }
        catch
        {
            return null;
        }
    }

    public string? GetAlertText()
    {
        try
        {
            return driver?.SwitchTo().Alert().Text;
        }
        catch
        {
            return null;
        }
    }

    public string? GetBandId() => bandId;
    public string? GetBandPw() => bandPw;
    public string? GetBandType() => bandType;

    public DirectoryInfo[]? GetDirectoryList(string path)
    {
        try
        {
            return new DirectoryInfo(path).GetDirectories();
        }
        catch
        {
            return null;
        }
    }

    public ChromeDriver? GetDriver() => driver;

    public List<ImageFile>? GetImageList(string path)
    {
        try
        {
            var images = new List<ImageFile>();
            var files = new DirectoryInfo(path).GetFiles();
            foreach (var file in files)
            {
                var ext = file.Extension.ToLower();
                if (ext is ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".png")
                {
                    var size = new FileInfo(file.FullName).Length;
                    images.Add(new ImageFile(file.Name, path, size));
                }
            }
            return images;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> GetMyIpAsync()
    {
        return await RequestHttpAsync("http://ektsn.dothome.co.kr/myip.php");
    }

    public string GetPageSource() => driver?.PageSource ?? string.Empty;

    public void GoToUrl(string url, int sleepTimeMs = 1000)
    {
        driver?.Navigate().GoToUrl(url);
        Delay(sleepTimeMs);
    }

    public bool IsOpenChrome()
    {
        try
        {
            SwitchToLast();
            return driver?.WindowHandles.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public void NavBack(int sleepTime = 0)
    {
        driver?.Navigate().Back();
        Delay(sleepTime * 1000);
    }

    public void NavForward(int sleepTime = 0)
    {
        driver?.Navigate().Forward();
        Delay(sleepTime * 1000);
    }

    private bool RandomClick(IWebElement element, int sleepTimeMs, int recCnt = 1)
    {
        try
        {
            var actions = new Actions(driver!);
            var random = new Random();
            var size = element.Size;
            var minX = (int)(size.Width * 0.4);
            var minY = (int)(size.Height * 0.4);
            var maxX = (int)(size.Width * 0.7);
            var maxY = (int)(size.Height * 0.7);

            actions.MoveToElement(element, random.Next(minX, maxX), random.Next(minY, maxY));
            actions.Click();
            actions.Build();

            for (int i = 0; i < recCnt; i++)
            {
                actions.Perform();
                Delay(sleepTimeMs);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string? ReadALine(string fileName)
    {
        try
        {
            using var sr = new StreamReader(fileName, Encoding.Default);
            return sr.ReadLine();
        }
        catch
        {
            return null;
        }
    }

    public List<string>? ReadAll(string fileName)
    {
        try
        {
            if (!File.Exists(fileName)) return null;

            var lines = new List<string>();
            var encoding = DetectFileEncoding(fileName);
            using var sr = new StreamReader(fileName, encoding);
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(line))
                    lines.Add(line);
            }
            return lines;
        }
        catch
        {
            return null;
        }
    }

    private Encoding DetectFileEncoding(string fileName)
    {
        try
        {
            // Read first few bytes to detect encoding
            var bytes = File.ReadAllBytes(fileName);
            if (bytes.Length == 0) return Encoding.UTF8;

            // Check for UTF-8 BOM
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;

            // Try UTF-8 first (without BOM)
            try
            {
                var utf8 = new UTF8Encoding(false, true);
                using var reader = new StreamReader(fileName, utf8);
                reader.ReadToEnd();
                return Encoding.UTF8;
            }
            catch (DecoderFallbackException)
            {
                // If UTF-8 fails, use Default (CP949 on Korean Windows)
                return Encoding.Default;
            }
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    public string? ReadAllToString(string fileName)
    {
        try
        {
            if (!File.Exists(fileName)) return null;

            var encoding = DetectFileEncoding(fileName);
            using var sr = new StreamReader(fileName, encoding);
            var sb = new StringBuilder();
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
        catch
        {
            return null;
        }
    }

    public void RemoveALine(string fileName, int idx)
    {
        var lines = ReadAll(fileName);
        if (lines == null) return;

        CreateNotePad(fileName);
        int num = 0;
        foreach (var line in lines)
        {
            if (num != idx)
            {
                WriteStream(fileName, line);
            }
            num++;
        }
    }

    public async Task<string> RequestHttpAsync(string url)
    {
        try
        {
            return await httpClient.GetStringAsync(url);
        }
        catch
        {
            return string.Empty;
        }
    }

    public string RequestHttp(string url)
    {
        try
        {
            return httpClient.GetStringAsync(url).Result;
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool ScrollDown(int per, int sleepTimeMs)
    {
        try
        {
            ExecuteJs($"window.scrollBy(0, document.body.scrollHeight * {per / 100.0});");
            Delay(sleepTimeMs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ScrollUp(int per, int sleepTimeMs)
    {
        try
        {
            ExecuteJs("window.scrollBy(0, -100);");
            Delay(sleepTimeMs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ScrollRight(int per, int sleepTimeMs)
    {
        try
        {
            ExecuteJs($"window.scrollBy(document.body.scrollWidth * {per / 100.0}, 0);");
            Delay(sleepTimeMs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ScrollTo(IWebElement element)
    {
        var location = element.Location;
        location.Y -= new Random().Next(100, 200);
        ExecuteJs($"window.scrollTo(0,{location.Y});");
    }

    public bool SendEnter()
    {
        try
        {
            var actions = new Actions(driver!);
            actions.SendKeys(Keys.Return);
            actions.Click();
            actions.Build();
            actions.Perform();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool SendKey(string css, string query, int sleepTimeMs = 0)
    {
        try
        {
            var element = FindElement(css);
            if (element == null) return false;

            ScrollTo(element);
            foreach (char c in query)
            {
                Delay(rd.Next(10, 40));
                element.SendKeys(c.ToString());
            }
            Delay(sleepTimeMs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool SendKeyNoDelay(string css, string query, int sleepTimeMs = 0)
    {
        try
        {
            FindElement(css)?.SendKeys(query);
            Delay(sleepTimeMs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool SendKeyPaste(string css, string msg)
    {
        try
        {
            // Note: Clipboard handling needs platform-specific implementation
            // For now, just send the keys directly
            var element = FindElement(css);
            element?.SendKeys(msg);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SetBandAccount(string id, string pw, string type)
    {
        bandId = id;
        bandPw = pw;
        bandType = type;
    }

    public string[] Split(string str, string begin, string end)
    {
        var results = new List<string>();
        if (string.IsNullOrEmpty(str)) return results.ToArray();

        while (str.IndexOf(begin) > -1)
        {
            str = str[(str.IndexOf(begin) + begin.Length)..];
            if (str.IndexOf(end) > -1)
            {
                results.Add(str[..str.IndexOf(end)]);
                str = str[(str.IndexOf(end) + end.Length)..];
            }
        }
        return results.ToArray();
    }

    public ChromeDriver? StartChrome(int type)
    {
        try
        {
            var options = new ChromeOptions();
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            options.AddArgument("window-position=100,0");
            options.AddArgument("window-size=1050,720");
            options.AddArgument($"user-data-dir={StartupPath}/chromedata");
            options.AddArgument("disable-gpu");
            options.AddArgument($"--disk-cache-dir={StartupPath}/Cache/");

            driver = new ChromeDriver(service, options);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);
            executor = driver;

            return driver;
        }
        catch
        {
            return null;
        }
    }

    public void RefreshPage()
    {
        driver?.Navigate().Refresh();
    }

    public void SwitchToLast()
    {
        if (driver?.WindowHandles.Count > 0)
            driver.SwitchTo().Window(driver.WindowHandles.Last());
    }

    public void WriteStream(string fileName, string str)
    {
        using var fs = new FileStream(fileName, FileMode.Append, FileAccess.Write);
        using var sw = new StreamWriter(fs, Encoding.UTF8);
        sw.WriteLine(str);
    }
}
