using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BandProgram
{
    public partial class LoginSecond : Form
    {
      private string LoginID { get; set; }
     private CookieContainer cookie { get; set; }

        private string fileName = "bandAccount.txt";
        private Point mousePoint;

        private MainForm form1;

        private FunctionList fl = new FunctionList();

        private Thread login_th;
        public LoginSecond(CookieContainer cookie,string lid)
        {
            this.cookie = cookie;
            LoginID = lid;
            InitializeComponent();
            List<string> strs = Util.getInstance().readAll(this.fileName);
            try
            {
                foreach (string str in strs)
                {
                    string[] strArrays = str.Split(new char[] { '\t' });
                    string str1 = strArrays[0];
                    string str2 = strArrays[1];
                    string str3 = strArrays[2];
                    ListViewItem listViewItem = new ListViewItem()
                    {
                        Text = str1
                    };
                    listViewItem.SubItems.Add(str2);
                    listViewItem.SubItems.Add(str3);
                    this.listView1.Items.Add(listViewItem);
                }
            }
            catch
            {
            }
        }

        private void LoginSecond_FormClosing(object sender, FormClosingEventArgs e)
        {
           // Process.GetCurrentProcess().Kill();
        }
        public void loginCheck()
        {

            try
            {
                HttpWebRequest req2 = (HttpWebRequest)WebRequest.Create("http://newsoft.kr/session.php?type=band");
                req2.Method = "GET";
                req2.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
                req2.CookieContainer = cookie;
                req2.AllowAutoRedirect = true;
                string read2 = "";
                using (var response = req2.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        read2 = sr.ReadToEnd();
                    }
                }
                if (read2 == "")
                {
                    Thread.Sleep(3000);
                    Util.getInstance().closeChrome();
                    Application.ExitThread();
                    Environment.Exit(0);
                }
            }
            catch(Exception e)
            {

            }

            //Thread.Sleep(1000 * 10);


        }
        private void LoginSecond_Load(object sender, EventArgs e)
        {
            loginCheck();
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string text = this.textBox1.Text;
            string str = Util.getInstance().Base64Encoding(this.textBox2.Text, null);
            string text1 = this.comboBox1.Text;
            ListViewItem listViewItem = new ListViewItem()
            {
                Text = text
            };
            listViewItem.SubItems.Add(str);
            listViewItem.SubItems.Add(text1);
            this.textBox1.Clear();
            this.textBox2.Clear();
            this.listView1.Items.Add(listViewItem);
            string str1 = string.Concat(new string[] { text, "\t", str, "\t", text1 });
            Util.getInstance().writeStream(this.fileName, str1);
        }
        private void MenuClick(object obj, EventArgs ea)
        {
            try
            {
                int index = LegacyMenu.IndexOf(obj);
                if (index == 0)
                {
                    this.removeItem();
                }
                else if (index == 1)
                {
                    this.removeAll();
                }
            }
            catch
            {
            }
        }

        private void removeAll()
        {
            Util.getInstance().createNotePad(this.fileName);
            this.listView1.Items.Clear();
        }

        private void removeItem()
        {
            Util.getInstance().removeALine(this.fileName, this.listView1.SelectedIndices[0]);
            this.listView1.Items.RemoveAt(this.listView1.SelectedIndices[0]);
        }
        private void bandLogin()
        {
            string text = this.listView1.Items[this.listView1.SelectedIndices[0]].SubItems[0].Text;
            string str = Util.getInstance().Base64Decoding(this.listView1.Items[this.listView1.SelectedIndices[0]].SubItems[1].Text, null);
            string text1 = this.listView1.Items[this.listView1.SelectedIndices[0]].SubItems[2].Text;
            Util.getInstance().setBandAccount(text, str, text1);
            if (!this.fl.login())
            {
                Util.getInstance().closeChrome();
                MessageBox.Show("로그인에 실패하였습니다.");
            }
            else
            {
                base.Visible = false;
                if (this.form1 == null)
                {
                    this.form1 = new MainForm(text, str, text1,cookie,LoginID);
                }
         
                this.form1.ShowDialog();
                base.Hide();
            }
            this.button1.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedIndices.Count > 0)
            {
                this.button1.Enabled = false;
                this.bandLogin();
            }
        }

        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                EventHandler eventHandler = new EventHandler(this.MenuClick);
                this.listView1.ContextMenuStrip = LegacyMenu.Create(eventHandler, "선택된 항목 삭제하기", "전체 삭제하기");
            }
        }

        private void LoginSecond_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.fl.closeChrome();
            Application.Exit();
            Environment.Exit(0);
            Process.GetCurrentProcess().Kill();
        }
    }
}
