using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BandProgram
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            if(File.Exists("acc.txt"))
            {
                string acc = File.ReadAllText("acc.txt");
                textBox1.Text = acc.Split('|')[0];
                textBox2.Text = acc.Split('|')[1];
                    
            }
        }
       

        private void button1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text.Length==0 || textBox2.Text.Length==0)
            {
                return;
            }

            string url_str = "https://newsoft.kr/login.php?action=login";
            if (Global.Instance.isLocalLogin)
            {
                url_str = "http://127.0.0.1:3000/login";
            }

            string reqstr = "id=" + textBox1.Text + "&pass=" + textBox2.Text + "&type=band";
            byte[] br = Encoding.UTF8.GetBytes(reqstr);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url_str);
            CookieContainer cookie = new CookieContainer();
            req.Method = "POST";
            req.CookieContainer = cookie;
            req.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            req.ContentLength = br.Length;
            req.AllowAutoRedirect = true;
            req.ServerCertificateValidationCallback = ServerTrust.Validate;
            req.GetRequestStream().Write(br, 0, br.Length);
            string read = "";
            using (var response = req.GetResponse())
            {
                //req.CookieContainer.Add(((HttpWebResponse)response).Cookies);
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    read = sr.ReadToEnd();
                }
            }
            if(read.Contains("기간만료"))
            {
                MessageBox.Show("기간이만료되었습니다");
                return;
            }
            else if (read.Contains("성공"))
            {

                
                    LoginSecond sec = new LoginSecond(cookie, textBox1.Text);
                    sec.Show();
                    this.Hide();
                    File.WriteAllText("acc.txt", textBox1.Text + "|" + textBox2.Text);

            }
            else
            {
                MessageBox.Show("로그인실패");
                return;
            }
        
        }
    }
}
