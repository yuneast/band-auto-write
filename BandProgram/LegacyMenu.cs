using System;
using System.Windows.Forms;

namespace BandProgram
{
	// .NET Framework의 MenuItem/ContextMenu는 .NET 10에서 실행 시 PlatformNotSupportedException을 던진다.
	// 기존 MenuClick 핸들러가 쓰던 MenuItem.Index(구분선 "-" 포함 위치)를 Tag로 보존한다.
	internal static class LegacyMenu
	{
		public static ContextMenuStrip Create(EventHandler onClick, params string[] labels)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			for (int i = 0; i < labels.Length; i++)
			{
				if (labels[i] == "-")
				{
					menu.Items.Add(new ToolStripSeparator());
					continue;
				}
				ToolStripMenuItem item = new ToolStripMenuItem(labels[i]) { Tag = i };
				item.Click += onClick;
				menu.Items.Add(item);
			}
			return menu;
		}

		public static int IndexOf(object sender)
		{
			return (int)((ToolStripItem)sender).Tag;
		}
	}
}
