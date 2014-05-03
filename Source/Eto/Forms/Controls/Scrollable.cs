using System;
using Eto.Drawing;

namespace Eto.Forms
{
	public partial interface IScrollable : IPanel
	{
		void UpdateScrollSizes();

		Point ScrollPosition { get; set; }

		Size ScrollSize { get; set; }

		BorderType Border { get; set; }

		Rectangle VisibleRect { get; }

		bool ExpandContentWidth { get; set; }

		bool ExpandContentHeight { get; set; }
	}

	public class ScrollEventArgs : EventArgs
	{
		public Point ScrollPosition { get; private set; }

		public ScrollEventArgs(Point scrollPosition)
		{
			this.ScrollPosition = scrollPosition;
		}
	}

	public enum BorderType
	{
		Bezel,
		Line,
		None
	}

	[Handler(typeof(IScrollable))]
	public partial class Scrollable : Panel
	{
		new IScrollable Handler { get { return (IScrollable)base.Handler; } }

		public const string ScrollEvent = "Scrollable.ScrollEvent";

		public event EventHandler<ScrollEventArgs> Scroll
		{
			add { Properties.AddHandlerEvent(ScrollEvent, value); }
			remove { Properties.RemoveEvent(ScrollEvent, value); }
		}

		public virtual void OnScroll(ScrollEventArgs e)
		{
			Properties.TriggerEvent(ScrollEvent, this, e);
		}

		static Scrollable()
		{
			EventLookup.Register<Scrollable>(c => c.OnScroll(null), Scrollable.ScrollEvent);
		}

		public Scrollable()
		{
		}

		[Obsolete("Use default constructor instead")]
		public Scrollable(Generator generator) : this(generator, typeof(IScrollable))
		{
		}

		[Obsolete("Use default constructor and HandlerAttribute instead")]
		protected Scrollable(Generator generator, Type type, bool initialize = true)
			: base(generator, type, initialize)
		{
		}

		public void UpdateScrollSizes()
		{
			Handler.UpdateScrollSizes();
		}

		public Point ScrollPosition
		{
			get { return Handler.ScrollPosition; }
			set { Handler.ScrollPosition = value; }
		}

		public Size ScrollSize
		{
			get { return Handler.ScrollSize; }
			set { Handler.ScrollSize = value; }
		}

		public BorderType Border
		{
			get { return Handler.Border; }
			set { Handler.Border = value; }
		}

		public Rectangle VisibleRect
		{
			get { return Handler.VisibleRect; }
		}

		public bool ExpandContentWidth
		{
			get { return Handler.ExpandContentWidth; }
			set { Handler.ExpandContentWidth = value; }
		}

		public bool ExpandContentHeight
		{
			get { return Handler.ExpandContentHeight; }
			set { Handler.ExpandContentHeight = value; }
		}
	}
}
