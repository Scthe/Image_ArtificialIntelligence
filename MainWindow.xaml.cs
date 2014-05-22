using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AI_4.model;
using System.Windows.Shapes;
using System.Windows.Media;

namespace AI_4 {

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		/*
		 canvas contains children - remove children of type lines etc.
		 */

		private enum IMAGE_PANEL { IP_LEFT, IP_RIGHT }

		private readonly string dataDir;
		private model.ImageData dataLeft;
		private model.ImageData dataRight;
		private BitmapImage imgLeft; // TODO use Image.SourceRect ?
		private BitmapImage imgRight;

		public MainWindow() {
			InitializeComponent();
			var a = Directory.GetCurrentDirectory();
			dataDir = a.Substring(0, a.LastIndexOf("bin")) + "data\\";
			Console.WriteLine("[Info] DataDir '" + dataDir + "'");
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
			this.Left = 0;// desktopWorkingArea.Right - this.Width;
			this.Top = desktopWorkingArea.Bottom - this.Height;

			//drawImage("sintel_render.png", Image2);
			setImage("sintel_render.png", IMAGE_PANEL.IP_LEFT);
		}

		private void setImage(string imgName, IMAGE_PANEL target) {
			//http://stackoverflow.com/questions/12866758/placing-bitmap-in-canvas-in-c-sharp

			Image img = target == IMAGE_PANEL.IP_LEFT ? Image1 : Image2;
			var path = dataDir + imgName;
			if (File.Exists(path)) {
				Console.WriteLine("[Info] " + target.ToString() + " > '" + imgName + "'");
				BitmapImage bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.UriSource = new Uri(path);
				bitmap.EndInit();
				img.Source = bitmap;

				if (target == IMAGE_PANEL.IP_LEFT) imgLeft = bitmap;
				else imgRight = bitmap;


				loadImgData(path, target);
			} else {
				Console.WriteLine("[Error] Could not find '" + imgName + "'\n\tSearched in: '" + path + "'");
			}
		}

		// controller
		private void loadImgData(string fullImagePath, IMAGE_PANEL target) {
			var path = fullImagePath + ".haraff.sift";
			if (File.Exists(path)) {
				try {
					SiftLoader l = new SiftLoader();
					var imgData = l.load(path);

					if (target == IMAGE_PANEL.IP_LEFT) dataLeft = imgData;
					else dataRight = imgData;

					showKeypoints(target);
					Console.WriteLine("[Info] HARAFF SIFT parse success");
				} catch (Exception e) {
					Console.WriteLine("[Error] Loading HARAFF SIFT error: " + e.Message + "\n\tFile: '" + path + "'");
				}

			} else {
				Console.WriteLine("[Error] Could not find HARAFF SIFT \n\tSearched in: '" + path + "'");
			}
		}

		/// <summary>
		/// Getting Image view size does not tell us about real image size.
		/// f.e. with HorizontalAlignment: left the right side of the Image view could be empty
		/// </summary>
		private static void getRealImageSize(Image imgView, BitmapImage img, out double outW, out double outH) {
			double w = imgView.Width;
			double h = imgView.Height;
			double imgW = img.Width;
			double imgH = img.Height;
			double aspectView = w / h; // ~ 1
			double aspectImg = imgW / imgH;
			if (aspectImg <= aspectView) {
				// the bitmap image is more 'higher' then the image view
				// f.e. image view by default is square-shaped
				// and this bitmap image if really vertical in its dimensions
				Console.WriteLine("[Warning] calculations for image size for currnet combination of dimensions not supported");
				outH = h;
				outW = imgH * aspectView;
			} else {
				outW = w;
				outH = w / aspectImg;
			}
		}

		private void showKeypoints(IMAGE_PANEL target) {
			var data = target == IMAGE_PANEL.IP_LEFT ? dataLeft : dataRight;
			var img = target == IMAGE_PANEL.IP_LEFT ? imgLeft : imgRight;
			if (data == null || img == null)
				throw new InvalidOperationException("Tried to show keypoints for " + target.ToString() + ", while either image keypoint data was null or could not find BitmapImage object to read dimansions from");

			// read image view values
			Image imgView = target == IMAGE_PANEL.IP_LEFT ? Image1 : Image2;
			double w, h;
			getRealImageSize(imgView, img, out w, out h);
			var baseX = (double)imgView.GetValue(Canvas.LeftProperty);
			var baseY = (double)imgView.GetValue(Canvas.TopProperty); // should be 0 anyway
			Console.WriteLine("[Debug] View " + target.ToString() + String.Format(" x:{0}; y:{1}; w:{2}; h:{3}", baseX, baseY, w, h));

			// read image dimensions
			double imgW = img.Width;
			double imgH = img.Height;
			Console.WriteLine("[Debug] Bitmap " + target.ToString() + String.Format(" w:{0}; h:{1}", imgW, imgH));

			// scale factors
			double scaleX = w / imgW;
			double scaleY = h / imgH;

			// draw
			const double radius = 1;
			foreach (var keypoint in data.Keypoints) {
				if (keypoint.X > imgW || keypoint.Y > imgH)
					Console.WriteLine("[Error] Keypoint position error");

				double xx = keypoint.X * scaleX + baseX;
				double yy = keypoint.Y * scaleY + baseY;
				if (xx > w || yy > h)
					Console.WriteLine("[Error] Ellipse position error");

				Ellipse e = new Ellipse();
				e.SetValue(Canvas.LeftProperty, xx - radius);
				e.SetValue(Canvas.TopProperty, yy - radius);
				e.Width = radius * 2;
				e.Height = radius * 2;
				e.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
				DrawCanvas.Children.Add(e);
			}
		}


	}

}
