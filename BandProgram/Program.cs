using System;
using System.Collections.Generic;
using System.Drawing;
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
            // .NET Framework와 같은 기본 글꼴·DPI 동작 (.NET Core 3.0부터 기본값이 바뀜)
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
            Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25f));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Login());
         //   Application.Run(new NewPostForm());
        }
    }
}
