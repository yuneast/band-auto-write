using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BandProgramAvalonia.Models;
using BandProgramAvalonia.Services;

namespace BandProgramAvalonia.Views;

public partial class MainWindow : Window
{
    private readonly string userId;
    private readonly ObservableCollection<BandListItem> bandList = new();
    private readonly ObservableCollection<string> logList = new();
    private CancellationTokenSource? cts;

    public MainWindow()
    {
        InitializeComponent();
        userId = "";
        Initialize();
    }

    public MainWindow(string userId)
    {
        InitializeComponent();
        this.userId = userId;
        Initialize();
    }

    private void Initialize()
    {
        lstBandList.ItemsSource = bandList;
        lstLog.ItemsSource = logList;

        // 예약 시간/분 ComboBox 초기화
        InitializeReservedTimeComboBoxes();

        lblUserInfo.Text = $"{userId}님 안녕하세요.";
        PrintLog("프로그램 시작");
        LoadBandListFromFile();
    }

    private void InitializeReservedTimeComboBoxes()
    {
        // 시간 (00-23)
        for (int i = 0; i < 24; i++)
        {
            var hourStr = i.ToString("00");
            cboPostReservedHour.Items.Add(hourStr);
            cboCommentReservedHour.Items.Add(hourStr);
            cboChatReservedHour.Items.Add(hourStr);
        }
        cboPostReservedHour.SelectedIndex = 0;
        cboCommentReservedHour.SelectedIndex = 0;
        cboChatReservedHour.SelectedIndex = 0;

        // 분 (00-59)
        for (int i = 0; i < 60; i++)
        {
            var minStr = i.ToString("00");
            cboPostReservedMinute.Items.Add(minStr);
            cboCommentReservedMinute.Items.Add(minStr);
            cboChatReservedMinute.Items.Add(minStr);
        }
        cboPostReservedMinute.SelectedIndex = 0;
        cboCommentReservedMinute.SelectedIndex = 0;
        cboChatReservedMinute.SelectedIndex = 0;
    }

    private void PrintLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yy-MM-dd HH:mm:ss");
        logList.Insert(0, $"[{timestamp}] {message}");
    }

    private void LoadBandListFromFile()
    {
        try
        {
            var util = Util.GetInstance();
            var lines = util.ReadAll("bandList.txt");
            if (lines == null) return;

            bandList.Clear();
            int index = 1;
            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length >= 5)
                {
                    bandList.Add(new BandListItem
                    {
                        Index = index++,
                        Name = parts[0],
                        Num = parts[1],
                        PostingListStr = parts[2],
                        CommentListStr = parts[3],
                        ChattingListStr = parts[4]
                    });
                }
            }
            lblBandCount.Text = $"밴드 리스트 ({bandList.Count})";
        }
        catch (Exception ex)
        {
            PrintLog($"밴드 리스트 로드 오류: {ex.Message}");
        }
    }

    private async void BtnLoadBands_Click(object? sender, RoutedEventArgs e)
    {
        btnLoadBands.IsEnabled = false;
        PrintLog("밴드 리스트 불러오는 중...");

        try
        {
            await Task.Run(() =>
            {
                var util = Util.GetInstance();
                if (!util.IsOpenChrome())
                {
                    util.StartChrome(0);
                }
            });

            PrintLog("밴드 리스트 불러오기 완료");
            LoadBandListFromFile();
        }
        catch (Exception ex)
        {
            PrintLog($"오류: {ex.Message}");
        }
        finally
        {
            btnLoadBands.IsEnabled = true;
        }
    }

    private void BtnSelectAll_Click(object? sender, RoutedEventArgs e)
    {
        lstBandList.SelectAll();
        PrintLog("전체 선택");
    }

    private void BtnDeleteSelected_Click(object? sender, RoutedEventArgs e)
    {
        var selected = lstBandList.SelectedItems.Cast<BandListItem>().ToList();
        foreach (var item in selected)
        {
            bandList.Remove(item);
        }

        // Reindex
        int index = 1;
        foreach (var item in bandList)
        {
            item.Index = index++;
        }

        lblBandCount.Text = $"밴드 리스트 ({bandList.Count})";
        SaveBandList();
        PrintLog($"{selected.Count}개 항목 삭제");
    }

    private void SaveBandList()
    {
        try
        {
            var util = Util.GetInstance();
            util.CreateNotePad("bandList.txt");
            foreach (var band in bandList)
            {
                util.WriteStream("bandList.txt",
                    $"{band.Name}\t{band.Num}\t{band.PostingListStr}\t{band.CommentListStr}\t{band.ChattingListStr}");
            }
        }
        catch (Exception ex)
        {
            PrintLog($"저장 오류: {ex.Message}");
        }
    }

    private async void BtnStartPost_Click(object? sender, RoutedEventArgs e)
    {
        cts = new CancellationTokenSource();
        btnStartPost.IsEnabled = false;
        btnPausePost.IsEnabled = true;
        PrintLog("포스팅 작업 시작...");

        try
        {
            await Task.Run(() =>
            {
                // Posting logic here
                PrintLog("포스팅 작업 완료");
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            PrintLog("포스팅 작업 일시정지");
        }
        finally
        {
            btnStartPost.IsEnabled = true;
            btnPausePost.IsEnabled = false;
        }
    }

    private void BtnStopPost_Click(object? sender, RoutedEventArgs e)
    {
        cts?.Cancel();
    }

    private async void BtnStartComment_Click(object? sender, RoutedEventArgs e)
    {
        cts = new CancellationTokenSource();
        btnStartComment.IsEnabled = false;
        btnPauseComment.IsEnabled = true;
        PrintLog("댓글 작업 시작...");

        try
        {
            await Task.Run(() =>
            {
                // Comment logic here
                PrintLog("댓글 작업 완료");
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            PrintLog("댓글 작업 일시정지");
        }
        finally
        {
            btnStartComment.IsEnabled = true;
            btnPauseComment.IsEnabled = false;
        }
    }

    private void BtnStopComment_Click(object? sender, RoutedEventArgs e)
    {
        cts?.Cancel();
    }

    private async void BtnStartChat_Click(object? sender, RoutedEventArgs e)
    {
        cts = new CancellationTokenSource();
        btnStartChat.IsEnabled = false;
        btnPauseChat.IsEnabled = true;
        PrintLog("대화 작업 시작...");

        try
        {
            await Task.Run(() =>
            {
                // Chat logic here
                PrintLog("대화 작업 완료");
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            PrintLog("대화 작업 일시정지");
        }
        finally
        {
            btnStartChat.IsEnabled = true;
            btnPauseChat.IsEnabled = false;
        }
    }

    private void BtnStopChat_Click(object? sender, RoutedEventArgs e)
    {
        cts?.Cancel();
    }

    private async void BtnAddPost_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new PostAddWindow
            {
                Path = "AutoDoc/Posting",
                Separator = "post_"
            };
            dialog.OnSaved = () => {
                PrintLog("포스팅 원고 추가 완료");
                // TODO: 리스트 새로고침
            };
            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            PrintLog($"포스팅 원고 추가 오류: {ex.Message}");
        }
    }

    private void BtnDeletePost_Click(object? sender, RoutedEventArgs e)
    {
        if (lstPostList.SelectedItem != null)
        {
            lstPostList.Items.Remove(lstPostList.SelectedItem);
            PrintLog("게시글 삭제 완료");
        }
    }

    private async void BtnAddComment_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new PostAddWindow
            {
                Path = "AutoDoc/Comment",
                Separator = "comment_"
            };
            dialog.OnSaved = () => {
                PrintLog("댓글 원고 추가 완료");
                // TODO: 리스트 새로고침
            };
            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            PrintLog($"댓글 원고 추가 오류: {ex.Message}");
        }
    }

    private void BtnDeleteComment_Click(object? sender, RoutedEventArgs e)
    {
        if (lstCommentList.SelectedItem != null)
        {
            lstCommentList.Items.Remove(lstCommentList.SelectedItem);
            PrintLog("댓글 삭제 완료");
        }
    }

    private async void BtnAddChat_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new PostAddWindow
            {
                Path = "AutoDoc/Chatting",
                Separator = "chatting_"
            };
            dialog.OnSaved = () => {
                PrintLog("대화 원고 추가 완료");
                // TODO: 리스트 새로고침
            };
            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            PrintLog($"대화 원고 추가 오류: {ex.Message}");
        }
    }

    private void BtnDeleteChat_Click(object? sender, RoutedEventArgs e)
    {
        if (lstChatList.SelectedItem != null)
        {
            lstChatList.Items.Remove(lstChatList.SelectedItem);
            PrintLog("대화 삭제 완료");
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            Util.GetInstance().CloseChrome();
        }
        catch { }
        base.OnClosing(e);
    }
}

public class BandListItem : INotifyPropertyChanged
{
    private bool isSelected;
    private int index;
    private string name = string.Empty;
    private string num = string.Empty;
    private string postingListStr = string.Empty;
    private string commentListStr = string.Empty;
    private string chattingListStr = string.Empty;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected != value)
            {
                isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public int Index
    {
        get => index;
        set
        {
            if (index != value)
            {
                index = value;
                OnPropertyChanged(nameof(Index));
            }
        }
    }

    public string Name
    {
        get => name;
        set
        {
            if (name != value)
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string Num
    {
        get => num;
        set
        {
            if (num != value)
            {
                num = value;
                OnPropertyChanged(nameof(Num));
            }
        }
    }

    public string PostingListStr
    {
        get => postingListStr;
        set
        {
            if (postingListStr != value)
            {
                postingListStr = value;
                OnPropertyChanged(nameof(PostingListStr));
            }
        }
    }

    public string CommentListStr
    {
        get => commentListStr;
        set
        {
            if (commentListStr != value)
            {
                commentListStr = value;
                OnPropertyChanged(nameof(CommentListStr));
            }
        }
    }

    public string ChattingListStr
    {
        get => chattingListStr;
        set
        {
            if (chattingListStr != value)
            {
                chattingListStr = value;
                OnPropertyChanged(nameof(ChattingListStr));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
