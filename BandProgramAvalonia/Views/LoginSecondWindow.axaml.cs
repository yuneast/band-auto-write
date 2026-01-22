using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using BandProgramAvalonia.Services;
using BandProgramAvalonia.Models;
using System.Linq;

namespace BandProgramAvalonia.Views;

public partial class LoginSecondWindow : Window
{
    private readonly string fileName = "bandAccount.txt";
    private readonly Util util = Util.GetInstance();
    private readonly BandService bandService;
    private readonly CookieContainer? cookie;
    private readonly string loginId;
    private bool loginSuccess = false;

    public ObservableCollection<AccountInfo> Accounts { get; } = new();

    public LoginSecondWindow() : this(null, "") { }

    public LoginSecondWindow(CookieContainer? cookie, string loginId)
    {
        System.Diagnostics.Debug.WriteLine("[생성자] LoginSecondWindow 시작");

        InitializeComponent();

        System.Diagnostics.Debug.WriteLine("[생성자] InitializeComponent 완료");

        this.cookie = cookie;
        this.loginId = loginId;
        this.bandService = new BandService();

        System.Diagnostics.Debug.WriteLine($"[생성자] lstAccounts null 여부: {lstAccounts == null}");

        // ListBox ItemsSource 설정
        lstAccounts.ItemsSource = Accounts;

        System.Diagnostics.Debug.WriteLine("[생성자] ItemsSource 설정 완료");

        // 파일에서 계정 목록 로드
        LoadAccounts();

        System.Diagnostics.Debug.WriteLine($"[생성자] LoadAccounts 완료. Accounts.Count = {Accounts.Count}");

        // 로그인 체크 (원본 LoginSecond_Load와 동일)
        LoginCheck();

        System.Diagnostics.Debug.WriteLine("[생성자] LoginSecondWindow 생성 완료");
    }

    private void LoginCheck()
    {
        // 원본 코드의 loginCheck() 메서드 로직
        // 세션 체크 로직 - 필요시 구현
    }

    private void LoadAccounts()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LoadAccounts] 파일 경로: {fileName}");
            System.Diagnostics.Debug.WriteLine($"[LoadAccounts] 파일 존재 여부: {File.Exists(fileName)}");

            var lines = util.ReadAll(fileName);

            if (lines == null)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadAccounts] lines == null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[LoadAccounts] 읽은 라인 수: {lines.Count}");

            foreach (var line in lines)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadAccounts] 라인: {line}");
                var parts = line.Split('\t');
                System.Diagnostics.Debug.WriteLine($"[LoadAccounts] parts 개수: {parts.Length}");

                if (parts.Length >= 3)
                {
                    var id = parts[0];
                    var encodedPassword = parts[1];
                    var loginType = parts[2];

                    System.Diagnostics.Debug.WriteLine($"[LoadAccounts] 추가: {id} / {encodedPassword} / {loginType}");

                    Accounts.Add(new AccountInfo
                    {
                        Id = id,
                        Password = encodedPassword, // Base64로 저장되어 있음
                        LoginType = loginType
                    });
                }
            }

            System.Diagnostics.Debug.WriteLine($"[LoadAccounts] 최종 Accounts 개수: {Accounts.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadAccounts] Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LoadAccounts] StackTrace: {ex.StackTrace}");
        }
    }

    private void BtnAdd_Click(object? sender, RoutedEventArgs e)
    {
        // 원본: buttonAdd_Click
        var id = txtId.Text;
        var password = txtPassword.Text;
        var loginType = (cboLoginType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "전화번호";

        System.Diagnostics.Debug.WriteLine($"[BtnAdd] ID: {id}, PW: {password}, Type: {loginType}");

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
        {
            System.Diagnostics.Debug.WriteLine("[BtnAdd] ID 또는 PW가 비어있음");
            return;
        }

        // Base64로 인코딩
        var encodedPassword = util.Base64Encoding(password);
        System.Diagnostics.Debug.WriteLine($"[BtnAdd] Base64 인코딩된 비밀번호: {encodedPassword}");

        // ListView에 추가
        Accounts.Add(new AccountInfo
        {
            Id = id,
            Password = encodedPassword,
            LoginType = loginType
        });

        System.Diagnostics.Debug.WriteLine($"[BtnAdd] Accounts에 추가 완료. Count = {Accounts.Count}");

        // 파일에 저장
        var line = $"{id}\t{encodedPassword}\t{loginType}";
        System.Diagnostics.Debug.WriteLine($"[BtnAdd] 파일에 저장할 라인: {line}");
        util.WriteStream(fileName, line);

        // 입력 필드 초기화
        txtId.Clear();
        txtPassword.Clear();

        System.Diagnostics.Debug.WriteLine("[BtnAdd] 입력 필드 초기화 완료");
    }

    private void MenuDeleteSelected_Click(object? sender, RoutedEventArgs e)
    {
        // 원본: removeItem()
        RemoveItem();
    }

    private void MenuDeleteAll_Click(object? sender, RoutedEventArgs e)
    {
        // 원본: removeAll()
        RemoveAll();
    }

    private void RemoveItem()
    {
        if (lstAccounts.SelectedItem is AccountInfo selected && lstAccounts.SelectedIndex >= 0)
        {
            var index = lstAccounts.SelectedIndex;
            util.RemoveALine(fileName, index);
            Accounts.RemoveAt(index);
        }
    }

    private void RemoveAll()
    {
        util.CreateNotePad(fileName);
        Accounts.Clear();
    }

    private async void BtnLogin_Click(object? sender, RoutedEventArgs e)
    {
        // 원본: button1_Click → bandLogin()
        if (lstAccounts.SelectedItem is not AccountInfo selected)
            return;

        btnLogin.IsEnabled = false;

        try
        {
            // Base64 디코딩된 비밀번호 가져오기
            var id = selected.Id;
            var password = util.Base64Decoding(selected.Password);
            var loginType = selected.LoginType;

            // setBandAccount 호출
            util.SetBandAccount(id, password, loginType);

            // 로그인 시도
            var success = await System.Threading.Tasks.Task.Run(() =>
            {
                var fl = new BandService();
                return fl.Login(id, password, loginType);
            });

            if (!success)
            {
                util.CloseChrome();
                await ShowMessageBox("로그인 실패", "로그인에 실패하였습니다.");
                btnLogin.IsEnabled = true;
                return;
            }

            // 로그인 성공
            System.Diagnostics.Debug.WriteLine("[BtnLogin] 로그인 성공!");
            loginSuccess = true;

            // 원본: base.Visible = false
            this.Opacity = 0;

            System.Diagnostics.Debug.WriteLine("[BtnLogin] MainWindow 생성 및 표시");

            // MainWindow 표시
            var mainWindow = new MainWindow(id);
            mainWindow.Show();

            System.Diagnostics.Debug.WriteLine("[BtnLogin] MainWindow.Show() 완료");

            // LoginSecond 창 닫기
            this.Close();
        }
        catch (Exception ex)
        {
            await ShowMessageBox("오류", $"로그인 중 오류가 발생했습니다: {ex.Message}");
            btnLogin.IsEnabled = true;
        }
    }

    private async System.Threading.Tasks.Task ShowMessageBox(string title, string message)
    {
        var msgBox = new Window
        {
            Title = title,
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20) };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        var btn = new Button
        {
            Content = "확인",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 20, 0, 0)
        };
        btn.Click += (s, e) => msgBox.Close();
        panel.Children.Add(btn);

        msgBox.Content = panel;

        await msgBox.ShowDialog(this);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // 원본: LoginSecond_FormClosed
        // 로그인 성공이 아닌 경우에만 브라우저 닫기
        if (!loginSuccess)
        {
            bandService.CloseChrome();
        }
        base.OnClosing(e);
    }
}
