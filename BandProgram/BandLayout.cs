using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace BandProgram
{
	internal class BandLayout
	{
		private Thread thPosting;

		private readonly WorkControl work = new WorkControl();

		private bool isPaused;

		private bool isReset;

		private FunctionList fl = new FunctionList();

		private string path = "";

		private string sep = "";

		private string entirePath = "";

		private string id;

		private int type;

		private Label cntLabel;

		private ListView postingListView;

		private List<ComboBox> comboList = new List<ComboBox>();

		private Button btnStart;

		private Button btnPause;

		private Button btnInit;

		private CheckBox checkBoxReserve;

		private CheckBox pasteCheckBox;

		private BandLayout.Del_PrintLog printLog;

		private BandLayout.Del_PrintLogLeft printLogLeft;

		private BandLayout.Del_Refresh refresh;

		private BandLayout.Del_IsOperating isOperating;

		public BandLayout(int type, string id, Label cntLabel, ListView postingListView, List<ComboBox> comboList, CheckBox checkBoxReserve, CheckBox pasteCheckBox, Button btnStart, Button btnPause, Button btnInit, BandLayout.Del_PrintLog printLog, BandLayout.Del_PrintLogLeft printLogLeft, BandLayout.Del_Refresh refresh, BandLayout.Del_IsOperating isOperating, string path, string sep)
		{
			this.type = type; //0: post, 1: comment, 2:chatting
			this.id = id;
			this.cntLabel = cntLabel;
			this.postingListView = postingListView;
			postingListView.DoubleClick += new EventHandler(this.postingList_DoubleClick);
			postingListView.MouseDown += new MouseEventHandler(this.listView_MouseDown);
			this.comboList = comboList;
			this.checkBoxReserve = checkBoxReserve;
			this.pasteCheckBox = pasteCheckBox;
			this.btnStart = btnStart;
			btnStart.Click += new EventHandler(this.btnStart_Click);
			this.btnPause = btnPause;
			btnPause.Click += new EventHandler(this.btnPause_Click);
			this.btnInit = btnInit;
			btnInit.Click += new EventHandler(this.btnInit_Click);
			this.printLog = printLog;
			this.printLogLeft = printLogLeft;
			this.refresh = refresh;
			this.isOperating = isOperating;
			this.path = path;
			this.sep = sep;
			this.entirePath = string.Concat(new string[] { AppPaths.DataDirWithSlash, path, "/", sep });
			this.prepareComboBox();
			this.loadPostingList();
		}

		private void addPosting()
		{

            //포스팅폼 생기는 부분 //Julian
			//(new PostingAddForm()
			//{
			//	path = this.path,
			//	sep = this.sep,
			//	handler = new PostingAddForm.Del(this.loadPostingList)
			//}).ShowDialog();


            if (this.sep.Contains("post"))
            {
                (new NewPostForm()
                {
                    path = this.path,
                    sep = this.sep,
                    new_handler = new NewPostForm.Del(this.loadPostingList)
                }).ShowDialog();
            }
            else if (this.sep.Contains("comment"))
            {
                (new PostingAddForm()
                {
                    path = this.path,
                    sep = this.sep,
                    handler = new PostingAddForm.Del(this.loadPostingList)
                }).ShowDialog();
            }
            else if (this.sep.Contains("chatting"))
            {
                (new PostingAddForm()
                {
                    path = this.path,
                    sep = this.sep,
                    handler = new PostingAddForm.Del(this.loadPostingList)
                }).ShowDialog();
            }


        }

		public void anotherRunning(bool isRunning)
		{
			this.btnStart.Enabled = !isRunning;
			this.btnPause.Enabled = false;
			if (this.thPosting != null && !this.isReset)
			{
				this.btnInit.Enabled = true;
				return;
			}
			this.btnInit.Enabled = false;
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			this.addPosting();
		}

		private void btnInit_Click(object sender, EventArgs e)
		{
			this.work.Reset();
			this.isReset = true;
			this.isPaused = false;
			this.toggleState(true, true);
		}

		private void btnPause_Click(object sender, EventArgs e)
		{
			if (this.thPosting == null)
			{
				return;
			}
			this.work.Pause();
			this.isPaused = true;
			this.toggleState(true, false);
		}

		private void btnRemove_Click(object sender, EventArgs e)
		{
			this.removePosting();
		}

		private void btnStart_Click(object sender, EventArgs e)
		{
            //Julian 포스팅작업시작
			if (this.thPosting == null || !this.isPaused)
			{
				if (this.type == 0)
				{
					this.printLog(string.Concat(this.id, " -> 포스팅 작업 시작"));
				}
				else if (this.type == 1)
				{
					this.printLog(string.Concat(this.id, " -> 댓글 작업 시작"));
				}
				else if (this.type == 2)
				{
					this.printLog(string.Concat(this.id, " -> 대화 작업 시작"));
				}
				this.isReset = false;
				this.thPosting = this.work.Start(this.startWork);
			}
			else
			{
				if (this.type == 0)
				{
					this.printLog(string.Concat(this.id, " -> 포스팅 작업 재시작"));
				}
				else if (this.type == 1)
				{
					this.printLog(string.Concat(this.id, " -> 댓글 작업 재시작"));
				}
				else if (this.type == 2)
				{
					this.printLog(string.Concat(this.id, " -> 대화 작업 재시작"));
				}
				this.resume();
				this.isPaused = false;
				this.work.Resume();
			}
			this.toggleState(false, false);
		}

		private void listView_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				EventHandler eventHandler = new EventHandler(this.MenuClick);
				this.postingListView.ContextMenuStrip = LegacyMenu.Create(eventHandler, "항목 추가하기", "선택된 항목 삭제하기");
			}
		}

		public void loadPostingList()
		{
			this.postingListView.Items.Clear();
			List<Post> postingList = this.fl.getPostingList(this.path, this.sep);
			if (postingList != null)
			{
				foreach (Post post in postingList)
				{
					ListViewItem listViewItem = new ListViewItem(post.idx.ToString());
					listViewItem.SubItems.Add(post.contents);
					ListViewItem.ListViewSubItemCollection subItems = listViewItem.SubItems;
					int num = post.images.Count<ImageFile>();
					subItems.Add(string.Concat(num.ToString(), "개"));
                    if(this.sep == "post_")
                    {
                        if (post.has_comment)
                        {
                            subItems.Add("o");
                        }
                        else
                        {
                            subItems.Add("x");
                        }
                    }


					this.postingListView.Items.Add(listViewItem);
				}
				if (this.type == 0)
				{
					this.cntLabel.Text = string.Concat("포스트 리스트(", postingList.Count<Post>(), ")");
				}
				else if (this.type == 1)
				{
					this.cntLabel.Text = string.Concat("코멘트 리스트(", postingList.Count<Post>(), ")");
				}
				else if (this.type == 2)
				{
					this.cntLabel.Text = string.Concat("대화 리스트(", postingList.Count<Post>(), ")");
				}
				this.sortListView(this.postingListView, false);
			}
		}

		private void MenuClick(object obj, EventArgs ea)
		{
			try
			{
				int index = LegacyMenu.IndexOf(obj);
				if (index == 0)
				{
					this.addPosting();
				}
				else if (index == 1)
				{
					this.removePosting();
				}
			}
			catch
			{
			}
		}

		private void postingList_DoubleClick(object sender, EventArgs e)
		{


            if (this.sep.Contains("post"))
            {

                NewPostForm postingAddForm = new NewPostForm()
                {
                    path = this.path,
                    sep = this.sep,
                    new_handler = new NewPostForm.Del(this.loadPostingList)
                };
                postingAddForm.applyData(int.Parse(this.postingListView.FocusedItem.SubItems[0].Text));
                postingAddForm.ShowDialog();
            }
            else if (this.sep.Contains("comment"))
            {
                


                PostingAddForm postingAddForm = new PostingAddForm()
                {
                    path = this.path,
                    sep = this.sep,
                    handler = new PostingAddForm.Del(this.loadPostingList)
                };
                postingAddForm.applyData(int.Parse(this.postingListView.FocusedItem.SubItems[0].Text));
                postingAddForm.ShowDialog();
            }




            






		}

		public void prepareComboBox()
		{
			if (this.type == 0)
			{
				this.comboList.ElementAt<ComboBox>(0).Items.Clear();
				this.comboList.ElementAt<ComboBox>(0).Items.Add("모든 밴드에 직접 지정한 포스트 작성하기");
				this.comboList.ElementAt<ComboBox>(0).Items.Add("모든 밴드에 랜덤 포스트 작성하기");
				this.comboList.ElementAt<ComboBox>(0).Items.Add("모든 밴드에 모든 포스트 작성하기");
				this.comboList.ElementAt<ComboBox>(0).SelectedIndex = 0;
			}
			else if (this.type == 1)
			{
				this.comboList.ElementAt<ComboBox>(0).Items.Clear();
				this.comboList.ElementAt<ComboBox>(0).Items.Add("모든 밴드에 직접 지정한 코멘트 작성하기");
				this.comboList.ElementAt<ComboBox>(0).Items.Add("모든 밴드에 랜덤 코멘트 작성하기");
				this.comboList.ElementAt<ComboBox>(0).Items.Add("모든 밴드에 모든 코멘트 작성하기");
				this.comboList.ElementAt<ComboBox>(0).SelectedIndex = 0;
			}
			else if (this.type == 2)
			{
				this.comboList.ElementAt<ComboBox>(0).Items.Clear();
				this.comboList.ElementAt<ComboBox>(0).Items.Add("모든 밴드에 직접 지정한 대화 작성하기");
				this.comboList.ElementAt<ComboBox>(0).Items.Add("모든 밴드에 랜덤 대화 작성하기");
				this.comboList.ElementAt<ComboBox>(0).Items.Add("모든 밴드에 모든 대화 작성하기");
				this.comboList.ElementAt<ComboBox>(0).SelectedIndex = 0;
			}
			this.comboList.ElementAt<ComboBox>(1).Items.Clear();
			this.comboList.ElementAt<ComboBox>(1).Items.Add("5초");
			this.comboList.ElementAt<ComboBox>(1).Items.Add("10초");
			this.comboList.ElementAt<ComboBox>(1).Items.Add("20초");
			this.comboList.ElementAt<ComboBox>(1).Items.Add("30초");
			this.comboList.ElementAt<ComboBox>(1).Items.Add("1분");
			this.comboList.ElementAt<ComboBox>(1).Items.Add("2분");
			this.comboList.ElementAt<ComboBox>(1).Items.Add("5분");
			this.comboList.ElementAt<ComboBox>(1).Items.Add("10분");
			this.comboList.ElementAt<ComboBox>(1).Items.Add("30분");
			this.comboList.ElementAt<ComboBox>(1).SelectedIndex = 0;
			this.comboList.ElementAt<ComboBox>(2).Items.Clear();
			for (int i = 0; i < 24; i++)
			{
				if (i >= 10)
				{
					this.comboList.ElementAt<ComboBox>(2).Items.Add(i.ToString());
				}
				else
				{
					this.comboList.ElementAt<ComboBox>(2).Items.Add(string.Concat("0", i.ToString()));
				}
			}
			this.comboList.ElementAt<ComboBox>(2).SelectedIndex = 0;
			this.comboList.ElementAt<ComboBox>(3).Items.Clear();
			for (int j = 0; j < 59; j++)
			{
				if (j >= 10)
				{
					this.comboList.ElementAt<ComboBox>(3).Items.Add(j.ToString());
				}
				else
				{
					this.comboList.ElementAt<ComboBox>(3).Items.Add(string.Concat("0", j.ToString()));
				}
			}
			this.comboList.ElementAt<ComboBox>(3).SelectedIndex = 0;
			this.comboList.ElementAt<ComboBox>(4).Items.Clear();
			this.comboList.ElementAt<ComboBox>(4).Items.Add("무한");
			for (int k = 1; k < 100; k++)
			{
				this.comboList.ElementAt<ComboBox>(4).Items.Add(k.ToString());
			}
			this.comboList.ElementAt<ComboBox>(4).SelectedIndex = 0;
			this.comboList.ElementAt<ComboBox>(5).Items.Clear();
			this.comboList.ElementAt<ComboBox>(5).Items.Add("5초");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("10초");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("20초");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("30초");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("1분");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("2분");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("5분");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("10분");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("20분");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("30분");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("1시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("2시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("3시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("4시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("5시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("6시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("7시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("8시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("9시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("10시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("11시간");
			this.comboList.ElementAt<ComboBox>(5).Items.Add("12시간");
			this.comboList.ElementAt<ComboBox>(5).SelectedIndex = 0;
		}

		public void printLogLeftMsg(string msg)
		{
			this.printLogLeft(msg);
		}

		public void printLogMsg(string msg)
		{
			this.printLog(msg);
		}

		private void removePosting()
		{
			this.printLog("btnRemove_Click");
			foreach (object checkedItem in this.postingListView.CheckedItems)
			{
				string text = ((ListViewItem)checkedItem).SubItems[0].Text;
				Util.getInstance().deleteDir(string.Concat(this.entirePath, text));
				this.printLog(string.Concat(this.entirePath, text));
			}
			this.loadPostingList();
		}

		private void resume()
		{
			this.setOption();
		}

		private void setOption()
		{
			int selectedIndex = this.comboList.ElementAt<ComboBox>(0).SelectedIndex;
			int num = 0;
			num = Util.getInstance().calculateTime(this.comboList.ElementAt<ComboBox>(1).Text);
			int num1 = 0;
			num1 = int.Parse(this.comboList.ElementAt<ComboBox>(2).Text);
			int num2 = 0;
			num2 = int.Parse(this.comboList.ElementAt<ComboBox>(3).Text);
			int num3 = 0;
			num3 = (!this.comboList.ElementAt<ComboBox>(4).Text.Contains("무한") ? int.Parse(this.comboList.ElementAt<ComboBox>(4).Text) : 0);
			int num4 = 0;
			num4 = Util.getInstance().calculateTime(this.comboList.ElementAt<ComboBox>(5).Text);
			if (this.type == 0)
			{
				this.fl.setPostingParam(selectedIndex, num, this.checkBoxReserve.Checked, this.pasteCheckBox.Checked, num1, num2, num3, num4);
				return;
			}
			if (this.type == 1)
			{
				this.fl.setCommentParam(selectedIndex, num, this.checkBoxReserve.Checked, this.pasteCheckBox.Checked, num1, num2, num3, num4);
				return;
			}
			if (this.type == 2)
			{
				this.fl.setChattingParam(selectedIndex, num, this.checkBoxReserve.Checked, this.pasteCheckBox.Checked, num1, num2, num3, num4);
			}
		}

		public void sortListView(ListView listview, bool isDesk = false)
		{
			if (!isDesk)
			{
				listview.ListViewItemSorter = new IntCompare(0, "asc");
				listview.Sorting = SortOrder.Ascending;
			}
			else
			{
				listview.ListViewItemSorter = new IntCompare(0, "desc");
				listview.Sorting = SortOrder.Descending;
			}
			listview.Sort();
		}

		private void startWork()
		{
			this.setOption();
			if (this.type == 0)
			{
				this.fl.startPosting(new FunctionList.Del_PrintLog(this.printLogMsg), new FunctionList.Del_PrintLogLeft(this.printLogLeftMsg));
			}
			else if (this.type == 1)
			{
				this.fl.startComment(new FunctionList.Del_PrintLog(this.printLogMsg), new FunctionList.Del_PrintLogLeft(this.printLogLeftMsg));
			}
			else if (this.type == 2)
			{
				this.fl.startChatting(new FunctionList.Del_PrintLog(this.printLogMsg), new FunctionList.Del_PrintLogLeft(this.printLogLeftMsg));
			}
			this.refresh();
			this.toggleState(true, true);
		}

		private void toggleState(bool enable, bool finished)
		{
			this.btnStart.Enabled = enable;
			this.btnPause.Enabled = !enable;
			this.btnInit.Enabled = !finished;
			this.checkBoxReserve.Enabled = enable;
			this.pasteCheckBox.Enabled = enable;
			foreach (ComboBox comboBox in this.comboList)
			{
				comboBox.Enabled = enable;
			}
			this.isOperating(this.type, !enable, finished);
		}

		public delegate void Del_IsOperating(int type, bool isRunning, bool finished);

		public delegate void Del_PrintLog(string msg);

		public delegate void Del_PrintLogLeft(string msg);

		public delegate void Del_Refresh();
	}
}