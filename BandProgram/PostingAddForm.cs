using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BandProgram
{
    public partial class PostingAddForm : Form
    {
        public delegate void Del();
        public string path = "";

        public string sep = "";

        private Point mousePoint;

        private FunctionList fl = new FunctionList();

        private List<ImageFile> imageFileList = new List<ImageFile>();

        public PostingAddForm.Del handler;

        private int index = -1;
        private void refreshImageList()
        {
            this.ImageListView.Items.Clear();
            foreach (ImageFile imageFile in this.imageFileList)
            {
                ListViewItem listViewItem = new ListViewItem(imageFile.getFileName());
                listViewItem.SubItems.Add(string.Concat(imageFile.getFileSize(), "bytes"));
                this.ImageListView.Items.Add(listViewItem);
            }
        }

        public void applyData(int index)
        {
            this.index = index;
            string str = string.Concat(new string[] { this.path, "/", this.sep, index.ToString(), "/" });
            string str1 = Util.getInstance().readAllToString(string.Concat(str, "contents.txt"));
            this.postContent.Text = str1;
            this.imageFileList = Util.getInstance().getImageList(str);
            this.refreshImageList();
        }

        public PostingAddForm()
        {
            InitializeComponent();
        }

        private void postingAddBtn_Click(object sender, EventArgs e)
        {
            if (this.index != -1)
            {
                string str = string.Concat(new string[] { this.path, "/", this.sep, this.index.ToString(), "/" });
                if (this.fl.savePosting(str, this.postContent.Text, this.imageFileList))
                {
                    Util.getInstance().getImageList(str);
                    this.handler();
                    base.Close();
                    return;
                }
                this.fl.deleteDirectory(str);
                MessageBox.Show("포스팅 파일을 저장하지 못했습니다. 다시 시도해주시기 바랍니다.", "오류 메시지", MessageBoxButtons.OK);
                return;
            }
            int postingNum = this.fl.getPostingNum(this.path, this.sep);
            string str1 = string.Concat(new string[] { this.path, "/", this.sep, postingNum.ToString(), "/" });
            if (this.fl.savePosting(str1, this.postContent.Text, this.imageFileList))
            {
                Util.getInstance().getImageList(str1);
                this.handler();
                base.Close();
                return;
            }
            this.fl.deleteDirectory(str1);
            MessageBox.Show("포스팅 파일을 저장하지 못했습니다. 다시 시도해주시기 바랍니다.", "오류 메시지", MessageBoxButtons.OK);
        }

        private void openImageBtnClick(object sender, EventArgs e)
        {
            if (radioFolder.Checked)
            {
                FolderBrowserDialog fd = new FolderBrowserDialog();
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    string[] files = Directory.GetFiles(fd.SelectedPath);
                    foreach (string s in files)
                    {
                        string pa = Path.GetExtension(s).ToLower();
                        if (pa.Contains("jpg") || pa.Contains("jpeg") || pa.Contains("gif") || pa.Contains("bmp") || pa.Contains("png"))
                        {
                            string test = Path.GetFileName(s);
                            ImageFile imageFile = new ImageFile(test, s.Replace(Path.GetFileName(s), ""), (new FileInfo(s)).Length);
                            if (imageFile != null)
                            {
                                this.imageFileList.Add(imageFile);
                                this.refreshImageList();
                            }
                        }
                    }

                }
            }
            else
            {
                try
                {
                    ImageFile imageFile = ImageFileDialog.Show();
                    if (imageFile != null)
                    {
                        this.imageFileList.Add(imageFile);
                        this.refreshImageList();
                    }
                }
                catch
                {
                }
            }
        }
        private void MenuClick(object obj, EventArgs ea)
        {
            try
            {
                int index = LegacyMenu.IndexOf(obj);
                if (index == 0)
                {
                    this.imageFileList.Clear();
                    this.refreshImageList();
                }
                else if (index == 1)
                {
                    if (this.ImageListView.FocusedItem != null)
                    {
                        this.imageFileList.RemoveAt(this.ImageListView.FocusedItem.Index);
                        this.refreshImageList();
                    }
                }
            }
            catch
            {
            }
        }
        private void ImageListView_DoubleClick(object sender, EventArgs e)
        {
            if (!this.radioImageOpen.Checked)
            {
                ShellOpen.Open(Path.GetDirectoryName(this.imageFileList[this.ImageListView.FocusedItem.Index].getPath()));
            }
            else
            {
                string str = string.Concat(this.imageFileList[this.ImageListView.FocusedItem.Index].getPath(), this.imageFileList[this.ImageListView.FocusedItem.Index].getFileName());
                try
                {
                    ShellOpen.Open(str);
                }
                catch
                {
                    try
                    {
                        ShellOpen.Open(string.Concat(string.Concat(Application.StartupPath.Replace('\\', '/'), "/"), str));
                    }
                    catch
                    {
                        MessageBox.Show("파일 경로가 잘못되었습니다.");
                    }
                }
            }
        }
        private void ImageListView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                EventHandler eventHandler = new EventHandler(this.MenuClick);
                this.ImageListView.ContextMenuStrip = LegacyMenu.Create(eventHandler, "전체 사진 삭제", "선택한 사진 삭제");
            }
        }

        private void PostingAddForm_Load(object sender, EventArgs e)
        {
            if (this.sep.Contains("post"))
            {
                this.Text = "포스팅원고 추가";
                return;
            }
            if (this.sep.Contains("comment"))
            {
                this.Text = "댓글원고 추가";
                return;
            }
            if (this.sep.Contains("chatting"))
            {
                this.Text = "대화원고 추가";
            }
        }
    }
}
