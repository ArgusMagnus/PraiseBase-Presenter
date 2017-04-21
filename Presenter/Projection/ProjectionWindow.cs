/*
 *   PraiseBase Presenter
 *   The open source lyrics and image projection software for churches
 *
 *   http://praisebase.org
 *
 *   This program is free software; you can redistribute it and/or
 *   modify it under the terms of the GNU General Public License
 *   as published by the Free Software Foundation; either version 2
 *   of the License, or (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program; if not, write to the Free Software
 *   Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 *
 */

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using PraiseBase.Presenter.Properties;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PraiseBase.Presenter.Projection
{
	static class XwtUtil
	{
		public static Xwt.Point ToXwtPoint(this Point p) => new Xwt.Point(p.X, p.Y);
		public static Xwt.Size ToXwtSize(this Size s) => new Xwt.Size(s.Width, s.Height);
		public static Xwt.Rectangle ToXwtRect(this Rectangle r) => new Xwt.Rectangle(r.X, r.Y, r.Width, r.Height);
		public static Xwt.Drawing.Color ToXwtColor(this Color c) => new Xwt.Drawing.Color(c.R, c.G, c.B, c.A);

		public static Xwt.Drawing.Image ToXwtImage(this Bitmap bitmap)
		{
			using (var stream = new BmpStream(bitmap))
				return Xwt.Drawing.Image.FromStream(stream);
		}

		class BmpStream : Stream
		{
			const uint BmpHeaderSize = 14;
			const uint DibHeaderSize = 108; // BITMAPV4HEADER
			const uint PixelArrayOffset = BmpHeaderSize + DibHeaderSize;
			const uint CompressionMethod = 3; // BI_BITFIELDS
			const uint MaskR = 0x00_FF_00_00;
			const uint MaskG = 0x00_00_FF_00;
			const uint MaskB = 0x00_00_00_FF;
			const uint MaskA = 0xFF_00_00_00;

			readonly Bitmap _bitmap;
			readonly byte[] _header;
			readonly uint _length;
			readonly uint _stride;
			readonly uint _rowLength;
			readonly BitmapData _data;
			uint _pos;

			public BmpStream(Bitmap bitmap)
			{
				if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
					throw new NotSupportedException($"Bitmap format {bitmap.PixelFormat} is not yet supported.");

				_bitmap = bitmap;
				_rowLength = 4 * (uint)bitmap.Width;
				_stride = ((4 * 8 * (uint)bitmap.Width + 31) / 32) * 4;
				_length = PixelArrayOffset + _stride * (uint)bitmap.Height;
				_header = GetHeader(_length, _bitmap);
				_pos = 0;
				_data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
					_bitmap.UnlockBits(_data);
				base.Dispose(disposing);
			}

			static byte[] GetHeader(uint fileSize, Bitmap bitmap)
			{
				const double MetersPerInch = 0.0254;

				byte[] header = new byte[BmpHeaderSize + DibHeaderSize];

				using (var ms = new MemoryStream(header))
				using (var writer = new BinaryWriter(ms))
				{
					writer.Write((byte)'B');
					writer.Write((byte)'M');
					writer.Write(fileSize);
					writer.Write(0u);
					writer.Write(PixelArrayOffset);
					writer.Write(DibHeaderSize);
					writer.Write(bitmap.Width);
					writer.Write(-bitmap.Height); // top-down image
					writer.Write((ushort)1);
					writer.Write((ushort)(4 * 8));
					writer.Write(CompressionMethod);
					writer.Write(0);
					writer.Write((int)Math.Round(bitmap.HorizontalResolution / MetersPerInch));
					writer.Write((int)Math.Round(bitmap.VerticalResolution / MetersPerInch));
					writer.Write(0L);
					writer.Write(MaskR);
					writer.Write(MaskG);
					writer.Write(MaskB);
					//if (bitmap.Format == BitmapFormats.FPDFBitmap_BGRA)
						writer.Write(MaskA);
				}
				return header;
			}

			public override bool CanRead => true;

			public override bool CanSeek => true;

			public override bool CanWrite => false;

			public override long Length => _length;

			public override long Position
			{
				get => _pos;
				set
				{
					if (value < 0 || value >= _length)
						throw new ArgumentOutOfRangeException();
					_pos = (uint)value;
				}
			}

			public override void Flush() { }

			public override int Read(byte[] buffer, int offset, int count)
			{
				int bytesToRead = count;
				int returnValue = 0;
				if (_pos < PixelArrayOffset)
				{
					returnValue = Math.Min(count, (int)(PixelArrayOffset - _pos));
					Buffer.BlockCopy(_header, (int)_pos, buffer, offset, returnValue);
					_pos += (uint)returnValue;
					offset += returnValue;
					bytesToRead -= returnValue;
				}

				if (bytesToRead <= 0)
					return returnValue;

				bytesToRead = Math.Min(bytesToRead, (int)(_length - _pos));
				uint idxBuffer = _pos - PixelArrayOffset;

				if (_stride == _data.Stride)
				{
					Marshal.Copy(_data.Scan0 + (int)idxBuffer, buffer, offset, bytesToRead);
					returnValue += bytesToRead;
					_pos += (uint)bytesToRead;
					return returnValue;
				}

				while (bytesToRead > 0)
				{
					int idxInStride = (int)(idxBuffer / _stride);
					int leftInRow = Math.Max(0, (int)_rowLength - idxInStride);
					int paddingBytes = (int)(_stride - _rowLength);
					int read = Math.Min(bytesToRead, leftInRow);
					if (read > 0)
						Marshal.Copy(_data.Scan0 + (int)idxBuffer, buffer, offset, read);
					offset += read;
					idxBuffer += (uint)read;
					bytesToRead -= read;
					returnValue += read;
					read = Math.Min(bytesToRead, paddingBytes);
					for (int i = 0; i < read; i++)
						buffer[offset + i] = 0;
					offset += read;
					idxBuffer += (uint)read;
					bytesToRead -= read;
					returnValue += read;
				}
				_pos = PixelArrayOffset + (uint)idxBuffer;
				return returnValue;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				if (origin == SeekOrigin.Begin)
					Position = offset;
				else if (origin == SeekOrigin.Current)
					Position += offset;
				else if (origin == SeekOrigin.End)
					Position = Length + offset;
				return Position;
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}
		}
	}

	public sealed class ProjectionWindow : Xwt.Window
    {
		const int AnimationFps = 20;
		const int AnimationTpf = 1000 / AnimationFps;
		const string WebViewStopUrl = "http://127.0.0.1/";


		Image CurrentPreviewImage;
        Image CurrentPreviewText;
		readonly Xwt.ImageView _projectionImageBack = new Xwt.ImageView();
		readonly Xwt.ImageView _projectionImage = new Xwt.ImageView() { Opacity = 0 };
		readonly Xwt.WebView _webView = new Xwt.WebView() { Visible = false, HorizontalPlacement = Xwt.WidgetPlacement.Fill, VerticalPlacement = Xwt.WidgetPlacement.Fill, ExpandHorizontal = true, ExpandVertical = true, BackgroundColor = Xwt.Drawing.Colors.Black };
		readonly Xwt.ImageView _textImageBack = new Xwt.ImageView();
		readonly Xwt.ImageView _textImage = new Xwt.ImageView() { Opacity = 0 };
		readonly Xwt.Canvas _blackoutImage = new Xwt.Canvas() { Opacity = 0, BackgroundColor = Xwt.Drawing.Colors.Black };

		static ProjectionWindow()
		{
			Xwt.Application.InitializeAsGuest(string.Empty);
		}

        public ProjectionWindow(Screen projScreen)
        {
			InitializeComponents();
			ShowInTaskbar = false;
            AssignToScreen(projScreen);
        }

		void InitializeComponents()
		{
			Padding = 0;
			var table = new Xwt.Table();
			table.Add(_projectionImageBack, 0, 0);
			table.Add(_projectionImage, 0, 0);
			table.Add(_webView, 0, 0);
			_webView.Loaded += _webView_Loaded;
			table.Add(_textImageBack, 0, 0);
			table.Add(_textImage, 0, 0);
			table.Add(_blackoutImage, 0, 0);
			Content = table;
		}

		private void _webView_Loaded(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(_webView.Url) && _webView.Url != WebViewStopUrl)
			{
				_webView.WidthRequest = Width;
				_webView.HeightRequest = Height;
				_webView.Visible = true;
			}
			else
			{
				_webView.StopLoading();
				_webView.Visible = false;
			}
		}

		/// <summary>
		/// Assigns the window to a screen's coordinates
		/// </summary>
		/// <param name="projScreen"></param>
		public void AssignToScreen(Screen projScreen)
        {
			Location = projScreen.WorkingArea.Location.ToXwtPoint();
			Size = projScreen.WorkingArea.Size.ToXwtSize();
		}

        /// <summary>
        /// Set to blackout
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="animate"></param>
        public void SetBlackout(bool enable, bool animate)
        {
			BlackOut(enable, (animate ? Settings.Default.ProjectionFadeTime : 10));
		}

        /// <summary>
        /// Display image with transition
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="fadetime"></param>
        public void DisplayImage(Bitmap bmp, int fadetime)
        {
			SetProjectionImage(bmp, fadetime);
			CurrentPreviewImage = bmp;
        }

        /// <summary>
        /// Hide image with transition
        /// </summary>
        /// <param name="fadetime"></param>
        public void HideImage(int fadetime)
        {
			HideProjectionImage(fadetime);
			CurrentPreviewImage = new Bitmap((int)Width, (int)Height);
        }

        /// <summary>
        /// Display text with transition
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="fadetime"></param>
        public void DisplayText(Bitmap bmp, int fadetime)
        {
			SetProjectionText(bmp, fadetime);
			CurrentPreviewText = bmp;
        }

        /// <summary>
        /// Hide text with transition
        /// </summary>
        /// <param name="fadetime"></param>
        public void HideText(int fadetime)
        {
            var bmp = new Bitmap((int)Width, (int)Height);
            SetProjectionText(bmp, fadetime);
            CurrentPreviewText = bmp;
        }

		public void ShowWebsite(Uri uri)
		{
			_webView.Url = uri.OriginalString;
			// TODO
			var bmp = new Bitmap((int)Width, (int)Height);
			CurrentPreviewImage = bmp;
			CurrentPreviewText = bmp;
		}

		public void HideWebsite()
		{
			_webView.Url = WebViewStopUrl;
			// TODO
			var bmp = new Bitmap((int)Width, (int)Height);
			CurrentPreviewImage = bmp;
			CurrentPreviewText = bmp;
		}

		/// <summary>
		/// Create preview image
		/// </summary>
		/// <returns></returns>
		public Image GetPreviewImage()
        {
            Image frame = new Bitmap((int)Width, (int)Height);
            Graphics gr = Graphics.FromImage(frame);
            if (CurrentPreviewImage != null)
            {
                gr.DrawImage(CurrentPreviewImage, new Rectangle(0, 0, frame.Width, frame.Height), new Rectangle(0, 0, frame.Width, frame.Height), GraphicsUnit.Pixel);
            }
            if (CurrentPreviewText != null)
            {
                gr.DrawImage(CurrentPreviewText, new Rectangle(0, 0, frame.Width, frame.Height), new Rectangle(0, 0, frame.Width, frame.Height), GraphicsUnit.Pixel);
            }
            return frame;
        }

        //public new void Dispose()
        //{
        //    //((ProjectionControl)(projectionControlHost.Child)).Dispose();
        //    base.Dispose();
        //}

		/// <summary>
		/// Set a new image
		/// </summary>
		/// <param name="img">Image that will be shown</param>
		/// <param name="fadeTime">Animation time in miliseconds</param>
		void SetProjectionImage(Bitmap img, int fadeTime)
		{
			if (fadeTime > 0)
			{
				_projectionImage.Opacity = 0f;
				_projectionImage.Image = img.ToXwtImage();
				int i = 0;
				int frames = AnimationFps * fadeTime / 1000;
				Xwt.Application.TimeoutInvoke(AnimationTpf, () =>
				{
					i++;
					_projectionImage.Opacity = (float)i / frames;
					if (i >= frames)
						_projectionImageBack.Image = _projectionImage.Image;
					return i < frames;
				});
			}
			else
			{
				_projectionImage.Image = img.ToXwtImage();
				_projectionImageBack.Image = _projectionImage.Image;
			}
		}

		void HideProjectionImage(int fadeTime)
		{
			_projectionImageBack.Image = null;
			if (fadeTime > 0)
			{
				int i = 0;
				int frames = AnimationFps * fadeTime / 1000;
				Xwt.Application.TimeoutInvoke(AnimationTpf, () =>
				{
					i++;
					_projectionImage.Opacity = 1f - (float)i / frames;
					if (i >= frames)
						_projectionImage.Image = null;
					return i < frames;
				});
			}
			else
			{
				_projectionImage.Opacity = 0f;
				_projectionImage.Image = null;
			}
		}

		void SetProjectionText(Bitmap img, int fadeTime)
		{
			//if (webBrowser.Source != null)
			//{
			//	HideWebsite();
			//}

			if (fadeTime > 0)
			{
				_textImage.Opacity = 0f;
				_textImage.Image = img.ToXwtImage();

				int i = 0;
				int frames = AnimationFps * fadeTime / 1000;
				Xwt.Application.TimeoutInvoke(AnimationTpf, () =>
				{
					i++;
					_textImage.Opacity = (float)i / frames;
					_textImageBack.Opacity = 1f - _textImage.Opacity;
					if (i >= frames)
					{
						_textImageBack.Image = _textImage.Image;
						_textImageBack.Opacity = 1f;
					}
					return i < frames;
				});
			}
			else
			{
				_textImage.Image = img.ToXwtImage();
				_textImageBack.Image = _textImage.Image;
				_textImageBack.Opacity = 1f;
			}
		}

		/// <summary>
		/// Toggle blackout mode
		/// </summary>
		/// <param name="enable">True if blackout should be enabled, else false</param>
		/// <param name="animationTime">Animation duration in miliseconds</param>
		void BlackOut(bool enable, int animationTime)
		{
			if (animationTime > 0)
			{
				int i = 0;
				int frames = AnimationFps * animationTime / 1000;
				if (enable)
				{
					Xwt.Application.TimeoutInvoke(AnimationTpf, () =>
					{
						i++;
						_blackoutImage.Opacity = (float)i / frames;
						return i < frames;
					});
				}
				else
				{
					Xwt.Application.TimeoutInvoke(AnimationTpf, () =>
					{
						i++;
						_blackoutImage.Opacity = 1f - (float)i / frames;
						return i < frames;
					});
				}
			}
			else
			{
				_blackoutImage.Opacity = enable ? 1f : 0f;
			}
		}
	}
}

///*
// *   PraiseBase Presenter
// *   The open source lyrics and image projection software for churches
// *
// *   http://praisebase.org
// *
// *   This program is free software; you can redistribute it and/or
// *   modify it under the terms of the GNU General Public License
// *   as published by the Free Software Foundation; either version 2
// *   of the License, or (at your option) any later version.
// *
// *   This program is distributed in the hope that it will be useful,
// *   but WITHOUT ANY WARRANTY; without even the implied warranty of
// *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// *   GNU General Public License for more details.
// *
// *   You should have received a copy of the GNU General Public License
// *   along with this program; if not, write to the Free Software
// *   Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// *
// */

//using System;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Interop;
//using System.Windows.Media;
//using System.Windows.Media.Animation;
//using System.Windows.Media.Imaging;

//namespace PraiseBase.Presenter.Projection
//{
//	/// <summary>
//	/// Interaction logic for UserControl1.xaml
//	/// </summary>
//	///
//	public partial class ProjectionControl : UserControl
//	{
//		#region Delegates

//		public delegate void AnimationFinish(object sender, EventArgs e);

//		#endregion Delegates

//		public ProjectionControl()
//		{
//			InitializeComponent();
//		}

//		public event AnimationFinish AnimationFinished;

//		private void ProjectionControlLoaded(object sender, RoutedEventArgs e)
//		{
//			webBrowser.LoadCompleted += WebBrowser_LoadCompleted;
//		}

//		/// <summary>
//		/// Load a bitmap and convert it to a WPF image source
//		/// </summary>
//		/// <param name="source"></param>
//		/// <returns></returns>
//		protected static BitmapSource LoadBitmap(System.Drawing.Bitmap source)
//		{
//			return Imaging.CreateBitmapSourceFromHBitmap(source.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
//		}

//		/// <summary>
//		/// Set a new image
//		/// </summary>
//		/// <param name="img">Image that will be shown</param>
//		/// <param name="fadeTime">Animation time in miliseconds</param>
//		public void SetProjectionImage(System.Drawing.Bitmap img, int fadeTime)
//		{
//			if (fadeTime > 0)
//			{
//				projectionImage.Opacity = 0f;
//				projectionImage.Source = LoadBitmap(img);

//				var imageAnimation = (Storyboard)FindResource("imageAnimation");
//				imageAnimation.SpeedRatio = 1000f / fadeTime;
//				imageAnimation.Begin(this);
//			}
//			else
//			{
//				projectionImage.Source = LoadBitmap(img);
//				projectionImageBack.Source = projectionImage.Source;
//			}
//		}

//		/// <summary>
//		/// Fired when the animation is finished
//		/// </summary>
//		/// <param name="sender"></param>
//		/// <param name="e"></param>
//		private void ImageAnimationCompleted(object sender, EventArgs e)
//		{
//			projectionImageBack.Source = projectionImage.Source;
//			AnimationFinished?.Invoke(this, new EventArgs());
//		}

//		public void HideProjectionImage(int fadeTime)
//		{
//			projectionImageBack.Source = null;
//			if (fadeTime > 0)
//			{
//				var imageAnimation = (Storyboard)FindResource("imageHideAnimation");
//				imageAnimation.SpeedRatio = 1000f / fadeTime;
//				imageAnimation.Begin(this);
//			}
//			else
//			{
//				projectionImage.Opacity = 0f;
//				projectionImage.Source = null;
//			}
//		}

//		/// <summary>
//		/// Fired when the animation is finished
//		/// </summary>
//		/// <param name="sender"></param>
//		/// <param name="e"></param>
//		private void ImageHideAnimationCompleted(object sender, EventArgs e)
//		{
//			projectionImage.Source = null;
//			AnimationFinished?.Invoke(this, new EventArgs());
//		}

//		public void SetProjectionText(System.Drawing.Bitmap img, int fadeTime)
//		{
//			if (webBrowser.Source != null)
//			{
//				HideWebsite();
//			}

//			if (fadeTime > 0)
//			{
//				textImage.Opacity = 0f;
//				textImage.Source = LoadBitmap(img);

//				var imageAnimation2 = (Storyboard)FindResource("textAnimation2");
//				imageAnimation2.SpeedRatio = 1000f / fadeTime;
//				imageAnimation2.Begin(this);

//				var imageAnimation = (Storyboard)FindResource("textAnimation");
//				imageAnimation.SpeedRatio = 1000f / fadeTime;
//				imageAnimation.Begin(this);
//			}
//			else
//			{
//				textImage.Source = LoadBitmap(img);
//				textImageBack.Source = textImage.Source;
//				textImageBack.Opacity = 1f;
//			}
//		}

//		internal void Dispose()
//		{
//			webBrowser.Dispose();
//		}

//		private void TextAnimationCompleted(object sender, EventArgs e)
//		{
//			textImageBack.Source = textImage.Source;
//			textImageBack.Opacity = 1f;
//			AnimationFinished?.Invoke(this, new EventArgs());
//		}

//		public void ShowWebsite(Uri uri)
//		{
//			webBrowser.Source = uri;
//		}

//		public void HideWebsite()
//		{
//			webBrowser.Visibility = Visibility.Collapsed;
//			webBrowser.Source = null;
//		}

//		private void WebBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
//		{
//			if (e.Uri != null)
//			{
//				webBrowser.Visibility = Visibility.Visible;
//			}
//		}

//		/// <summary>
//		/// Toggle blackout mode
//		/// </summary>
//		/// <param name="val">True if blackout should be enabled, else false</param>
//		/// <param name="animationTime">Animation duration in miliseconds</param>
//		public void BlackOut(bool val, int animationTime)
//		{
//			Storyboard blackoutAnimation = (Storyboard)FindResource(val ? "blackoutAnimationOn" : "blackoutAnimationOff");
//			blackoutAnimation.SpeedRatio = (animationTime == 0 ? 100 : 1000f / animationTime);
//			blackoutAnimation.Begin(this);
//		}

//		//public void SetText(List<TextBlock> textBlocks)
//		//{
//		//    TextBlock bl = textBlocks[0];
//		//    String str = string.Empty;
//		//    for (int i = 0; i < bl.Lines.Count; i++)
//		//    {
//		//        str += bl.Lines[i].Text + "\n";
//		//        ;
//		//    }
//		//    //textBlock1.Text = str;
//		//    //textBlock1.Width = this.Width - (2 * textBlock1.Margin.Left);
//		//    //textBlock1.Height = this.Height - (2 * textBlock1.Margin.Top);
//		//}

//		/*
//        TextContent.Text = "aasdfasdfasdf\naasdfasdfasdf\naasdfasdfasdf\naasdfasdfasdf\naasdfasdfasdf\n";
//        TextContent.Width = this.Width - (2 * TextContent.Margin.Left);
//        TextContent.Height = this.Height - (2 * TextContent.Margin.Top);
//        */
//		/*
//                    textBlock1.Text = "Animation: "+fadeTime+" ms";
//                    textBlock1.Visibility = System.Windows.Visibility.Visible;
//                    textBlock1.Width = this.Width - (2 * textBlock1.Margin.Left);
//                    textBlock1.Height = this.Height - (2 * textBlock1.Margin.Top);
//                    */
//	}
//}