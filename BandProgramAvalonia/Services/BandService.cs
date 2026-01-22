using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BandProgramAvalonia.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;

namespace BandProgramAvalonia.Services;

public class BandService
{
    private readonly Util util = Util.GetInstance();
    private readonly ApiService apiService = new();
    private static readonly string StartPath = AppDomain.CurrentDomain.BaseDirectory;

    public Action<string>? OnLog { get; set; }
    public Action<string>? OnStatusLog { get; set; }

    #region Band List Management

    public List<BandInfo> GetBandListFromFile()
    {
        try
        {
            var bandList = new List<BandInfo>();
            var lines = util.ReadAll("bandList.txt");
            if (lines == null) return bandList;

            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length >= 5)
                {
                    bandList.Add(new BandInfo
                    {
                        Name = parts[0],
                        Num = parts[1],
                        PostingList = StringToIntList(parts[2]),
                        CommentList = StringToIntList(parts[3]),
                        ChattingList = StringToIntList(parts[4])
                    });
                }
            }
            return bandList;
        }
        catch
        {
            return new List<BandInfo>();
        }
    }

    public void SaveBandList(List<BandInfo> bandList)
    {
        try
        {
            util.CreateNotePad("bandList.txt");
            foreach (var band in bandList)
            {
                util.WriteStream("bandList.txt",
                    $"{band.Name}\t{band.Num}\t{IntListToString(band.PostingList)}\t{IntListToString(band.CommentList)}\t{IntListToString(band.ChattingList)}");
            }
        }
        catch { }
    }

    public List<BandInfo>? GetBandListFromWeb(List<BandInfo>? preList)
    {
        var bandInfos = preList ?? new List<BandInfo>();

        try
        {
            if (!util.IsOpenChrome())
            {
                Login();
            }

            var existingIds = new HashSet<string>(bandInfos.Select(b => b.Num).Where(s => !string.IsNullOrWhiteSpace(s)));

            if (Global.Instance.IsTest)
            {
                util.GoToUrl("http://127.0.0.1:3000/feed.html", 1000);
            }
            else
            {
                util.GoToUrl("https://band.us/feed/", 1000);
            }

            var tries = 0;
            while (tries < 5 && util.FindElements("[class='bandLogo']").Count == 0)
            {
                util.Delay(3000);
                tries++;
            }
            if (tries >= 5) return null;

            util.Delay(3000);
            util.ClickByCss("[class='myBandMoreView _btnMore']", 1500);
            util.ScrollDown(100, 500);

            foreach (var element in util.FindElements("[class='feedMyBandList']"))
            {
                try
                {
                    var nameElement = util.FindElement(element, "[class='ellipsis']");
                    var name = nameElement?.Text?.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var anchor = util.FindElement(element, "a");
                    var href = anchor?.GetAttribute("href");
                    if (string.IsNullOrEmpty(href)) continue;

                    var id = ExtractBandId(href);
                    if (string.IsNullOrEmpty(id)) continue;

                    if (!existingIds.Add(id)) continue;

                    bandInfos.Add(new BandInfo { Name = name, Num = id });
                }
                catch { continue; }
            }

            return bandInfos;
        }
        catch
        {
            return null;
        }
    }

    public List<BandInfo>? RemoveBands(List<int> indexList)
    {
        try
        {
            var lines = util.ReadAll("bandList.txt");
            if (lines == null) return null;

            util.CreateNotePad("bandList.txt");
            var bandList = new List<BandInfo>();
            var idx = 0;

            foreach (var line in lines)
            {
                if (!indexList.Contains(idx))
                {
                    var parts = line.Split('\t');
                    if (parts.Length >= 5)
                    {
                        var band = new BandInfo
                        {
                            Name = parts[0],
                            Num = parts[1],
                            PostingList = StringToIntList(parts[2]),
                            CommentList = StringToIntList(parts[3]),
                            ChattingList = StringToIntList(parts[4])
                        };
                        bandList.Add(band);
                        util.WriteStream("bandList.txt",
                            $"{band.Name}\t{band.Num}\t{IntListToString(band.PostingList)}\t{IntListToString(band.CommentList)}\t{IntListToString(band.ChattingList)}");
                    }
                }
                idx++;
            }
            return bandList;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Login

    public bool Login()
    {
        var bandId = util.GetBandId();
        var bandPw = util.GetBandPw();
        var bandType = util.GetBandType();

        if (string.IsNullOrEmpty(bandId) || string.IsNullOrEmpty(bandPw))
            return false;

        return Login(bandId, bandPw, bandType ?? "이메일", true);
    }

    public bool Login(string id, string pw, string type, bool logout = true)
    {
        try
        {
            util.CloseChrome();
            util.StartChrome(0);

            if (Global.Instance.IsTest) return true;

            util.GoToUrl("https://band.us", 300);

            // 페이지 로드 대기 (최대 20초)
            int waitCount = 0;
            while (waitCount < 40 &&
                   util.FindElement("[class='user _settingRegion']") == null &&
                   util.FindElement("[data-viewname='DIntroHeaderView']") == null)
            {
                util.Delay(500);
                waitCount++;
            }

            if (util.FindElement("[class='user _settingRegion']") != null)
            {
                if (!logout) return true;

                util.ClickByCss("[class='uProfile -size30 -border']", 500);
                util.ClickByCss("[class='menuModalLink _btnLogout']", 500);
                util.ClickByCss("[class='uButton -confirm _btnLogout']", 500);
            }

            OnStatusLog?.Invoke("로그인 페이지 이동 중...");
            util.GoToUrl("https://auth.band.us", 300);

            OnStatusLog?.Invoke("로그인 버튼 대기 중...");
            util.DelayNext("[class='uButtonRound -h56 -icoType -phone']", 200);

            if (type.Contains("이메일"))
            {
                OnStatusLog?.Invoke("이메일 로그인 버튼 클릭 중...");
                util.DelayNext("[class='loginBtnList'] [class='uButtonRound -h56 -icoType -email']", 200);

                if (!util.ClickByCss("[class='loginBtnList'] [class='uButtonRound -h56 -icoType -email']", 500))
                {
                    OnLog?.Invoke("이메일 로그인 버튼을 찾을 수 없습니다.");
                    return false;
                }

                OnStatusLog?.Invoke("이메일 입력 중...");
                util.SendKey("[id='input_email']", id, 300);
                util.SendKey("[id='input_password']", pw, 1300);
                util.SendEnter();
            }
            else if (type.Contains("전화번호"))
            {
                OnStatusLog?.Invoke("전화번호 로그인 버튼 클릭 중...");
                util.DelayNext("[class='loginBtnList'] [class='uButtonRound -h56 -icoType -phone']", 200);

                if (!util.ClickByCss("[class='loginBtnList'] [class='uButtonRound -h56 -icoType -phone']", 500))
                {
                    OnLog?.Invoke("전화번호 로그인 버튼을 찾을 수 없습니다.");
                    return false;
                }

                OnStatusLog?.Invoke("전화번호 입력 중...");
                util.SendKey("[id='input_local_phone_number']", id, 300);
                util.SendEnter();
                util.SendKey("[id='pw']", pw, 300);
                util.SendEnter();
            }
            else if (type.Contains("네이버"))
            {
                OnStatusLog?.Invoke("네이버 로그인 버튼 클릭 중...");
                util.DelayNext("[class*='uButtonRound -h56 -icoType -naver externalLogin']", 200);

                if (!util.ClickByCss("[class*='uButtonRound -h56 -icoType -naver externalLogin']", 500))
                {
                    OnLog?.Invoke("네이버 로그인 버튼을 찾을 수 없습니다.");
                    return false;
                }

                OnStatusLog?.Invoke("네이버 ID/PW 입력 중...");
                util.DelayNext("[id='id']", 200);
                util.SendKey("[id='id']", id, 300);
                util.SendKey("[id='pw']", pw, 300);
                util.SendEnter();
                util.ClickByCss("[class='btn_unit_on']");
            }

            for (int i = 0; i < 200; i++)
            {
                util.Delay(3000);
                if (!util.IsOpenChrome()) return false;
                if (util.FindElements("[class='uProfile -size30 -border']").Count > 0)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool CloseChrome() => util.CloseChrome();

    #endregion

    #region Band Signup

    public string SignupBand(BandInfo band, string nickName)
    {
        try
        {
            util.GoToUrl($"https://band.us/band/{band.Num}", 3000);

            if (util.AcceptAlert())
            {
                util.AcceptAlert();
                util.CloseCurrent();
            }

            var joinBtn = util.FindElement("[class='uButton -sizeL -confirm _btnJoinBand']");
            if (!util.ClickByElement(joinBtn!))
            {
                return "이미 가입한 밴드입니다.";
            }

            var alertText = util.GetAlertText();
            if (alertText != null) return alertText;

            if (joinBtn!.Text.Contains("가입신청 중"))
            {
                return "가입 신청 중인 밴드입니다.";
            }

            if (util.FindElement("[class='uProfile -size30 -border']") != null)
            {
                util.SendKey("[class='_joinAnswer _use_keyup_event']", "네^^", 500);
                util.ClickByCss("[class='uButton _confirmBtn -confirm']", 500);
            }

            util.ClickByCss("[class='addProfile _newProfileBtn']", 500);
            util.SendKey("[class='_nameInput']", nickName, 500);
            util.ClickByCss("[class='uButton -confirm -sizeL _confirmBtn']", 500);

            for (int i = 0; i < 100 && !util.AcceptAlert(); i++)
            {
                util.Delay(50);
            }
            util.AcceptAlert();

            return $"{band.Name} 밴드 가입 성공";
        }
        catch
        {
            return "가입 실패";
        }
    }

    #endregion

    #region Helpers

    private static string? ExtractBandId(string? href)
    {
        var match = Regex.Match(href ?? string.Empty, @"(?:^|/)band/(\d+)(?:/|$|\?)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private List<int> StringToIntList(string? str)
    {
        if (string.IsNullOrEmpty(str)) return new List<int>();

        var list = new List<int>();
        foreach (var part in str.Split(','))
        {
            if (int.TryParse(part.Trim(), out var num))
                list.Add(num);
        }
        return list;
    }

    private string IntListToString(List<int>? list)
    {
        if (list == null || list.Count == 0) return string.Empty;
        return string.Join(", ", list);
    }

    public void UpdateBandPostingList(BandInfo band)
    {
        try
        {
            var lines = util.ReadAll("bandList.txt");
            if (lines == null) return;

            util.CreateNotePad("bandList.txt");
            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length >= 2 && parts[1] == band.Num)
                {
                    util.WriteStream("bandList.txt",
                        $"{band.Name}\t{band.Num}\t{IntListToString(band.PostingList)}\t{IntListToString(band.CommentList)}\t{IntListToString(band.ChattingList)}");
                }
                else
                {
                    util.WriteStream("bandList.txt", line);
                }
            }
        }
        catch { }
    }

    #endregion
}
