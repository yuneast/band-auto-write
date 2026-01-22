using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BandProgramAvalonia.Models;

namespace BandProgramAvalonia.Services;

public class ChattingService
{
    private readonly Util util = Util.GetInstance();
    private readonly BandService bandService = new();
    private readonly PostingService postingService = new();
    private static readonly string StartPath = AppDomain.CurrentDomain.BaseDirectory;

    public Action<string>? OnLog { get; set; }
    public Action<string>? OnStatusLog { get; set; }

    #region Parameters

    public int ChattingType { get; set; }
    public int BetweenWork { get; set; }
    public bool ReservedCheck { get; set; }
    public bool PasteCheck { get; set; }
    public int ReserveHour { get; set; }
    public int ReserveMin { get; set; }
    public int RecCnt { get; set; }
    public int RecBetweenWork { get; set; }

    public void SetParams(int type, int betweenWork, bool reservedCheck, bool pasteCheck,
        int reserveHour, int reserveMin, int recCnt, int recBetweenWork)
    {
        ChattingType = type;
        BetweenWork = betweenWork;
        ReservedCheck = reservedCheck;
        PasteCheck = pasteCheck;
        ReserveHour = reserveHour;
        ReserveMin = reserveMin;
        RecCnt = recCnt;
        RecBetweenWork = recBetweenWork;
    }

    #endregion

    #region Chatting Content

    private string? GetChattingContent(int chattingNum)
    {
        try
        {
            return util.ReadAllToString(Path.Combine(StartPath, $"AutoDoc/Chatting/chatting_{chattingNum}/contents.txt"));
        }
        catch
        {
            return null;
        }
    }

    private List<ImageFile>? GetChattingImages(int chattingNum)
    {
        try
        {
            return util.GetImageList(Path.Combine(StartPath, $"AutoDoc/Chatting/chatting_{chattingNum}"));
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Chatting Execution

    public async Task<bool> StartChattingAsync(CancellationToken cancellationToken)
    {
        try
        {
            for (int i = 0; (i < RecCnt || RecCnt == 0) && !cancellationToken.IsCancellationRequested; i++)
            {
                if (i != 0)
                    await Task.Delay(1000 * RecBetweenWork, cancellationToken);

                var chattingList = postingService.GetPostingList("AutoDoc/Chatting", "chatting_");
                var chattingNums = chattingList.Select(c => c.Idx).OrderBy(x => x).ToList();

                if (ReservedCheck)
                    await WaitForReservedTimeAsync(cancellationToken);

                if (!util.IsOpenChrome())
                {
                    OnStatusLog?.Invoke("로그인 중입니다.");
                    bandService.Login();
                }

                if (RecCnt == 0) i = 0;

                var chattingIdx = 0;
                var isFirst = true;

                foreach (var band in bandService.GetBandListFromFile())
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        util.AcceptAlert();
                        util.CloseCurrent();

                        var hasChatting = DetermineChattingList(band, chattingNums, ref chattingIdx);
                        if (!hasChatting) continue;

                        if (!isFirst)
                            await Task.Delay(1000 * BetweenWork, cancellationToken);
                        else
                            isFirst = false;

                        OnStatusLog?.Invoke("url 이동");
                        var content = GetChattingContent(band.ChattingList.First());
                        if (string.IsNullOrEmpty(content)) continue;

                        util.GoToUrl($"https://band.us/band/{band.Num}", 1000);

                        if (util.AcceptAlert())
                        {
                            OnLog?.Invoke($"https://band.us/band/{band.Num} - 작업 불가능한 밴드입니다.");
                            continue;
                        }

                        if (util.FindElementWithRec("[class='uButton -sizeL -confirm _btnJoinBand']", 3) != null)
                        {
                            OnLog?.Invoke("가입이 되어있지 않습니다.");
                            continue;
                        }

                        var chatRoomCount = util.FindElements("[class='chat _bandChattingChannelList'] li [class='_btnOpenChat']").Count;

                        if (chatRoomCount <= 0)
                        {
                            await CreateNewChatRoomAsync(band, content, cancellationToken);
                        }
                        else
                        {
                            await SendToChatRoomsAsync(band, content, chatRoomCount, cancellationToken);
                        }

                        OnLog?.Invoke($"http://band.us/band/{band.Num} - 채팅 성공");

                        if (ChattingType == 0)
                            RotateChattingList(band);
                    }
                    catch
                    {
                        OnLog?.Invoke("예외상황이 발생하여 다음 작업으로 넘어갑니다.");
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task WaitForReservedTimeAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            if (now.Hour == ReserveHour && now.Minute == ReserveMin)
                break;
            await Task.Delay(5000, cancellationToken);
        }
    }

    private bool DetermineChattingList(BandInfo band, List<int> chattingNums, ref int chattingIdx)
    {
        switch (ChattingType)
        {
            case 0:
                return band.ChattingList != null && band.ChattingList.Count > 0;

            case 1:
                band.ChattingList.Clear();
                band.ChattingList.Add(chattingNums[new Random().Next(0, chattingNums.Count)]);
                return true;

            case 2:
                band.ChattingList.Clear();
                band.ChattingList.Add(chattingNums[chattingIdx]);
                chattingIdx++;
                if (chattingIdx >= chattingNums.Count) chattingIdx = 0;
                return true;

            default:
                return false;
        }
    }

    private async Task CreateNewChatRoomAsync(BandInfo band, string content, CancellationToken cancellationToken)
    {
        OnStatusLog?.Invoke("채팅방에 들어가는 중입니다.");
        util.ScrollRight(100, 300);
        util.ScrollDown(5, 300);
        util.ScrollUp(3000, 300);

        var newChatBtn = util.FindElement("[class='newChat sf_color _btnNewChat']");
        if (newChatBtn == null)
        {
            OnLog?.Invoke("새채팅 버튼을 발견하지 못했습니다.");
            return;
        }

        util.ClickByElementNoScroll(newChatBtn, 300, 3);
        util.Delay(1500);

        var privateChatLink = util.FindElementWithRec("[class='lyMenu _lyChatTypes'][style='min-width: 145px; display: block;'] [class='_linkNewPrivateChat']", 2);
        if (privateChatLink != null)
            util.ClickByElementNoScroll(privateChatLink, 500, 1);

        if (util.FindElementWithRec("[class='main -tSpaceNone']", 5) == null)
        {
            OnLog?.Invoke("채팅방을 생성할 수 없습니다.");
            return;
        }

        OnStatusLog?.Invoke("멤버 초대 중입니다.");
        util.FindElementWithRec("[class='listLine']", 10);

        var members = util.FindElements("[class='listLine'] li");
        if (members.Count == 0)
        {
            OnLog?.Invoke("멤버 없음");
            return;
        }

        foreach (var member in members)
            util.ClickByElement(member);

        var confirmBtn = util.FindElementWithRec("[class='uButton -confirm']", 5);
        if (confirmBtn != null)
            util.ClickByElement(confirmBtn, 1000);

        var createBtn = util.FindElementWithRec("[class='uButton -confirm _btnConfirm']", 5);
        if (createBtn != null)
            util.ClickByElement(createBtn, 1000);

        util.SwitchToLast();
        util.FindElementWithRec("[class='commentWrite _use_keyup_event']", 10);

        await SendChatMessageAsync(band, content);
    }

    private async Task SendToChatRoomsAsync(BandInfo band, string content, int chatRoomCount, CancellationToken cancellationToken)
    {
        for (int j = 0; j < chatRoomCount; j++)
        {
            OnStatusLog?.Invoke("채팅방에 들어가는 중입니다.");
            util.SwitchToLast();

            var chatBtn = util.FindElements("[class='chat _bandChattingChannelList'] li [class='_btnOpenChat']").ElementAt(j);
            util.ClickByElement(chatBtn, 1500);
            util.ClickByElement(chatBtn, 1500);
            util.SwitchToLast();

            if (util.FindElementWithRec("[class='commentWrite _use_keyup_event']", 10) == null)
            {
                OnLog?.Invoke("채팅방을 찾을 수 없습니다.");
                continue;
            }

            await SendChatMessageAsync(band, content);
        }
    }

    private async Task SendChatMessageAsync(BandInfo band, string content)
    {
        OnStatusLog?.Invoke("내용 입력 중입니다.");

        if (!PasteCheck)
            util.SendKeyNoDelay("[class='commentWrite _use_keyup_event']", content, 500);
        else
            util.SendKeyPaste("[class='commentWrite _use_keyup_event']", content);

        util.Delay(500);
        util.SendEnter();

        var images = GetChattingImages(band.ChattingList.First());
        if (images != null && images.Count > 0)
        {
            OnStatusLog?.Invoke("이미지 업로드 중입니다.");
            var uploadInput = util.FindElementWithRec("[data-uiselector='imageUploadArea'] [type='file']", 5);

            if (uploadInput != null)
            {
                foreach (var img in images)
                {
                    var path = Path.Combine(img.Path, img.FileName);
                    uploadInput.SendKeys(path.Replace('/', Path.DirectorySeparatorChar));
                }

                util.Delay(2000);

                for (int k = 0; k < 100 && util.FindElement("[data-uiselector='imageUploadButton'][aria-disabled='true']") != null; k++)
                {
                    util.Delay(500);
                }
            }
        }

        util.Delay(1500);
        util.CloseCurrent();
    }

    private void RotateChattingList(BandInfo band)
    {
        if (band.ChattingList == null || band.ChattingList.Count <= 1) return;

        var first = band.ChattingList[0];
        band.ChattingList.RemoveAt(0);
        band.ChattingList.Add(first);

        bandService.UpdateBandPostingList(band);
    }

    #endregion
}
