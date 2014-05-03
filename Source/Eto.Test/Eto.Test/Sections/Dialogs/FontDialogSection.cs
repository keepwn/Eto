using System;
using Eto.Drawing;
using Eto.Forms;
using System.Collections.Generic;
using System.Linq;

namespace Eto.Test.Sections.Dialogs
{
	public class FontDialogSection : Scrollable
	{
		Font selectedFont;
		TextArea preview;
		ListBox fontList;
		ListBox fontStyles;
		ListBox fontSizes;
		bool updating;
		Drawable metricsPreview;

		public FontDialogSection()
		{
			var layout = new DynamicLayout(new Size(5, 5));

			layout.AddSeparateRow(null, PickFont(), PickFontWithStartingFont(), SetToFontFamily(), null);
			layout.AddSeparateRow(null, new Label { Text = "Set Font Family", VerticalAlign = VerticalAlign.Middle }, PickFontFamily(), null);

			layout.AddSeparateRow(null, FontList(), FontStyles(), FontSizes(), null);
			layout.AddSeparateRow(null, new Label { Text = "Style:" }, BoldFont(), ItalicFont(), UnderlineFont(), StrikeoutFont(), null);

			var tabs = new TabControl();
			tabs.TabPages.Add(new TabPage(Preview()) { Text = "Preview" });
			tabs.TabPages.Add(new TabPage(Metrics()) { Text = "Metrics" });

			layout.Add(tabs, yscale: true);
			UpdatePreview(Fonts.Serif(18, FontStyle.Bold));

			Content = layout;
		}

		Control PickFontFamily()
		{
			var fontFamilyName = new TextBox { Text = "Times, serif", Size = new Size(200, -1) };

			var button = new Button { Text = "Set" };
			button.Click += (sender, e) =>
			{

				try
				{
					UpdatePreview(new Font(fontFamilyName.Text, selectedFont.Size));
				}
				catch (Exception ex)
				{
					Log.Write(this, "Exception: {0}", ex);
				}
			};

			var layout = new DynamicLayout(Padding.Empty);
			layout.BeginHorizontal();
			layout.AddCentered(fontFamilyName, Padding.Empty, Size.Empty);
			layout.AddCentered(button, Padding.Empty, Size.Empty);
			return layout;
		}

		static Control Descender()
		{
			var control = new Label { TextColor = Colors.Red };
			control.TextBinding.Bind<Font>(r => r.Descent.ToString());
			return control;
		}

		static Control Ascender()
		{
			var control = new Label { TextColor = Colors.Blue };
			control.TextBinding.Bind<Font>(r => r.Ascent.ToString());
			return control;
		}

		static Control XHeight()
		{
			var control = new Label { TextColor = Colors.Green };
			control.TextBinding.Bind<Font>(r => r.XHeight.ToString());
			return control;
		}

		static Control LineHeight()
		{
			var control = new Label { TextColor = Colors.Orange };
			control.TextBinding.Bind<Font>(r => r.LineHeight.ToString());
			return control;
		}

		static Control Leading()
		{
			var control = new Label { TextColor = Colors.Orange };
			control.TextBinding.Bind<Font>(r => r.Leading.ToString());
			return control;
		}

		static Control BaseLine()
		{
			var control = new Label { TextColor = Colors.Black };
			control.TextBinding.Bind<Font>(r => r.Baseline.ToString());
			return control;
		}

		Control PickFont()
		{
			var button = new Button { Text = "Pick Font" };
			button.Click += delegate
			{
				var dialog = new FontDialog();
				dialog.FontChanged += delegate
				{
					// you need to handle this event for OS X, where the dialog is a floating window
					UpdatePreview(dialog.Font);
					Log.Write(dialog, "FontChanged, Font: {0}", dialog.Font);
				};
				var result = dialog.ShowDialog(ParentWindow);
				Log.Write(dialog, "Result: {0}", result);
			};
			return button;
		}

		Control PickFontWithStartingFont()
		{
			var button = new Button { Text = "Pick Font with initial starting font" };
			button.Click += delegate
			{
				var dialog = new FontDialog
				{
					Font = selectedFont
				};
				dialog.FontChanged += delegate
				{
					// need to handle this event for OS X, where the dialog is a floating window
					UpdatePreview(dialog.Font);
					Log.Write(dialog, "FontChanged, Font: {0}", dialog.Font);
				};
				var result = dialog.ShowDialog(ParentWindow);
				// do not get the font here, it may return immediately with a result of DialogResult.None on certain platforms
				Log.Write(dialog, "Result: {0}", result);
			};
			return button;
		}

		Control SetToFontFamily()
		{
			var button = new Button { Text = "Set to a specific font family (Times New Roman 20pt)" };
			button.Click += delegate
			{
				var family = new FontFamily("Times New Roman");
				var font = new Font(family, 20);
				UpdatePreview(font);
			};
			return button;
		}

		Control FontList()
		{
			fontList = new ListBox { Size = new Size(300, 180) };
			var lookup = Fonts.AvailableFontFamilies.ToDictionary(r => r.Name);
			fontList.Items.AddRange(lookup.Values.OrderBy(r => r.Name).Select(r => new ListItem { Text = r.Name, Key = r.Name }).OfType<IListItem>());
			fontList.SelectedIndexChanged += (sender, e) =>
			{
				if (updating || fontList.SelectedKey == null)
					return;
				var family = lookup[fontList.SelectedKey];
				UpdatePreview(new Font(family.Typefaces.First(), selectedFont.Size, selectedFont.FontDecoration));
			};

			return fontList;
		}

		Control FontStyles()
		{
			fontStyles = new ListBox { Size = new Size(150, 100) };
			fontStyles.SelectedIndexChanged += (sender, e) =>
			{
				if (updating)
					return;
				var face = selectedFont.Family.Typefaces.FirstOrDefault(r => r.Name == fontStyles.SelectedKey);
				if (face != null)
				{
					UpdatePreview(new Font(face, selectedFont.Size, selectedFont.FontDecoration));
				}
			};
			return fontStyles;
		}

		Control FontSizes()
		{
			fontSizes = new ListBox { Size = new Size(60, 100) };
			for (int i = 6; i < 72; i++)
			{
				fontSizes.Items.Add(i.ToString(), i.ToString());
			}
			fontSizes.SelectedIndexChanged += (sender, e) =>
			{
				if (updating)
					return;
				float size;
				if (float.TryParse(fontSizes.SelectedKey, out size))
				{
					UpdatePreview(new Font(selectedFont.Typeface, size, selectedFont.FontDecoration));
				}
			};
			return fontSizes;
		}

		static Control BoldFont()
		{
			var control = new CheckBox { Text = "Bold", Enabled = false };
			control.CheckedBinding.Bind<Font>(r => r.Bold);
			return control;
		}

		static Control ItalicFont()
		{
			var control = new CheckBox { Text = "Italic", Enabled = false };
			control.CheckedBinding.Bind<Font>(r => r.Italic);
			return control;
		}

		Control UnderlineFont()
		{
			var control = new CheckBox { Text = "Underline" };
			control.CheckedBinding.Bind<Font>(f => f.Underline, (f,val) => {
				var decoration = selectedFont.FontDecoration;
				if (val ?? false) decoration |= FontDecoration.Underline;
				else decoration &= ~FontDecoration.Underline;
				UpdatePreview(new Font(selectedFont.Typeface, selectedFont.Size, decoration));
			});
			return control;
		}

		Control StrikeoutFont()
		{
			var control = new CheckBox { Text = "Strikethrough" };
			control.CheckedBinding.Bind<Font>(f => f.Strikethrough, (f,val) => {
				var decoration = selectedFont.FontDecoration;
				if (val ?? false) decoration |= FontDecoration.Strikethrough;
				else decoration &= ~FontDecoration.Strikethrough;
				UpdatePreview(new Font(selectedFont.Typeface, selectedFont.Size, decoration));
			});
			return control;
		}

		void UpdatePreview(Font font)
		{
			if (updating)
				return;
			updating = true;
			var newFamily = selectedFont == null || selectedFont.Family != font.Family;
			selectedFont = font;
			DataContext = selectedFont;
			preview.Font = selectedFont;
			preview.Invalidate();

			var family = selectedFont.Family;
			if (newFamily)
			{
				fontStyles.Items.Clear();
				fontStyles.Items.AddRange(family.Typefaces.Select(r => new ListItem { Text = r.Name, Key = r.Name }).OfType<IListItem>());
			}
			fontStyles.SelectedKey = selectedFont.Typeface.Name;
			fontList.SelectedKey = family.Name;
			fontSizes.SelectedKey = font.Size.ToString();
			metricsPreview.Invalidate();

			updating = false;
		}

		Control Metrics()
		{
			var layout = new DynamicLayout(Padding.Empty);
			layout.BeginHorizontal();
			layout.BeginVertical();
			layout.Add(null);
			layout.AddRow(new Label { Text = "Descent" }, Descender());
			layout.AddRow(new Label { Text = "Ascent" }, Ascender());
			layout.AddRow(new Label { Text = "Leading" }, Leading());
			layout.Add(null);
			layout.EndBeginVertical();
			layout.Add(null);
			layout.AddRow(new Label { Text = "BaseLine" }, BaseLine());
			layout.AddRow(new Label { Text = "XHeight" }, XHeight());
			layout.AddRow(new Label { Text = "LineHeight" }, LineHeight());
			layout.Add(null);
			layout.EndBeginVertical();
			layout.Add(null);
			layout.Add(MetricsPreview());
			layout.Add(null);
			layout.EndVertical();
			layout.EndHorizontal();
			return layout;
		}

		Control MetricsPreview()
		{
			metricsPreview = new Drawable { Size = new Size(200, 100) };
			metricsPreview.Paint += (sender, pe) =>
			{
				var width = metricsPreview.Size.Width;
				pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

				pe.Graphics.DrawRectangle(Colors.Black, new RectangleF(metricsPreview.Size));

				var size = pe.Graphics.MeasureString(selectedFont, preview.Text);
				var scale = ParentWindow.Screen.Scale;

				var ypos = Math.Max(0, (metricsPreview.Size.Height - size.Height) / 2);

				pe.Graphics.FillRectangle(Brushes.GhostWhite, new RectangleF(new PointF(0, ypos), size));

				pe.Graphics.DrawText(selectedFont, Colors.Black, 0, ypos, preview.Text);

				var baseline = ypos + selectedFont.Baseline * scale;
				pe.Graphics.DrawLine(Pens.Black, 0, baseline, width, baseline);

				var ascender = baseline - selectedFont.Ascent * scale;
				pe.Graphics.DrawLine(Pens.Blue, 0, ascender, width, ascender);

				var descender = baseline + selectedFont.Descent * scale;
				pe.Graphics.DrawLine(Pens.Red, 0, descender, width, descender);

				var xheight = baseline - selectedFont.XHeight * scale;
				pe.Graphics.DrawLine(Pens.Green, 0, xheight, width, xheight);

				var lineheight = ypos + selectedFont.LineHeight * scale;
				pe.Graphics.DrawLine(Pens.Orange, 0, lineheight, width, lineheight);
			};
			return metricsPreview;
		}

		Control Preview()
		{
			preview = new TextArea { Wrap = true, Size = new Size(-1, 100) };
			preview.Text = "The quick brown fox jumps over the lazy dog";

			return preview;
		}
	}
}

