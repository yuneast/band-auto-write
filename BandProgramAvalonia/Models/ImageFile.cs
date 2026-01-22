namespace BandProgramAvalonia.Models;

public class ImageFile
{
    public string FileName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public ImageFile() { }

    public ImageFile(string fileName, string path)
    {
        FileName = fileName;
        Path = path;
    }

    public ImageFile(string fileName, string path, long fileSize)
    {
        FileName = fileName;
        Path = path;
        FileSize = fileSize;
    }
}
