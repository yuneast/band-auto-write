namespace BandProgramAvalonia.Models;

public class AccountInfo
{
    public string Id { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string LoginType { get; set; } = string.Empty;

    public string PasswordMasked => new string('*', Password.Length);

    public AccountInfo() { }

    public AccountInfo(string id, string password)
    {
        Id = id;
        Password = password;
    }

    public AccountInfo(string id, string password, string loginType)
    {
        Id = id;
        Password = password;
        LoginType = loginType;
    }
}
