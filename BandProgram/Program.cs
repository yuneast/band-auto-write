using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BandProgram
{
    static class Program
    {
        public static string Session { get; set; }
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // .NET Framework와 같은 기본 글꼴·DPI 동작 (.NET Core 3.0부터 기본값이 Segoe UI 9pt로 바뀜).
            // .NET Framework의 기본 글꼴은 시스템 기본 글꼴(한국어 Windows는 굴림 9pt)이었고,
            // 폼들도 그 크기(AutoScaleDimensions 7x12)로 디자인되어 있다. 다른 글꼴을 쓰면 폼이 축소·확대되어
            // 글꼴이 따로 지정된 탭 머리글과 버튼이 겹친다.
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
            Application.SetDefaultFont(SystemFonts.DefaultFont);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // 상대 경로 파일(bandList.txt, AutoDoc/...)이 마이그레이션이 변환한 폴더와 같은 곳을 가리키게 한다.
            Directory.SetCurrentDirectory(AppPaths.DataDir);
            Util.Clipboard = new WinFormsClipboard();
            SelectorTrace.Sink = line =>
            {
                try
                {
                    File.AppendAllText(Path.Combine(AppPaths.DataDir, "selector-miss.log"),
                        string.Concat(DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), " ", line, Environment.NewLine));
                }
                catch
                {
                }
            };

            // .NET Framework 버전이 CP949로 저장한 파일을 UTF-8로 한 번 변환한다.
            MigrationResult migration = Utf8Migration.Run(AppPaths.DataDir);
            try
            {
                if (migration.Converted.Count > 0 || migration.Failed.Count > 0)
                {
                    string log = Path.Combine(AppPaths.DataDir, "encoding-migration.log");
                    string stamp = DateTime.Now.ToString("yy-MM-dd HH:mm:ss");
                    foreach (string file in migration.Converted)
                    {
                        File.AppendAllText(log, string.Concat(stamp, " 변환: ", file, Environment.NewLine));
                    }
                    foreach (string failure in migration.Failed)
                    {
                        File.AppendAllText(log, string.Concat(stamp, " 실패: ", failure, Environment.NewLine));
                    }
                }
            }
            catch
            {
                // 로그를 못 써도 프로그램은 시작해야 한다.
            }
            if (migration.Failed.Count > 0)
            {
                MessageBox.Show("일부 텍스트 파일을 UTF-8로 바꾸지 못했습니다. encoding-migration.log를 확인해 주세요.");
            }
            Application.Run(new Login());
         //   Application.Run(new NewPostForm());
        }
    }
}
