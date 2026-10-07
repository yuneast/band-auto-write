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
    public partial class MainForm : Form
    {
        private Thread loginCheckTh;
        private string bandid { get; set; }
        private string bandpw { get; set; }
        private string bandtype { get; set; }
        private CookieContainer session { get; set; }
        private string userid { get; set; }
        private void printLog(string msg)
        {
           
                ListViewItem listViewItem = new ListViewItem(DateTime.Now.ToString("yy-MM-dd HH:mm:ss"));
                listViewItem.SubItems.Add(msg);
                this.listView2.Items.Add(listViewItem);
                Util.getInstance().writeStream("사용기록.txt", msg);
        
        }
        int errcount = 0;
        public void loginCheck()
        {
            while (true)
            {
                if(errcount>=5)
                {
                    Util.getInstance().closeChrome();
                    Application.ExitThread();
                    Environment.Exit(0);
                    this.printLog("서버통신오류로 종료되었습니다");
                }
                try
                {
                    HttpWebRequest req2 = (HttpWebRequest)WebRequest.Create("http://newsoft.kr/session.php?type=band");
                    req2.Method = "GET";
                    req2.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
                    req2.CookieContainer = session;
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
                        this.printLog("중복로그인발생. 프로그램종료");


                        Thread.Sleep(3000);
                        Util.getInstance().closeChrome();
                        Application.ExitThread();
                        Environment.Exit(0);
                    }

                    this.labelLeftLog.Text = string.Concat(new object[] { this.userid, "으로 로그인되었습니다" });
                    Thread.Sleep(1000 * 30);
                }
                catch(Exception e)
                {
                    errcount++;
                  
                    Thread.Sleep(1000 * 30);
                }
            }

        }
        public MainForm(string bandid,string bandpw,string bandtype,CookieContainer session,string id)
        {
            InitializeComponent();
            this.bandid = bandid;
            this.bandpw = bandpw;
            this.bandtype = bandtype;
            this.userid = id;
            this.session = session;
            this.loginCheckTh = new Thread(new ThreadStart(this.loginCheck));
            this.loginCheckTh.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            (new Thread(new ThreadStart(this.loadBandList))).Start();
        }
        private void removeSelectedList()
        {
            if (MessageBox.Show("해당 목록들을 삭제하시겠습니까?", "목록 삭제", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                int count = this.listView1.Items.Count;
                this.fl.getBandListInFile();
                List<int> nums = new List<int>();
                for (int i = 0; i < count; i++)
                {
                    if (this.listView1.Items[i].Checked)
                    {
                        nums.Add(i);
                    }
                }
                this.bandList = this.fl.removeBand(nums);
                this.refreshList();
            }
        }
      
        private void refreshList()
        {
            int num = 0;
            this.listView1.Items.Clear();
            foreach (BandInfo bandInfo in this.bandList)
            {
                num++;
                this.addBandListView(bandInfo, num);
            }
            Label label = this.label1;
            int num1 = this.bandList.Count<BandInfo>();
            label.Text = string.Concat("밴드리스트(", num1.ToString(), ")");
        }
        public void selectPosting(int idx, List<int> postingList, List<int> commentList, List<int> chattingList)
        {
            this.bandList.ElementAt<BandInfo>(idx).postingList = postingList;
            this.bandList.ElementAt<BandInfo>(idx).commentList = commentList;
            this.bandList.ElementAt<BandInfo>(idx).chattingList = chattingList;
            this.refreshBandListAndSaveFile();
        }

        private bool setBandMain()
        {
            bool flag;
            try
            {
                if (!this.radioButton4.Checked && this.radioButton3.Checked)
                {
                    int num = 0;
                    if (this.textBox11.Text == null || this.textBox11.Text.Equals(""))
                    {
                        this.printLog("키워드를 입력해주세요");
                        flag = false;
                        return flag;
                    }
                    else
                    {
                        try
                        {
                            num = int.Parse(this.textBox10.Text);
                        }
                        catch
                        {
                            this.printLog("검색 범위를 정확히 입력해주세요 (숫자만)");
                            flag = false;
                            return flag;
                        }
                        if (this.textBox9.Text == null || this.textBox9.Text.Equals(""))
                        {
                            this.printLog("밴드 내 이름을 입력해주세요");
                            flag = false;
                            return flag;
                        }
                        else if (!this.checkBox2.Checked)
                        {
                            this.fl.setBandListFromQuery(this.textBox11.Text, num, -1, -1);
                        }
                        else
                        {
                            try
                            {
                                int num1 = int.Parse(this.textBox8.Text);
                                int num2 = int.Parse(this.textBox7.Text);
                                this.fl.setBandListFromQuery(this.textBox11.Text, num, num1, num2);
                            }
                            catch
                            {
                                this.printLog("최소 최대 멤버수를 입력해주세요.");
                                flag = false;
                                return flag;
                            }
                        }
                    }
                }
                flag = true;
            }
            catch
            {
                flag = false;
            }
            return flag;
        }
        private FunctionList fl = new FunctionList();
        private List<BandInfo> bandList;

        private List<BandInfo> linkList = new List<BandInfo>();

        private List<BandInfo> linkListWithQuery = new List<BandInfo>();
        private void addBandListView(BandInfo band, int idx)
        {
            ListViewItem listViewItem = new ListViewItem(idx.ToString());
            listViewItem.SubItems.Add(band.name);
            listViewItem.SubItems.Add(band.num);
            string str = this.intListToString(band.postingList);
            string str1 = this.intListToString(band.commentList);
            string str2 = this.intListToString(band.chattingList);
            listViewItem.SubItems.Add(str);
            listViewItem.SubItems.Add(str1);
            listViewItem.SubItems.Add(str2);
            this.listView1.Items.Add(listViewItem);
        }
        private string intListToString(List<int> list)
        {
            string str = "";
            if (list != null)
            {
                foreach (int num in list)
                {
                    str = (!str.Equals("") ? string.Concat(str, ", ", num.ToString()) : string.Concat(str, num.ToString()));
                }
            }
            return str;
        }
        private void refreshBandListAndSaveFile()
        {
            int num = 0;
            this.listView1.Items.Clear();
            string str = "bandList.txt";
            if (this.bandList == null)
            {
                this.printLog("서버에서 데이터를 가져오는데에 실패하였습니다.");
                return;
            }
            Util.getInstance().createNotePad(str);
            foreach (BandInfo bandInfo in this.bandList)
            {
                num++;
                this.addBandListView(bandInfo, num);
                Util.getInstance().writeStream(str, string.Concat(new string[] { bandInfo.name, "\t", bandInfo.num, "\t", this.intListToString(bandInfo.postingList), "\t", this.intListToString(bandInfo.commentList), "\t", this.intListToString(bandInfo.chattingList) }));
            }
            Label label = this.label1;
            int num1 = this.bandList.Count<BandInfo>();
            label.Text = string.Concat("밴드리스트(", num1.ToString(), ")");
        }
        private void loadBandList()
        {
            this.printLog("불러오는 중");
            this.button1.Enabled = false;
            this.bandList = this.fl.getBandList(this.bandList);
            if (this.bandList == null)
            {
                this.printLog("목록 불러오기에 실패하였습니다..");
                this.button1.Enabled = true;
                return;
            }
            this.refreshBandListAndSaveFile();
            this.button1.Enabled = true;
            this.printLog("목록 불러오기 완료");
        }
        private void allCheck()
        {
            int count = this.listView1.Items.Count;
            for (int i = 0; i < count; i++)
            {
                this.listView1.Items[i].Checked = true;
            }
        }

        private void allDeCheck()
        {
            int count = this.listView1.Items.Count;
            for (int i = 0; i < count; i++)
            {
                this.listView1.Items[i].Checked = false;
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            this.allCheck();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.removeSelectedList();
        }


        private void button16_Click(object sender, EventArgs e)
        {
            Util.getInstance().closeChrome();
            base.Hide();
            LoginSecond sec = new LoginSecond(session, userid);
          
          sec.Show();
        }


        private BandLayout posting;

        private BandLayout comment;

        private BandLayout chatting;
        private void printLogLeft(string msg)
        {
             this.labelLeftLog.Text = msg;
        }
        public void isOperating(int type, bool isRunning, bool finished)
        {
            if (type == 0)
            {
                this.comment.anotherRunning(isRunning);
                this.chatting.anotherRunning(isRunning);
                this.button15.Enabled = !isRunning;
                this.button14.Enabled = false;
                if (!isRunning)
                {
                   this.labelLeftLog.Text = "포스팅 작업이 중지되었습니다.";
                    if (finished)
                    {
                        this.printLog("포스팅 작업 완료");
                    }
                }
                else
                {
                    this.labelLeftLog.Text = "포스팅 작업이 시작되었습니다.";
                }
            }
            else if (type == 1)
            {
                this.posting.anotherRunning(isRunning);
                this.chatting.anotherRunning(isRunning);
                this.button15.Enabled = !isRunning;
                this.button14.Enabled = false;
                if (!isRunning)
                { 
                    this.labelLeftLog.Text = "댓글 작업이 중지되었습니다.";
                    if (finished)
                    {
                        this.printLog("댓글 작업 완료");
                    }
                }
                else
                {
                    this.labelLeftLog.Text = "댓글 작업이 시작되었습니다.";
                }
            }
            else if (type == 2)
            {
                this.posting.anotherRunning(isRunning);
                this.comment.anotherRunning(isRunning);
                this.button15.Enabled = !isRunning;
                this.button14.Enabled = false;
                if (!isRunning)
                {
                    this.labelLeftLog.Text = "채팅 작업이 중지되었습니다.";
                }
                else
                {
                    this.labelLeftLog.Text = "채팅 작업이 시작되었습니다.";
                }
                if (finished)
                {
                    this.printLog("채팅 작업 완료");
                }
            }
            else if (type == 3)
            {
                this.posting.anotherRunning(isRunning);
                this.comment.anotherRunning(isRunning);
                this.chatting.anotherRunning(isRunning);
                if (!isRunning)
                {
                    this.labelLeftLog.Text = "가입 작업이 중지되었습니다.";
                }
                else
                {
                    this.labelLeftLog.Text = "가입 작업이 시작되었습니다.";
                }
                this.button10.Enabled = !finished;
            }
            if (!isRunning)
            {
                this.printLog("작업 중지");
            }
            if (finished)
            {
                this.labelLeftLog.Text = "작업 대기중입니다.";
            }
        }
        public void refreshFromFile()
        {
            this.bandList = this.fl.getBandListInFile();
            int num = 0;
            this.listView1.Items.Clear();
            foreach (BandInfo bandInfo in this.bandList)
            {
                num++;
                this.addBandListView(bandInfo, num);
            }
            Label label = this.label1;
            int num1 = this.bandList.Count<BandInfo>();
            label.Text = string.Concat("밴드리스트(", num1.ToString(), ")");
        }
        private void prepareListView()
        {
            List<ComboBox> comboBoxes = new List<ComboBox>()
            {
                this.comboBox18,
                this.comboBox17,
                this.comboBox16,
                this.comboBox15,
                this.comboBox14,
                this.comboBox6
            };
            this.posting = new BandLayout(0, this.userid, this.label2, this.listView3, comboBoxes, this.checkBox4, this.checkBox5, this.button8, this.button9, this.button5, new BandLayout.Del_PrintLog(this.printLog), new BandLayout.Del_PrintLogLeft(this.printLogLeft), new BandLayout.Del_Refresh(this.refreshFromFile), new BandLayout.Del_IsOperating(this.isOperating), "AutoDoc/Posting", "post_");
            List<ComboBox> comboBoxes1 = new List<ComboBox>()
            {
                this.comboBox5,
                this.comboBox4,
                this.comboBox3,
                this.comboBox2,
                this.comboBox1,
                this.comboBox7
            };
            this.comment = new BandLayout(1, this.userid, this.label11, this.listView4, comboBoxes1, this.checkBox3, this.checkBox6, this.button13, this.button12, this.button6, new BandLayout.Del_PrintLog(this.printLog), new BandLayout.Del_PrintLogLeft(this.printLogLeft), new BandLayout.Del_Refresh(this.refreshFromFile), new BandLayout.Del_IsOperating(this.isOperating), "AutoDoc/Comment", "comment_");
            List<ComboBox> comboBoxes2 = new List<ComboBox>()
            {
                this.comboBox13,
                this.comboBox12,
                this.comboBox11,
                this.comboBox10,
                this.comboBox9,
                this.comboBox8
            };
            this.chatting = new BandLayout(2, this.userid, this.label28, this.listView6, comboBoxes2, this.checkBox1, this.checkBox7, this.button20, this.button18, this.button7, new BandLayout.Del_PrintLog(this.printLog), new BandLayout.Del_PrintLogLeft(this.printLogLeft), new BandLayout.Del_Refresh(this.refreshFromFile), new BandLayout.Del_IsOperating(this.isOperating), "AutoDoc/Chatting", "chatting_");
        }
        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.printLog("프로그램 실행");
            base.CenterToParent();
            this.prepareListView();
            this.bandList = this.fl.getBandListInFile();
            this.refreshList();
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            int index = this.listView1.FocusedItem.Index;
            string text = this.listView1.FocusedItem.SubItems[1].Text;
            string str = this.listView1.FocusedItem.SubItems[2].Text;
            string text1 = this.listView1.FocusedItem.SubItems[3].Text;
            string str1 = this.listView1.FocusedItem.SubItems[4].Text;
            string text2 = this.listView1.FocusedItem.SubItems[5].Text;
            SelectPostingForm selectPostingForm = new SelectPostingForm();
            selectPostingForm.applyData(index, text, str, text1, str1, text2, new SelectPostingForm.Del(this.selectPosting));
            selectPostingForm.ShowDialog();
        }
   
        private void MenuClick(object obj, EventArgs ea)
        {
            try
            {
                switch (LegacyMenu.IndexOf(obj))
                {
                    case 0:
                        {
                            this.removeSelectedList();
                            goto case 7;
                        }
                    case 1:
                    case 4:
                    case 7:
                        {
                            break;
                        }
                    case 2:
                        {
                            this.refreshBandListAndSaveFile();
                            goto case 7;
                        }
                    case 3:
                        {
                            this.bandList = this.fl.getBandListInFile();
                            this.refreshList();
                            goto case 7;
                        }
                    case 5:
                        {
                            SelectPostingForm selectPostingForm = new SelectPostingForm();
                            List<int> nums = new List<int>();
                            foreach (ListViewItem item in this.listView1.Items)
                            {
                                if (!item.Checked)
                                {
                                    continue;
                                }
                                nums.Add(item.Index);
                            }
                            selectPostingForm.applyIndexList(nums, new SelectPostingForm.Del(this.selectPosting));
                            selectPostingForm.ShowDialog();
                            goto case 7;
                        }
                    case 6:
                        {
                            (new Thread(new ThreadStart(this.loadBandList))).Start();
                            goto case 7;
                        }
                    case 8:
                        {
                            this.allCheck();
                            goto case 7;
                        }
                    case 9:
                        {
                            this.allDeCheck();
                            goto case 7;
                        }
                    default:
                        {
                            goto case 7;
                        }
                }
            }
            catch
            {
            }
        }
        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                EventHandler eventHandler = new EventHandler(this.MenuClick);
                this.listView1.ContextMenuStrip = LegacyMenu.Create(eventHandler, "선택된 항목 삭제하기", "-", "현재 리스트를 파일로 저장하기", "파일로 저장된 리스트 불러오기", "-", "선택된 항목 포스트 번호 일괄 수정하기", "서버에서 가입된 밴드 리스트 불러오기", "-", "모든 항목 선택하기", "모든 항목 선택 해제하기");
            }
        }
        private void signupBand()
        {
            if (this.setBandMain())
            {
                this.signupBandMain();
            }
            this.toggleToBandSign(true, true);
        }
        private void signupBandMain()
        {
            try
            {
                Util.getInstance().acceptAlert();
                Util.getInstance().closeCurrent();
                if (!Util.getInstance().isOpenChrome())
                {
                    this.fl.login();
                }
                if (!this.radioButton4.Checked && this.radioButton3.Checked)
                {
                    if (!this.checkBox2.Checked)
                    {
                        this.linkList = this.fl.getBandListFromQuery();
                    }
                    else
                    {
                        try
                        {
                            this.linkList = this.fl.getBandListFromQueryWithMemCnt();
                        }
                        catch
                        {
                            this.printLog("최소 최대 멤버수를 입력해주세요.");
                            return;
                        }
                    }
                    this.listView5.Items.Clear();
                    int num = 0;
                    foreach (BandInfo bandInfo in this.linkList)
                    {
                        num++;
                        ListViewItem listViewItem = new ListViewItem(num.ToString());
                        listViewItem.SubItems.Add(bandInfo.name);
                        listViewItem.SubItems.Add(bandInfo.num);
                        this.listView5.Items.Add(listViewItem);
                    }
                }
                if (this.linkList != null)
                {
                    Util instance = Util.getInstance();
                    foreach (BandInfo bandInfo1 in this.linkList)
                    {
                        string str = this.fl.signupBand(bandInfo1, this.textBox9.Text);
                        if (!str.Contains("1일"))
                        {
                            if (!str.Contains("성공"))
                            {
                                this.printLog(str);
                            }
                            else
                            {
                                this.printLog(string.Concat("http://band.us/band/", bandInfo1.num, " - 밴드 가입 성공"));
                            }
                            instance.acceptAlert();
                        }
                        else
                        {
                            this.printLog("1일 가입 갯수를 초과하였습니다.");
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            this.listView5.Items.Clear();
        }
        private void button15_Click(object sender, EventArgs e)
        {
            if (this.thSignup == null || this.thSignup.ThreadState != System.Threading.ThreadState.Suspended)
            {
                this.printLog(string.Concat(this.userid, " -> 가입 작업 시작"));
                this.thSignup = new Thread(new ThreadStart(this.signupBand));
                this.thSignup.Start();
            }
            else
            {
                this.printLog("가입 작업 재시작");
                if (!this.setBandMain())
                {
                    return;
                }
                this.thSignup.Resume();
            }
            this.toggleToBandSign(false, false);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            try
            {
                this.thSignup.Suspend();
                this.toggleToBandSign(true, false);
            }
            catch
            {
            }
        }
        private void toggleToBandSign(bool enable, bool finished)
        {
            this.radioButton4.Enabled = enable;
            this.radioButton3.Enabled = enable;
            this.textBox12.Enabled = enable;
            this.textBox11.Enabled = enable;
            this.textBox10.Enabled = enable;
            this.textBox9.Enabled = enable;
            this.textBox8.Enabled = enable;
            this.textBox7.Enabled = enable;
            this.button19.Enabled = enable;
            this.button15.Enabled = enable;
            this.button14.Enabled = !enable;
            this.checkBox2.Enabled = enable;
            this.isOperating(3, !enable, finished);
        }
        private Thread thSignup;
        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                this.thSignup.Abort();
            }
            catch
            {
            }
            this.toggleToBandSign(true, true);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                this.fl.closeChrome();
                Application.Exit();
                Environment.Exit(0);
                Process.GetCurrentProcess().Kill();
            }
            catch
            {
            }
        }
        private void addLinkList()
        {
            BandInfo bandInfoFromUrl = this.fl.getBandInfoFromUrl(this.textBox12.Text);
            if (bandInfoFromUrl.num.Equals(""))
            {
                this.printLog("잘못된 링크입니다.");
                return;
            }
            if (bandInfoFromUrl.name.Equals("비공개 밴드"))
            {
                this.printLog("비공개 밴드입니다.");
                return;
            }
            foreach (BandInfo bandInfo in this.linkList)
            {
                if (!bandInfo.num.Equals(bandInfoFromUrl.num))
                {
                    continue;
                }
                return;
            }
            this.linkList.Add(bandInfoFromUrl);
            int count = this.linkList.Count;
            ListViewItem listViewItem = new ListViewItem(count.ToString());
            listViewItem.SubItems.Add(bandInfoFromUrl.name);
            listViewItem.SubItems.Add(bandInfoFromUrl.num);
            this.listView5.Items.Add(listViewItem);
            Label label = this.label31;
            count = this.listView5.Items.Count;
            label.Text = string.Concat("가입할 밴드 리스트(", count.ToString(), ")");
        }
        private void button19_Click(object sender, EventArgs e)
        {
            try
            {
                this.addLinkList();
            }
            catch
            {
                this.printLog("잘못된 링크입니다.");
            }
        }

        private void listView5_MouseDown(object sender, MouseEventArgs e)
        {
     
        }

        private void 지정된아이템삭제ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                listView5.SelectedItems[0].Remove();
            } catch
            {

            }
        }

        
    }
}
