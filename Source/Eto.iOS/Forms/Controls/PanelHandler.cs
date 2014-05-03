using System;
using MonoTouch.UIKit;
using Eto.Forms;
using Eto.Mac.Forms;

namespace Eto.iOS.Forms.Controls
{
	public class PanelHandler : MacPanel<UIView, Panel>, IPanel
	{
		public override UIView ContainerControl { get { return Control; } }

		protected override void Initialize ()
		{
			base.Initialize ();
			Control = new UIView();
			Control.BackgroundColor = UIColor.White;
		}
	}
}

