using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BandProgramAvalonia.Models;

namespace BandProgramAvalonia.Views;

public partial class LoginWindow : Window
{
    private static readonly HttpClient httpClient = new();
    private readonly string accFilePath;

    public LoginWindow()
    {
        InitializeComponent();
        accFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "acc.txt");
        LoadSavedCredentials();
    }

    private void LoadSavedCredentials()
    {
        if (File.Exists(accFilePath))
        {
            try
            {
                var acc = File.ReadAllText(accFilePath);
                var parts = acc.Split('|');
                if (parts.Length >= 2)
                {
                    txtId.Text = parts[0];
                    txtPassword.Text = parts[1];
                }
            }
            catch { }
        }
    }

    private async void BtnLogin_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(txtId.Text) || string.IsNullOrEmpty(txtPassword.Text))
        {
            lblStatus.Text = "아이디와 비밀번호를 입력하세요";
            return;
        }

        btnLogin.IsEnabled = false;
        lblStatus.Text = "로그인 중...";

        try
        {
            var urlStr = Global.Instance.IsLocalLogin
                ? "http://127.0.0.1:3000/login"
                : "http://newsoft.kr/login.php?action=login";

            var content = new StringContent(
                $"id={txtId.Text}&pass={txtPassword.Text}&type=band",
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            var response = await httpClient.PostAsync(urlStr, content);
            var result = await response.Content.ReadAsStringAsync();

            // Debug: Show response
            System.Diagnostics.Debug.WriteLine($"Login response: {result}");

            if (result.Contains("기간만료"))
            {
                lblStatus.Text = "기간이 만료되었습니다";
            }
            else if (result.Contains("성공"))
            {
                // Save credentials
                await File.WriteAllTextAsync(accFilePath, $"{txtId.Text}|{txtPassword.Text}");

                // Open band account selection window
                var loginSecond = new LoginSecondWindow(null, txtId.Text ?? "");
                loginSecond.Show();
                this.Close();
            }
            else
            {
                lblStatus.Text = $"로그인 실패 (응답: {result.Substring(0, Math.Min(100, result.Length))})";
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"오류: {ex.Message}";
        }
        finally
        {
            btnLogin.IsEnabled = true;
        }
    }
}
