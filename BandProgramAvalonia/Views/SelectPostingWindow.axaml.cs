using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BandProgramAvalonia.Views;

public partial class SelectPostingWindow : Window
{
    public Action<int, List<int>, List<int>, List<int>>? OnApply { get; set; }
    public List<int>? IndexList { get; set; }

    private int bandIndex;

    public SelectPostingWindow()
    {
        InitializeComponent();
    }

    public void ApplyData(int index, string bandName, string bandId,
        string postingContents, string commentContents, string chattingContents)
    {
        bandIndex = index;
        txtBandName.Text = bandName;
        txtBandId.Text = bandId;
        txtPosting.Text = postingContents;
        txtComment.Text = commentContents;
        txtChatting.Text = chattingContents;
    }

    private void BtnApply_Click(object? sender, RoutedEventArgs e)
    {
        var postingList = ParseIntList(txtPosting.Text);
        var commentList = ParseIntList(txtComment.Text);
        var chattingList = ParseIntList(txtChatting.Text);

        if (IndexList != null)
        {
            foreach (var idx in IndexList)
            {
                OnApply?.Invoke(idx, postingList, commentList, chattingList);
            }
        }
        else
        {
            OnApply?.Invoke(bandIndex, postingList, commentList, chattingList);
        }

        Close();
    }

    private List<int> ParseIntList(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return new List<int>();

        var result = new List<int>();
        foreach (var part in str.Split(','))
        {
            if (int.TryParse(part.Trim(), out var num))
                result.Add(num);
        }
        return result;
    }
}
