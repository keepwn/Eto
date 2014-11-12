using Eto.Forms;
using System.Collections.Generic;
using Eto.Mac.Forms.Actions;
using System.Linq;
using Eto.Mac.Forms;
using System.Collections.ObjectModel;

#if XAMMAC2
using AppKit;
using Foundation;
using CoreGraphics;
using ObjCRuntime;
using CoreAnimation;
#else
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.CoreGraphics;
using MonoMac.ObjCRuntime;
using MonoMac.CoreAnimation;
#endif

namespace Eto.Mac.Forms.Menu
{
	public class MenuBarHandler : WidgetHandler<NSMenu, MenuBar>, MenuBar.IHandler
	{
		public bool AddPrintingMenuItems { get; set; }

		string applicationMenuName = "Application";

		public MenuBarHandler()
		{
			Control = new NSMenu();
			Control.AutoEnablesItems = false;
			Control.ShowsStateColumn = true;
		}

		public void AddMenu(int index, MenuItem item)
		{
			var itemHandler = item.Handler as IMenuHandler;
			if (itemHandler != null)
				itemHandler.EnsureSubMenu();
			Control.InsertItem((NSMenuItem)item.ControlObject, index);
		}

		public void RemoveMenu(MenuItem item)
		{
			Control.RemoveItem((NSMenuItem)item.ControlObject);
		}

		public void Clear()
		{
			Control.RemoveAllItems();
		}

		public ButtonMenuItem ApplicationMenu
		{
			get { return Widget.Items.GetSubmenu(applicationMenuName, -100); }
		}

		public ButtonMenuItem HelpMenu
		{
			get { return Widget.Items.GetSubmenu("&Help", 1000); }
		}

		MenuItem quitItem;
		public void SetQuitItem(MenuItem item)
		{
			item.Order = 1000;
			var appMenu = ApplicationMenu;
			if (quitItem != null)
				appMenu.Items.Remove(quitItem);
			appMenu.Items.Add(item);
			quitItem = item;
		}

		MenuItem aboutItem;
		public void SetAboutItem(MenuItem item)
		{
			item.Order = -100;
			var appMenu = ApplicationMenu;
			if (aboutItem != null)
				appMenu.Items.Remove(aboutItem);
			appMenu.Items.Add(item);
			if (aboutItem == null)
				appMenu.Items.AddSeparator(item.Order);
			aboutItem = item;
		}

		public IEnumerable<Command> GetSystemCommands()
		{
			var appName = Application.Instance.Name ?? NSRunningApplication.CurrentApplication.LocalizedName;
			yield return new Command((sender, e) => NSApplication.SharedApplication.Hide(NSApplication.SharedApplication))
			{ 
				ID = "mac_hide", 
				MenuText = string.Format("Hide {0}", appName),
				ToolBarText = "Hide",
				ToolTip = string.Format("Hides the main {0} window", appName), 
				Shortcut = Keys.H | Keys.Application
			};

			yield return new Command((sender, e) => NSApplication.SharedApplication.HideOtherApplications(NSApplication.SharedApplication))
			{
				ID = "mac_hideothers", 
				MenuText = "Hide Others",
				ToolBarText = "Hide Others",
				ToolTip = "Hides all other application windows",
				Shortcut = Keys.H | Keys.Application | Keys.Alt
			};

			yield return new Command((sender, e) => NSApplication.SharedApplication.UnhideAllApplications(NSApplication.SharedApplication))
			{
				ID = "mac_showall",
				MenuText = "Show All",
				ToolBarText = "Show All",
				ToolTip = "Show All Windows"
			};

			yield return new MacCommand("mac_performMiniaturize", "Minimize", "performMiniaturize:") { Shortcut = Keys.Application | Keys.M };
			yield return new MacCommand("mac_performZoom", "Zoom", "performZoom:");
			yield return new MacCommand("mac_performClose", "Close", "performClose:") { Shortcut = Keys.Application | Keys.W };
			yield return new MacCommand("mac_arrangeInFront", "Bring All To Front", "arrangeInFront:");
			yield return new MacCommand("mac_cut", "Cut", "cut:") { Shortcut = Keys.Application | Keys.X };
			yield return new MacCommand("mac_copy", "Copy", "copy:") { Shortcut = Keys.Application | Keys.C };
			yield return new MacCommand("mac_paste", "Paste", "paste:") { Shortcut = Keys.Application | Keys.V };
			yield return new MacCommand("mac_pasteAsPlainText", "Paste and Match Style", "pasteAsPlainText:") { Shortcut = Keys.Application | Keys.Alt | Keys.Shift | Keys.V };
			yield return new MacCommand("mac_delete", "Delete", "delete:");
			yield return new MacCommand("mac_selectAll", "Select All", "selectAll:") { Shortcut = Keys.Application | Keys.A };
			yield return new MacCommand("mac_undo", "Undo", "undo:") { Shortcut = Keys.Application | Keys.Z };
			yield return new MacCommand("mac_redo", "Redo", "redo:") { Shortcut = Keys.Application | Keys.Shift | Keys.Z };
			yield return new MacCommand("mac_toggleFullScreen", "Enter Full Screen", "toggleFullScreen:") { Shortcut = Keys.Application | Keys.Control | Keys.F };
			yield return new MacCommand("mac_runPageLayout", "Page Setup...", "runPageLayout:") { Shortcut = Keys.Application | Keys.Shift | Keys.P };
			yield return new MacCommand("mac_print", "Print...", "print:") { Shortcut = Keys.Application | Keys.P };
		}

		public void CreateLegacySystemMenu()
		{
			applicationMenuName = Application.Instance.Name ?? applicationMenuName;
			CreateSystemMenu();
		}

		public void CreateSystemMenu()
		{
			var items = Widget.Items;
			var lookup = Widget.SystemCommands.ToLookup(r => r.ID);
			if (Widget.IncludeSystemItems.HasFlag(MenuBarSystemItems.Quit) && quitItem == null)
			{
				var application = ApplicationMenu;
				var quitCommand = new Command { MenuText = "Quit", Shortcut = Keys.Application | Keys.Q };
				quitCommand.Executed += (sender, e) => Application.Instance.Quit();
				application.Items.AddSeparator(999);
				application.Items.Add(quitCommand, 1000);
			}
			if (Widget.IncludeSystemItems.HasFlag(MenuBarSystemItems.Common))
			{
				var application = ApplicationMenu;
				application.Items.AddSeparator(800);
				application.Items.AddRange(lookup["mac_hide"], 800);
				application.Items.AddRange(lookup["mac_hideothers"], 800);
				application.Items.AddRange(lookup["mac_showall"], 800);
				application.Items.AddSeparator(801);

				var file = items.GetSubmenu("&File", 100);
				file.Trim = true;
				file.Items.AddSeparator(900);
				file.Items.AddRange(lookup["mac_performClose"], 900);

				if (AddPrintingMenuItems)
				{
					file.Items.AddSeparator(1000);
					file.Items.AddRange(lookup["mac_runPageLayout"], 1000);
					file.Items.AddRange(lookup["mac_print"], 1000);
				}

				var edit = items.GetSubmenu("&Edit", 200);
				edit.Trim = true;
				edit.Items.AddSeparator(100);
				edit.Items.AddRange(lookup["mac_undo"], 100);
				edit.Items.AddRange(lookup["mac_redo"], 100);
				edit.Items.AddSeparator(101);

				edit.Items.AddSeparator(200);
				edit.Items.AddRange(lookup["mac_cut"], 200);
				edit.Items.AddRange(lookup["mac_copy"], 200);
				edit.Items.AddRange(lookup["mac_paste"], 200);
				edit.Items.AddRange(lookup["mac_delete"], 200);
				edit.Items.AddRange(lookup["mac_selectAll"], 200);
				edit.Items.AddSeparator(201);

				var window = items.GetSubmenu("&Window", 900);
				window.Trim = true;
				window.Items.AddSeparator(100);
				window.Items.AddRange(lookup["mac_performMiniaturize"], 100);
				window.Items.AddRange(lookup["mac_performZoom"], 100);
				window.Items.AddSeparator(101);

				window.Items.AddSeparator(200);
				window.Items.AddRange(lookup["mac_arrangeInFront"], 200);
				window.Items.AddSeparator(201);

				if (ApplicationHandler.Instance.AddFullScreenMenuItem)
				{
					var view = items.GetSubmenu("&View", 300);
					view.Trim = true;
					view.Items.AddSeparator(900);
					view.Items.AddRange(lookup["mac_toggleFullScreen"], 900);
					view.Items.AddSeparator(901);
				}

				var help = items.GetSubmenu("&Help", 900);
				// always show help menu
				help.Trim = false;
			}
		}
	}
}
