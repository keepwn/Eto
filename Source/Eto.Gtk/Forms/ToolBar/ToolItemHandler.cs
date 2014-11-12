using Eto.Forms;
using Eto.Drawing;

namespace Eto.GtkSharp.Forms.ToolBar
{
	public interface IToolBarItemHandler
	{
		void CreateControl(ToolBarHandler handler, int index);
		
	}

	public abstract class ToolItemHandler<TControl, TWidget> : WidgetHandler<TControl, TWidget>, ToolItem.IHandler, IToolBarItemHandler
		where TControl: Gtk.Widget
		where TWidget: ToolItem
	{
		bool enabled = true;
		Image image;

		protected Gtk.Image GtkImage { get; set; }

		public abstract void CreateControl(ToolBarHandler handler, int index);
		
		public string Text { get; set; }
		
		public string ToolTip { get; set; }

		public Image Image
		{
			get { return image; }
			set
			{
				image = value;
				GtkImage = image.ToGtk (Gtk.IconSize.Button);
			}
		}

		public bool Enabled 
		{
			get { return enabled; }
			set { 
				enabled = value;
				if (Control != null)
					Control.Sensitive = value;
			}
		}

		public void CreateFromCommand(Command command)
		{
		}
	}
}
