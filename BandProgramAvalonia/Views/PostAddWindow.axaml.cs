using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BandProgramAvalonia.Models;
using BandProgramAvalonia.Services;

namespace BandProgramAvalonia.Views;

public partial class PostAddWindow : Window
{
    public string Path { get; set; } = "AutoDoc/Posting";
    public string Separator { get; set; } = "post_";
    public Action? OnSaved { get; set; }

    private int editIndex = -1;
    private readonly ObservableCollection<string> postImages = new();
    private readonly ObservableCollection<string> commentImages = new();
    private readonly List<ImageFile> postImageFiles = new();
    private readonly List<ImageFile> commentImageFiles = new();
    private readonly PostingService postingService = new();
    private readonly Util util = Util.GetInstance();

    public PostAddWindow()
    {
        InitializeComponent();
        lstPostImages.ItemsSource = postImages;
        lstCommentImages.ItemsSource = commentImages;
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        if (Separator.Contains("post"))
            Title = editIndex == -1 ? "포스팅 원고 추가" : "포스팅 원고 수정";
        else if (Separator.Contains("comment"))
            Title = editIndex == -1 ? "댓글 원고 추가" : "댓글 원고 수정";
        else if (Separator.Contains("chatting"))
            Title = editIndex == -1 ? "대화 원고 추가" : "대화 원고 수정";
    }

    public void ApplyData(int index)
    {
        editIndex = index;
        UpdateTitle();

        var basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path, $"{Separator}{index}");

        var content = util.ReadAllToString(System.IO.Path.Combine(basePath, "contents.txt"));
        txtPostContent.Text = content ?? "";

        var images = util.GetImageList(basePath);
        if (images != null)
        {
            foreach (var img in images)
            {
                postImageFiles.Add(img);
                postImages.Add($"{img.FileName} ({img.FileSize} bytes)");
            }
        }

        if (Separator == "post_")
        {
            var commentContent = util.ReadAllToString(System.IO.Path.Combine(basePath, "comment_contents.txt"));
            if (!string.IsNullOrWhiteSpace(commentContent))
            {
                chkAddComment.IsChecked = true;
                txtCommentContent.Text = commentContent;

                var commentImgs = util.GetImageList(System.IO.Path.Combine(basePath, "comment_images"));
                if (commentImgs != null)
                {
                    foreach (var img in commentImgs)
                    {
                        commentImageFiles.Add(img);
                        commentImages.Add($"{img.FileName} ({img.FileSize} bytes)");
                    }
                }
            }
        }
    }

    private async void BtnAddPostImage_Click(object? sender, RoutedEventArgs e)
    {
        await AddImagesAsync(postImageFiles, postImages, radioPostFolder.IsChecked == true);
    }

    private async void BtnAddCommentImage_Click(object? sender, RoutedEventArgs e)
    {
        await AddImagesAsync(commentImageFiles, commentImages, radioCommentFolder.IsChecked == true);
    }

    private async Task AddImagesAsync(List<ImageFile> imageFiles, ObservableCollection<string> displayList, bool isFolder)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        if (isFolder)
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var folderPath = folders[0].Path.LocalPath;
                var files = Directory.GetFiles(folderPath);

                foreach (var file in files)
                {
                    var ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext is ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".png")
                    {
                        var fileName = System.IO.Path.GetFileName(file);
                        var fileInfo = new FileInfo(file);
                        var imgFile = new ImageFile(fileName, folderPath + System.IO.Path.DirectorySeparatorChar, fileInfo.Length);
                        imageFiles.Add(imgFile);
                        displayList.Add($"{fileName} ({fileInfo.Length} bytes)");
                    }
                }
            }
        }
        else
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Image",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files") { Patterns = new[] { "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.png" } }
                }
            });

            foreach (var file in files)
            {
                var filePath = file.Path.LocalPath;
                var fileName = System.IO.Path.GetFileName(filePath);
                var dirPath = System.IO.Path.GetDirectoryName(filePath) + System.IO.Path.DirectorySeparatorChar;
                var fileInfo = new FileInfo(filePath);
                var imgFile = new ImageFile(fileName, dirPath, fileInfo.Length);
                imageFiles.Add(imgFile);
                displayList.Add($"{fileName} ({fileInfo.Length} bytes)");
            }
        }
    }

    private void BtnRemovePostImage_Click(object? sender, RoutedEventArgs e)
    {
        if (lstPostImages.SelectedIndex >= 0)
        {
            postImageFiles.RemoveAt(lstPostImages.SelectedIndex);
            postImages.RemoveAt(lstPostImages.SelectedIndex);
        }
    }

    private void BtnRemoveCommentImage_Click(object? sender, RoutedEventArgs e)
    {
        if (lstCommentImages.SelectedIndex >= 0)
        {
            commentImageFiles.RemoveAt(lstCommentImages.SelectedIndex);
            commentImages.RemoveAt(lstCommentImages.SelectedIndex);
        }
    }

    private void BtnSave_Click(object? sender, RoutedEventArgs e)
    {
        var postNum = editIndex != -1 ? editIndex : postingService.GetNextPostingNumber(Path, Separator);
        var targetDir = System.IO.Path.Combine(Path, $"{Separator}{postNum}");

        bool success;
        if (Separator == "post_" && chkAddComment.IsChecked == true)
        {
            success = postingService.SavePostingWithComment(
                targetDir,
                txtPostContent.Text ?? "",
                postImageFiles,
                txtCommentContent.Text ?? "",
                commentImageFiles);
        }
        else
        {
            success = postingService.SavePosting(
                targetDir,
                txtPostContent.Text ?? "",
                postImageFiles);
        }

        if (success)
        {
            OnSaved?.Invoke();
            Close();
        }
    }
}
