using System;


namespace Eto.Forms
{
	public interface IImageTextCell : ICell
	{
	}

	[Handler(typeof(IImageTextCell))]
	public class ImageTextCell : Cell
	{
		public IndirectBinding ImageBinding { get; set; }
		
		public IndirectBinding TextBinding { get; set; }
		
		public ImageTextCell (int imageColumn, int textColumn)
		{
			ImageBinding = new ColumnBinding (imageColumn);
			TextBinding = new ColumnBinding (textColumn);
		}
		
		public ImageTextCell (string imageProperty, string textProperty)
		{
			ImageBinding = new PropertyBinding(imageProperty);
			TextBinding = new PropertyBinding(textProperty);
		}
		
		public ImageTextCell()
		{
		}

		[Obsolete("Use default constructor instead")]
		public ImageTextCell (Generator generator)
			: base(generator, typeof(IImageTextCell), true)
		{
		}
	}
}

