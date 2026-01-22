using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BandProgramAvalonia.Models;

namespace BandProgramAvalonia.Services;

public class PostingService
{
    private readonly Util util = Util.GetInstance();
    private readonly BandService bandService = new();
    private static readonly string StartPath = AppDomain.CurrentDomain.BaseDirectory;

    public Action<string>? OnLog { get; set; }
    public Action<string>? OnStatusLog { get; set; }

    #region Parameters

    public int PostType { get; set; }
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
        PostType = type;
        BetweenWork = betweenWork;
        ReservedCheck = reservedCheck;
        PasteCheck = pasteCheck;
        ReserveHour = reserveHour;
        ReserveMin = reserveMin;
        RecCnt = recCnt;
        RecBetweenWork = recBetweenWork;
    }

    #endregion

    #region Post Content Management

    public List<Post> GetPostingList(string path, string separator)
    {
        try
        {
            var posts = new List<Post>();
            var fullPath = Path.Combine(StartPath, path);

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            var directories = new DirectoryInfo(fullPath).GetDirectories();

            foreach (var dir in directories)
            {
                if (dir.Name.Contains(separator))
                {
                    var post = new Post();
                    post.Idx = int.Parse(dir.Name.Substring(separator.Length));
                    post.Contents = util.ReadAllToString(Path.Combine(fullPath, $"{separator}{post.Idx}", "contents.txt")) ?? "";
                    post.Images = util.GetImageList(Path.Combine(fullPath, $"{separator}{post.Idx}")) ?? new List<ImageFile>();

                    if (separator == "post_")
                    {
                        post.CommentContents = util.ReadAllToString(Path.Combine(fullPath, $"{separator}{post.Idx}", "comment_contents.txt")) ?? "";
                        post.CommentImages = util.GetImageList(Path.Combine(fullPath, $"{separator}{post.Idx}", "comment_images")) ?? new List<ImageFile>();
                        post.HasComment = !string.IsNullOrWhiteSpace(post.CommentContents);
                    }

                    posts.Add(post);
                }
            }
            return posts;
        }
        catch
        {
            return new List<Post>();
        }
    }

    public int GetNextPostingNumber(string path, string separator)
    {
        try
        {
            var fullPath = Path.Combine(StartPath, path);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            var directories = new DirectoryInfo(fullPath).GetDirectories();
            var nums = new List<int>();

            foreach (var dir in directories)
            {
                if (dir.Name.Contains(separator))
                {
                    if (int.TryParse(dir.Name.Substring(separator.Length), out var num))
                        nums.Add(num);
                }
            }

            nums.Sort();
            var nextNum = 1;
            foreach (var n in nums)
            {
                if (nextNum != n) break;
                nextNum++;
            }
            return nextNum;
        }
        catch
        {
            return 1;
        }
    }

    public bool SavePosting(string newDir, string contents, List<ImageFile> imageFiles)
    {
        try
        {
            var fullPath = Path.Combine(StartPath, newDir);
            Directory.CreateDirectory(fullPath);

            File.WriteAllText(Path.Combine(fullPath, "contents.txt"), contents, System.Text.Encoding.UTF8);

            foreach (var img in imageFiles)
            {
                var srcPath = Path.Combine(img.Path, img.FileName);
                var destPath = Path.Combine(fullPath, img.FileName);
                if (File.Exists(srcPath))
                    File.Copy(srcPath, destPath, true);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool SavePostingWithComment(string newDir, string contents, List<ImageFile> imageFiles,
        string commentContents, List<ImageFile> commentImageFiles)
    {
        try
        {
            var fullPath = Path.Combine(StartPath, newDir);
            Directory.CreateDirectory(fullPath);

            var commentImagesPath = Path.Combine(fullPath, "comment_images");
            Directory.CreateDirectory(commentImagesPath);

            File.WriteAllText(Path.Combine(fullPath, "contents.txt"), contents, System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(fullPath, "comment_contents.txt"), commentContents, System.Text.Encoding.UTF8);

            foreach (var img in imageFiles)
            {
                var srcPath = Path.Combine(img.Path, img.FileName);
                var destPath = Path.Combine(fullPath, img.FileName);
                if (File.Exists(srcPath))
                    File.Copy(srcPath, destPath, true);
            }

            foreach (var img in commentImageFiles)
            {
                var srcPath = Path.Combine(img.Path, img.FileName);
                var destPath = Path.Combine(commentImagesPath, img.FileName);
                if (File.Exists(srcPath))
                    File.Copy(srcPath, destPath, true);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void DeletePosting(string path)
    {
        try
        {
            var fullPath = Path.Combine(StartPath, path);
            if (Directory.Exists(fullPath))
                Directory.Delete(fullPath, true);
        }
        catch { }
    }

    #endregion

    #region Posting Execution

    public async Task<bool> StartPostingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var postList = GetPostingList("AutoDoc/Posting", "post_");
            var postNums = postList.Select(p => p.Idx).OrderBy(x => x).ToList();

            for (int i = 0; (i < RecCnt || RecCnt == 0) && !cancellationToken.IsCancellationRequested; i++)
            {
                if (i != 0)
                    await Task.Delay(1000 * RecBetweenWork, cancellationToken);

                if (RecCnt == 0) i = 0;

                var postIdx = 0;
                var isFirst = true;

                foreach (var band in bandService.GetBandListFromFile())
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        util.AcceptAlert();
                        util.CloseCurrent();

                        if (!util.IsOpenChrome())
                        {
                            OnStatusLog?.Invoke("로그인 중입니다.");
                            bandService.Login();
                        }

                        if (ReservedCheck)
                        {
                            OnStatusLog?.Invoke("예약 대기 중입니다.");
                            await WaitForReservedTimeAsync(cancellationToken);
                        }

                        var hasPosting = DeterminePostingList(band, postNums, ref postIdx);
                        if (!hasPosting) continue;

                        if (!isFirst)
                            await Task.Delay(1000 * BetweenWork, cancellationToken);
                        else
                            isFirst = false;

                        await ExecutePostingAsync(band, postList, cancellationToken);
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

    private bool DeterminePostingList(BandInfo band, List<int> postNums, ref int postIdx)
    {
        switch (PostType)
        {
            case 0: // Assigned
                return band.PostingList != null && band.PostingList.Count > 0;

            case 1: // Random
                band.PostingList.Clear();
                var randomIdx = new Random().Next(0, postNums.Count);
                band.PostingList.Add(postNums[randomIdx]);
                return true;

            case 2: // Sequential
                band.PostingList.Clear();
                band.PostingList.Add(postNums[postIdx]);
                postIdx++;
                if (postIdx >= postNums.Count) postIdx = 0;
                return true;

            default:
                return false;
        }
    }

    private async Task ExecutePostingAsync(BandInfo band, List<Post> postList, CancellationToken cancellationToken)
    {
        try
        {
            var postNum = band.PostingList.First();
            var post = postList.FirstOrDefault(p => p.Idx == postNum);
            if (post == null) return;

            OnStatusLog?.Invoke("url 이동");
            util.GoToUrl($"https://band.us/band/{band.Num}", 3000);

            if (util.AcceptAlert())
            {
                OnLog?.Invoke($"https://band.us/band/{band.Num} - 작업 불가능한 밴드입니다.");
                return;
            }

            if (util.FindElementWithRec("[class='uButton -sizeL -confirm _btnJoinBand']", 3) != null)
            {
                OnLog?.Invoke("가입이 되어있지 않습니다.");
                return;
            }

            OnStatusLog?.Invoke("포스팅 내용작성 중입니다.");

            if (!util.ClickByCssNew("[class='cPostWriteEventWrapper _btnOpenWriteLayer']", 1000, 0))
            {
                OnLog?.Invoke($"https://band.us/band/{band.Num} - 포스팅을 작성할 권한이 없습니다.");
                return;
            }

            util.ClickByCss("[class='contentEditor _richEditor skin8 cke_editable cke_editable_inline cke_contents_ltr']", 500);

            if (!PasteCheck)
                util.SendKeyNoDelay("[contenteditable='true']", post.Contents);
            else
                util.SendKeyPaste("[contenteditable='true']", post.Contents);

            util.Delay(500);

            if (post.Images != null && post.Images.Count > 0)
            {
                OnStatusLog?.Invoke("이미지 업로드 중입니다.");
                await UploadImagesAsync(post.Images);
            }

            util.Delay(2000);
            util.ClickByCssNew("[class='uButton -sizeM _btnSubmitPost -confirm']", 500, 0);
            util.Delay(2000);

            OnLog?.Invoke($"http://band.us/band/{band.Num} - 포스팅 성공");

            // Post comment if exists
            if (post.HasComment && !string.IsNullOrWhiteSpace(post.CommentContents))
            {
                await PostCommentAsync(band, post, cancellationToken);
            }
        }
        catch (Exception)
        {
            OnLog?.Invoke($"https://band.us/band/{band.Num} - 작업 불가능한 밴드입니다.");
            util.AcceptAlert();
            util.CloseCurrent();
        }
    }

    private async Task UploadImagesAsync(List<ImageFile> images)
    {
        var uploadInput = util.FindElement("[class='photo _btnAttachPhoto _attachWidgetBtn js-fileapi-wrapper'] [type='file']");
        if (uploadInput == null) return;

        var paths = images.Select(img => Path.Combine(img.Path, img.FileName)).ToList();
        var pathStr = string.Join("\n", paths);
        uploadInput.SendKeys(pathStr);

        util.Delay(500);

        while (!util.GetPageSource().Contains("uButton -confirm _submitBtn"))
        {
            util.Delay(100);
        }

        util.Delay(500);
        util.ClickByCssNew("[class='modalFooter'] [class='uButton -confirm _submitBtn']", 500, 0);

        while (util.FindElement("[class='uButton -sizeM _btnSubmitPost -confirm']") == null)
        {
            util.Delay(100);
        }
    }

    private async Task PostCommentAsync(BandInfo band, Post post, CancellationToken cancellationToken)
    {
        OnStatusLog?.Invoke("댓글 준비중입니다.");
        util.Delay(2000);

        if (!util.ClickByCssNew("[class='_commentMainBtn addStatus -comment']", 1000, 0))
            return;

        OnStatusLog?.Invoke("댓글내용 입력 중입니다.");

        if (!PasteCheck)
            util.SendKeyNoDelay("[class='commentWrite _use_keyup_event _messageTextArea']", post.CommentContents);
        else
            util.SendKeyPaste("[class='commentWrite _use_keyup_event _messageTextArea']", post.CommentContents);

        util.Delay(500);

        if (post.CommentImages != null && post.CommentImages.Count > 0)
        {
            OnStatusLog?.Invoke("이미지 업로드 중입니다.");
            util.ClickByCssNew("[class='btnUpload _btnUpload']", 300, 0);

            var uploadInput = util.FindElement("[class='inputUploadFile _imageUploadButton']");
            if (uploadInput != null)
            {
                var paths = post.CommentImages.Select(img => Path.Combine(img.Path, img.FileName)).ToList();
                uploadInput.SendKeys(string.Join("\n", paths));
            }
        }

        util.Delay(1000);
        util.ClickByCssNew("[class='writeSubmit uButton _sendMessageButton -active']", 500, 0);

        OnLog?.Invoke($"http://band.us/band/{band.Num} - 댓글 성공");
    }

    #endregion
}
