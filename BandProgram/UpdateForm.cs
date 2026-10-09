using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BandProgram
{
	// 업데이트 다운로드·설치 동안 띄우는 진행률 창. 닫기 버튼 없이, 작업이 끝나면 스스로 닫힌다.
	internal sealed class UpdateForm : Form
	{
		private readonly Label label = new Label();
		private readonly ProgressBar bar = new ProgressBar();
		private int lastPercent = -1;
		private bool done;

		public UpdateForm(Updater updater, UpdateManifest manifest)
		{
			this.Text = "밴드프로그램 업데이트";
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.ControlBox = false;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.ClientSize = new Size(360, 90);
			this.label.SetBounds(12, 14, 336, 20);
			this.label.Text = string.Concat("새 버전으로 업데이트 중... (", manifest.Version, ")");
			this.bar.SetBounds(12, 46, 336, 22);
			this.bar.Style = ProgressBarStyle.Marquee;
			this.FormClosing += (sender, e) =>
			{
				if (!this.done && e.CloseReason != CloseReason.WindowsShutDown)
				{
					e.Cancel = true;
				}
			};
			this.Controls.Add(this.label);
			this.Controls.Add(this.bar);
			this.Shown += (sender, e) =>
			{
				Task.Run(() => updater.Install(manifest, this.ReportProgress)).ContinueWith(task =>
				{
					this.SafeInvoke(() =>
					{
						this.Result = task.IsFaulted ? null : task.Result;
						this.done = true;
						this.Close();
					});
				});
			};
		}

		public UpdateResult Result { get; private set; }

		private void ReportProgress(long received, long? total)
		{
			if (total == null || total.Value <= 0)
			{
				return;
			}
			int percent = (int)(received * 100 / total.Value);
			if (percent == this.lastPercent)
			{
				return;
			}
			this.lastPercent = percent;
			this.SafeInvoke(() =>
			{
				this.bar.Style = ProgressBarStyle.Continuous;
				this.bar.Value = Math.Min(100, Math.Max(0, percent));
			});
		}

		private void SafeInvoke(Action action)
		{
			try
			{
				if (this.IsHandleCreated && !this.IsDisposed)
				{
					this.BeginInvoke(action);
				}
			}
			catch (ObjectDisposedException)
			{
			}
			catch (InvalidOperationException)
			{
			}
		}
	}
}
