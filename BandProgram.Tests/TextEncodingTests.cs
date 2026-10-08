using System.Text;

namespace BandProgram.Tests;

[Collection("Serial")]
public class TextEncodingTests
{
    [Fact]
    public void Util_reads_utf8_files()
    {
        string file = Path.Combine(Path.GetTempPath(), $"band-text-{Guid.NewGuid():N}.txt");
        File.WriteAllBytes(file, Encoding.UTF8.GetBytes("12345\t캠핑 밴드\r\n67890\t낚시\r\n"));
        try
        {
            List<string> lines = Util.getInstance().readAll(file);
            Assert.Equal(new[] { "12345\t캠핑 밴드", "67890\t낚시" }, lines);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Util_writes_utf8_without_bom()
    {
        string file = Path.Combine(Path.GetTempPath(), $"band-text-{Guid.NewGuid():N}.txt");
        try
        {
            Util.getInstance().writeStream(file, "가");
            byte[] expected = Encoding.UTF8.GetBytes("가" + Environment.NewLine); // EA B0 80, BOM 없음
            Assert.Equal(expected, File.ReadAllBytes(file));
        }
        finally
        {
            File.Delete(file);
        }
    }
}
