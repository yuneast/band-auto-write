namespace BandProgramAvalonia.Models;

public class Global
{
    private static Global? _instance;

    public static Global Instance
    {
        get
        {
            _instance ??= new Global();
            return _instance;
        }
    }

    public Global()
    {
        _instance = this;
    }

    public bool IsTest { get; set; } = false;
    public bool IsLocalLogin { get; set; } = false;
}
