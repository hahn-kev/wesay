using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WeSay.UI
{
	public class DisplaySettings
	{
		public static DisplaySettings Default = new DisplaySettings();

		public DisplaySettings()
		{
			_startBackgroundColors = new Hashtable();
			_endBackgroundColors = new Hashtable();
			SetColors();
		}

		private Color _backgroundColor;
		private Color _currentIndicatorColor;
		private Color _wsLabelColor;
		private Color _startBackgroundColorDefault;
		private Color _endBackgroundColorDefault;
		private readonly Hashtable _startBackgroundColors;
		private readonly Hashtable _endBackgroundColors;

		private string _skinName;

		public Color BackgroundColor
		{
			get { return _backgroundColor; }
			set { _backgroundColor = value; }
		}

		public Color StartBackgroundColorDefault
		{
			get { return _startBackgroundColorDefault; }
			set { _startBackgroundColorDefault = value; }
		}

		public Color EndBackgroundColorDefault
		{
			get { return _endBackgroundColorDefault; }
			set { _endBackgroundColorDefault = value; }
		}

		public bool UsingProjectorScheme
		{
			get { return Default._skinName == "projector"; }
		}

		public Color WritingSystemLabelColor
		{
			get { return _wsLabelColor; }
			set { _wsLabelColor = value; }
		}

		public Color CurrentIndicatorColor
		{
			get { return _currentIndicatorColor; }
			set { _currentIndicatorColor = value; }
		}

		public Color GetStartBackgroundColor(object obj)
		{
			if (obj != null && _startBackgroundColors.Contains(obj.GetType().Name))
			{
				return (Color)_startBackgroundColors[obj.GetType().Name];
			}
			return _startBackgroundColorDefault;
		}

		public Color GetEndBackgroundColor(object obj)
		{
			if (obj != null && _endBackgroundColors.Contains(obj.GetType().Name))
			{
				return (Color)_endBackgroundColors[obj.GetType().Name];
			}
			return _endBackgroundColorDefault;
		}

		public Color GetAverageBackgroundColor(object obj)
		{
			Color startColor = GetStartBackgroundColor(obj);
			Color endColor = GetEndBackgroundColor(obj);
			return Color.FromArgb((startColor.R + endColor.R) / 2,
								  (startColor.G + endColor.G) / 2,
								  (startColor.B + endColor.B) / 2);
		}

		public void ToggleColorScheme()
		{
			if (_skinName != "projector")
			{
				_skinName = "projector";
			}
			else
			{
				_skinName = string.Empty;
			}
			SetColors();
		}

		public string SkinName
		{
			get { return _skinName; }

			set
			{
				_skinName = value;
				SetColors();
			}
		}

		public void PaintBackground(Control control, PaintEventArgs e)
		{
			Debug.Assert(control != null);
			Debug.Assert(e != null);
			// If the rectangle is non-existent, then don't try to paint it - exceptions will happen
			if (control.ClientRectangle.Width <= 0 || control.ClientRectangle.Height <= 0)
			{
				return;
			}
			LinearGradientBrush gradBrush = new LinearGradientBrush(control.ClientRectangle,
																	GetStartBackgroundColor(control),
																	GetEndBackgroundColor(control),
																	LinearGradientMode.Vertical);
			e.Graphics.FillRectangle(gradBrush, e.ClipRectangle);
			gradBrush.Dispose();
		}

		public void PaintBackground(Graphics graphics, Rectangle rectangle, object objectForColor)
		{
			Debug.Assert(graphics != null);
			// If the rectangle is non-existent, then don't try to paint it - exceptions will happen
			if (rectangle.Width <= 0 || rectangle.Height <= 0)
			{
				return;
			}
			LinearGradientBrush gradBrush = new LinearGradientBrush(rectangle,
																	GetStartBackgroundColor(
																			objectForColor),
																	GetEndBackgroundColor(
																			objectForColor),
																	LinearGradientMode.Vertical);
			graphics.FillRectangle(gradBrush, rectangle);
			gradBrush.Dispose();
		}

		private void SetColors()
		{
			if (_skinName == "projector")
			{
				BackgroundColor = Color.LightGreen;
				StartBackgroundColorDefault = Color.White;
				EndBackgroundColorDefault = Color.LightGreen;
				_startBackgroundColors["Dash"] = Color.White;
				_endBackgroundColors["Dash"] = Color.LightBlue;
				_wsLabelColor = Color.Blue;
			}
			else
			{
				//original                BackgroundColor = Color.FromArgb(235, 255, 215);
				BackgroundColor = Color.FromArgb(203, 255, 185);
				StartBackgroundColorDefault = Color.FromArgb(229, 255, 220);
				EndBackgroundColorDefault = Color.FromArgb(203, 255, 185);
				_startBackgroundColors["Dash"] = Color.FromArgb(232, 242, 255); // 220,230,242
				_endBackgroundColors["Dash"] = Color.FromArgb(164, 191, 224); // 186, 211, 242

				// CurrentIndicatorColor = Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(253)))), ((int)(((byte)(219)))));
				_wsLabelColor = Color.Gray;
			}
			CurrentIndicatorColor = BackgroundColor;
		}

		public static List<Size> GetPossibleTextSizes(IDeviceContext dc,
													  string text,
													  Font font,
													  TextFormatFlags textFormatFlags)
		{
			Dictionary<int, int> requiredSizes = new Dictionary<int, int>();
			int maxWidth;
			Size sizeNeeded;
			sizeNeeded = TextRenderer.MeasureText(dc,
												  text,
												  font,
												  new Size(int.MaxValue, int.MaxValue),
												  textFormatFlags);
			requiredSizes.Add(sizeNeeded.Height, sizeNeeded.Width);
			// ws-34187: Better feedback for slower computers
			// This was starting at 1 and going by increments of 1,
			// and measuretext is slow. So for SIL CAWL, it was talking 30 seconds
			// on slow machines!  Not only did this mean that they wouldn't
			// see this popup unless they were very patient, but worse, if they
			// were just trying to launch the task, we wouldn't get the OnClick
			// for 30 seconds.  It's still a hack, but I (JH) made it at least start
			// at a reasonable spot, make larger jumps, and stop looking after the box
			// is already large
			int minWidth = 200;
			int stepSize = 40;
			int kAsWideAsWeWouldWant = 500;
			maxWidth = Math.Min(sizeNeeded.Width, kAsWideAsWeWouldWant);
			for (int i = minWidth; i < maxWidth; i = i + stepSize)
			{
				sizeNeeded = TextRenderer.MeasureText(dc,
													  text,
													  font,
													  new Size(i, int.MaxValue),
													  textFormatFlags);
				if (!requiredSizes.ContainsKey(sizeNeeded.Height))
				{
					requiredSizes.Add(sizeNeeded.Height, sizeNeeded.Width);
				}
				else if (sizeNeeded.Width < requiredSizes[sizeNeeded.Height])
				{
					requiredSizes[sizeNeeded.Height] = sizeNeeded.Width;
				}
				// skip unnecessary checks
				if (sizeNeeded.Width > i)
				{
					i = sizeNeeded.Width;
				}
			}
			// convert to return type
			List<Size> possibleSizes = new List<Size>(requiredSizes.Count);
			foreach (KeyValuePair<int, int> size in requiredSizes)
			{
				possibleSizes.Add(new Size(size.Value, size.Key));
			}
			return possibleSizes;
		}
	}
}