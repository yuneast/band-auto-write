using System.Drawing;

namespace BandProgram.Tests;

public class PointerOffsetTests
{
    // Selenium 4의 MoveToElement(element, x, y)는 요소 중앙 기준이다(Selenium 3은 왼쪽 위 기준).
    // 기존 클릭 위치(왼쪽 위에서 너비·높이의 40~70%)를 중앙 기준으로 옮겨야 요소 안을 누른다.
    [Theory]
    [InlineData(300, 50)]
    [InlineData(120, 40)]
    [InlineData(10, 10)]
    [InlineData(1, 1)]
    public void Random_offset_stays_inside_the_element_from_its_center(int width, int height)
    {
        var random = new Random(1);
        for (int i = 0; i < 200; i++)
        {
            Point offset = Util.RandomPointerOffset(new Size(width, height), random);

            int fromLeft = offset.X + width / 2;
            int fromTop = offset.Y + height / 2;
            Assert.InRange(fromLeft, (int)(width * 0.4), Math.Max((int)(width * 0.4), (int)(width * 0.7) - 1));
            Assert.InRange(fromTop, (int)(height * 0.4), Math.Max((int)(height * 0.4), (int)(height * 0.7) - 1));
        }
    }
}
