using System.Text;

namespace BandProgram.Dev;

internal sealed class ShellCommand
{
    public string Name { get; private set; } = "";
    public List<string> Args { get; } = new();
    public Dictionary<string, string> Options { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string Rest { get; private set; } = "";

    public static ShellCommand Parse(string line)
    {
        var cmd = new ShellCommand();
        string trimmed = (line ?? "").Trim();
        if (trimmed.Length == 0) return cmd;

        int space = trimmed.IndexOf(' ');
        cmd.Name = (space < 0 ? trimmed : trimmed[..space]).ToLowerInvariant();
        cmd.Rest = space < 0 ? "" : trimmed[(space + 1)..].Trim();

        List<string> tokens = Tokenize(cmd.Rest);
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].StartsWith("--") && i + 1 < tokens.Count)
            {
                cmd.Options[tokens[i][2..]] = tokens[i + 1];
                i++;
            }
            else
            {
                cmd.Args.Add(tokens[i]);
            }
        }
        return cmd;
    }

    public int IntOption(string key, int fallback)
    {
        return Options.TryGetValue(key, out string value) && int.TryParse(value, out int n) ? n : fallback;
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        bool quoted = false;
        foreach (char c in text)
        {
            if (c == '"') { quoted = !quoted; continue; }
            if (c == ' ' && !quoted)
            {
                if (current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); }
                continue;
            }
            current.Append(c);
        }
        if (current.Length > 0) tokens.Add(current.ToString());
        return tokens;
    }
}
