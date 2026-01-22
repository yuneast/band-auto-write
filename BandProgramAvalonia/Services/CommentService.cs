using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BandProgramAvalonia.Models;

namespace BandProgramAvalonia.Services;

public class CommentService
{
    private readonly Util util = Util.GetInstance();
    private readonly BandService bandService = new();
    private readonly PostingService postingService = new();
    private static readonly string StartPath = AppDomain.CurrentDomain.BaseDirectory;

    public Action<string>? OnLog { get; set; }
    public Action<string>? OnStatusLog { get; set; }

    #region Parameters

    public int CommentType { get; set; }
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
        CommentType = type;
        BetweenWork = betweenWork;
        ReservedCheck = reservedCheck;
        PasteCheck = pasteCheck;
        ReserveHour = reserveHour;
        ReserveMin = reserveMin;
        RecCnt = recCnt;
        RecBetweenWork = recBetweenWork;
    }

    #endregion

    #region Comment Content

    private string? GetCommentContent(int commentNum)
    {
        try
        {
            return util.ReadAllToString(Path.Combine(StartPath, $"AutoDoc/Comment/comment_{commentNum}/contents.txt"));
        }
        catch
        {
            return null;
        }
    }

    private List<ImageFile>? GetCommentImages(int commentNum)
    {
        try
        {
            return util.GetImageList(Path.Combine(StartPath, $"AutoDoc/Comment/comment_{commentNum}"));
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Comment Execution

    public async Task<bool> StartCommentAsync(CancellationToken cancellationToken)
    {
        try
        {
            for (int i = 0; (i < RecCnt || RecCnt == 0) && !cancellationToken.IsCancellationRequested; i++)
            {
                if (i != 0)
                    await Task.Delay(1000 * RecBetweenWork, cancellationToken);

                var commentList = postingService.GetPostingList("AutoDoc/Comment", "comment_");
                var commentNums = commentList.Select(c => c.Idx).OrderBy(x => x).ToList();

                if (ReservedCheck)
                    await WaitForReservedTimeAsync(cancellationToken);

                if (!util.IsOpenChrome())
                {
                    OnStatusLog?.Invoke("로그인 중입니다.");
                    bandService.Login();
                }

                if (RecCnt == 0) i = 0;

                var commentIdx = 0;
                var isFirst = true;

                foreach (var band in bandService.GetBandListFromFile())
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        util.AcceptAlert();
                        util.CloseCurrent();

                        var hasComment = DetermineCommentList(band, commentNums, ref commentIdx);
                        if (!hasComment) continue;

                        if (!isFirst)
                            await Task.Delay(1000 * BetweenWork, cancellationToken);
                        else
                            isFirst = false;

                        var content = GetCommentContent(band.CommentList.First());
                        if (string.IsNullOrEmpty(content)) continue;

                        OnStatusLog?.Invoke("url 이동");
                        util.GoToUrl($"https://band.us/band/{band.Num}", 3000);

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

                        if (!util.ClickByCss("[class='postWrap'] [data-viewname='DPostLayoutView'] [class='_commentMainBtn addStatus -comment']", 1000))
                        {
                            OnLog?.Invoke("댓글을 입력할 권한이 없습니다.");
                            continue;
                        }

                        OnStatusLog?.Invoke("내용 입력 중입니다.");

                        if (!PasteCheck)
                            util.SendKeyNoDelay("[class='commentWrite _use_keyup_event _messageTextArea']", content);
                        else
                            util.SendKeyPaste("[class='commentWrite _use_keyup_event _messageTextArea']", content);

                        util.Delay(500);

                        var images = GetCommentImages(band.CommentList.First());
                        if (images != null && images.Count > 0)
                        {
                            OnStatusLog?.Invoke("이미지 업로드 중입니다.");
                            util.ClickByCss("[class='btnUpload _btnUpload']", 300);

                            var uploadInput = util.FindElement("[class='_imageUploadArea js-fileapi-wrapper'] [class='_imageUploadButton']");
                            if (uploadInput != null)
                            {
                                var randomImg = images[new Random().Next(0, images.Count)];
                                var path = Path.Combine(randomImg.Path, randomImg.FileName);
                                uploadInput.SendKeys(path.Replace('/', Path.DirectorySeparatorChar));

                                while (!util.FindElement("[class='loading _loadingImage']")?.GetAttribute("style")?.Contains("display: none;") ?? false)
                                {
                                    util.Delay(100);
                                }
                            }
                        }

                        util.Delay(1000);
                        util.ClickByCss("[class='writeSubmit uButton _sendMessageButton -active']", 500);

                        OnLog?.Invoke($"http://band.us/band/{band.Num} - 댓글 성공");

                        if (CommentType == 0)
                            RotateCommentList(band);
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

    private bool DetermineCommentList(BandInfo band, List<int> commentNums, ref int commentIdx)
    {
        switch (CommentType)
        {
            case 0:
                return band.CommentList != null && band.CommentList.Count > 0;

            case 1:
                band.CommentList.Clear();
                band.CommentList.Add(commentNums[new Random().Next(0, commentNums.Count)]);
                return true;

            case 2:
                band.CommentList.Clear();
                band.CommentList.Add(commentNums[commentIdx]);
                commentIdx++;
                if (commentIdx >= commentNums.Count) commentIdx = 0;
                return true;

            default:
                return false;
        }
    }

    private void RotateCommentList(BandInfo band)
    {
        if (band.CommentList == null || band.CommentList.Count <= 1) return;

        var first = band.CommentList[0];
        band.CommentList.RemoveAt(0);
        band.CommentList.Add(first);

        bandService.UpdateBandPostingList(band);
    }

    #endregion
}
