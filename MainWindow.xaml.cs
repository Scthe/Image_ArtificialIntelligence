using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AI_4.model;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AI_4 {

	// TODO add 'is-busy' indicator ? ( mark running async task)

	public enum IMAGES_ENUM {
		[Description(" ")]
		None,
		[Description("15after.png")]
		Sintel1,
		[Description("sintel_render.png")]
		Sintel2,
		[Description("TheRoad1.png")]
		TheRoad1,
		[Description("TheRoad2.png")]
		TheRoad2
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private enum IMAGE_PANEL { IP_LEFT, IP_RIGHT }

		public IEnumerable<ValueDescription> ImagesList {
			get {
				return EnumHelper.GetAllValuesAndDescriptions<IMAGES_ENUM>();
			}
		}
		private IMAGES_ENUM ComboBox_Left;
		private IMAGES_ENUM ComboBox_Right;

		private readonly string dataDir;
		private model.ImageData dataLeft;
		private model.ImageData dataRight;
		private BitmapImage imgLeft; // TODO use Image.SourceRect ?
		private BitmapImage imgRight;
		private List<Tuple<int, int>> pairs;

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

			SetImage("sintel_render.png", IMAGE_PANEL.IP_LEFT);
			//setImage("15after.png", IMAGE_PANEL.IP_RIGHT);
		}

		private void SetImage(string imgName, IMAGE_PANEL target) {
			//http://stackoverflow.com/questions/12866758/placing-bitmap-in-canvas-in-c-sharp

			removeKeypoints(target);
			removeNeighbours();

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

				LoadImgData(path, target);

			} else {
				Console.WriteLine("[Error] Could not find '" + imgName + "'\n\tSearched in: '" + path + "'");
			}
		}

		#region controller

		private async void LoadImgData(string fullImagePath, IMAGE_PANEL target) {
			// http://blog.stephencleary.com/2012/02/async-and-await.html

			if (target == IMAGE_PANEL.IP_LEFT) dataLeft = null;
			else dataRight = null;

			var path = fullImagePath + ".haraff.sift";
			bool ok = false;
			if (File.Exists(path)) {
				try {
					SiftLoader l = new SiftLoader();
					await Task.Run(() => l.load(path));
					ImageData imgData = l.Result;

					if (target == IMAGE_PANEL.IP_LEFT) dataLeft = imgData;
					else dataRight = imgData;

					ok = true;
					Console.WriteLine("[Info] HARAFF SIFT parse success: " + imgData.Keypoints.Count + " keypoints");
				} catch (Exception e) {
					Console.WriteLine("[Error] Loading HARAFF SIFT error: " + e.Message + "\n\tFile: '" + path + "'");
				}

			} else {
				Console.WriteLine("[Error] Could not find HARAFF SIFT \n\tSearched in: '" + path + "'");
			}

			// if everything went ok - update neighbours
			if (ok) {
				await Task.Run(() => ReloadPairs());
			}
		}

		private void ReloadPairs() {
			if (dataLeft != null && dataRight != null) {
				var matcher = new NeighbourPointsMatcher();
				try {
					pairs = matcher.match(dataLeft, dataRight);
					Console.WriteLine("[Info] Found " + pairs.Count + " pairs");
				} catch (Exception ex) {
					Console.WriteLine("[Error] NeighbourPointsMatcher: " + ex.Message);
				}
			}
		}

		#endregion


		#region display utils

		/// <summary>
		/// Getting Image view size does not tell us about real image size.
		/// f.e. with HorizontalAlignment: left the right side of the Image view could be empty
		/// </summary>
		private static void getRealImageSize(Image imgView, BitmapImage img, out double outW, out double outH) {
			double w = imgView.Width;
			double h = imgView.Height;
			double imgW = img.PixelWidth; // http://stackoverflow.com/questions/11571365/how-to-get-the-width-and-height-of-a-bitmapimage-in-metro
			double imgH = img.PixelHeight;
			double aspectView = w / h; // ~ 1
			double aspectImg = imgW / imgH;
			if (aspectImg <= aspectView) {
				// the bitmap image is more 'higher' then the image view
				// f.e. image view by default is square-shaped
				// and this bitmap image if really vertical in its dimensions
				outH = h;
				outW = h * aspectImg;
			} else {
				outW = w;
				outH = w / aspectImg;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void getDrawTransformation(IMAGE_PANEL target,
			out double outBaseX, out double outBaseY,
			out double outScaleX, out double outScaleY) {

			var data = target == IMAGE_PANEL.IP_LEFT ? dataLeft : dataRight;
			var img = target == IMAGE_PANEL.IP_LEFT ? imgLeft : imgRight;
			if (data == null || img == null)
				throw new InvalidOperationException("Tried to get transformation for " + target.ToString() + ", while either image keypoint data was null or could not find BitmapImage object to read dimansions from");

			// read image view values
			Image imgView = target == IMAGE_PANEL.IP_LEFT ? Image1 : Image2;
			double w, h;
			getRealImageSize(imgView, img, out w, out h);
			outBaseX = (double)imgView.GetValue(Canvas.LeftProperty);
			outBaseY = (double)imgView.GetValue(Canvas.TopProperty); // should be 0 anyway
			//Console.WriteLine("[Debug] View " + target.ToString() + String.Format(" x:{0}; y:{1}; w:{2}; h:{3}", outBaseX, outBaseY, w, h));

			// read image dimensions
			double imgW = img.PixelWidth;
			double imgH = img.PixelHeight;
			//Console.WriteLine("[Debug] Bitmap " + target.ToString() + String.Format(" w:{0}; h:{1}", imgW, imgH));

			// scale factors
			outScaleX = w / imgW;
			outScaleY = h / imgH;
		}

		#endregion


		#region display data on the images

		private void showKeypoints(IMAGE_PANEL target) {
			var data = target == IMAGE_PANEL.IP_LEFT ? dataLeft : dataRight;

			double baseX, baseY;
			double scaleX, scaleY;
			getDrawTransformation(target, out baseX, out baseY, out scaleX, out scaleY);

			// draw
			const double radius = 1;
			foreach (var keypoint in data.Keypoints) {
				//if (keypoint.X > imgW || keypoint.Y > imgH)
				//Console.WriteLine("[Error] Keypoint position error");

				double xx = keypoint.X * scaleX + baseX;
				double yy = keypoint.Y * scaleY + baseY;
				//if (xx > w + baseX || yy > h + baseY)
				//Console.WriteLine("[Error] Ellipse position error");

				Ellipse e = new Ellipse() { Uid = "kp" + target.ToString() };
				e.SetValue(Canvas.LeftProperty, xx - radius);
				e.SetValue(Canvas.TopProperty, yy - radius);
				e.Width = radius * 2;
				e.Height = radius * 2;
				e.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
				DrawCanvas.Children.Add(e);
			}
		}

		private void showNeighbourConnectionLines() {
			if (pairs == null)
				Console.WriteLine("[Error] Could not display neighbours - list is not valid");

			Console.WriteLine("[Debug] Displaying " + pairs.Count + " pairs");
			var kp1 = dataLeft.Keypoints;
			var kp2 = dataRight.Keypoints;

			// get transformation set
			double baseX_1, baseY_1, baseX_2, baseY_2;
			double scaleX_1, scaleY_1, scaleX_2, scaleY_2;
			getDrawTransformation(IMAGE_PANEL.IP_LEFT, out baseX_1, out baseY_1, out scaleX_1, out scaleY_1);
			getDrawTransformation(IMAGE_PANEL.IP_RIGHT, out baseX_2, out baseY_2, out scaleX_2, out scaleY_2);

			foreach (var idPair in pairs) {
				//for (int i = 0; i < 5; i++) {
				//var idPair = pairs[i];

				var idA = idPair.Item1;
				var idB = idPair.Item2;
				var kpA = kp1[idA];
				var kpB = kp2[idB];

				var line = new Line() { Uid = "Line" + idA };
				line.Stroke = new SolidColorBrush(Color.FromArgb(32, 255, 255, 0));
				line.X1 = kpA.X * scaleX_1 + baseX_1;
				line.Y1 = kpA.Y * scaleY_1 + baseY_1;
				line.X2 = kpB.X * scaleX_2 + baseX_2;
				line.Y2 = kpB.Y * scaleY_2 + baseY_2;
				line.StrokeThickness = 1;
				DrawCanvas.Children.Add(line);
			}
		}

		private void removeNeighbours() {
			List<UIElement> itemstoremove = new List<UIElement>();
			foreach (UIElement ui in DrawCanvas.Children) {
				if (ui.Uid.StartsWith("Line")) {
					itemstoremove.Add(ui);
				}
			}
			foreach (UIElement ui in itemstoremove) {
				DrawCanvas.Children.Remove(ui);
			}
		}

		private void removeKeypoints(IMAGE_PANEL target) {
			List<UIElement> itemstoremove = new List<UIElement>();
			foreach (UIElement ui in DrawCanvas.Children) {
				if (ui.Uid.StartsWith("kp") && ui.Uid.EndsWith(target.ToString())) {
					itemstoremove.Add(ui);
				}
			}
			foreach (UIElement ui in itemstoremove) {
				DrawCanvas.Children.Remove(ui);
			}
		}

		#endregion display data on the images


		#region listeners

		public IMAGES_ENUM ComboBoxLeftSelected {
			get { return ComboBox_Left; }
			set {
				if (ComboBox_Left != value) {
					ComboBox_Left = value;
					//OnPropertyChanged("SelectedClass");
					//OnPropertyChanged();
					Console.WriteLine(IMAGE_PANEL.IP_LEFT.ToString() + " > " + value.ToString());
					var path = EnumHelper.Description(value);
					SetImage(path, IMAGE_PANEL.IP_LEFT);
				}
			}
		}
		public IMAGES_ENUM ComboBoxRightSelected {
			get { return ComboBox_Right; }
			set {
				if (ComboBox_Right != value) {
					ComboBox_Right = value;
					//OnPropertyChanged("SelectedClass");
					//OnPropertyChanged();
					Console.WriteLine(IMAGE_PANEL.IP_RIGHT.ToString() + " > " + value.ToString());
					var path = EnumHelper.Description(value);
					SetImage(path, IMAGE_PANEL.IP_RIGHT);
				}
			}
		}

		private void Keypoints_Changed(object sender, RoutedEventArgs e) {
			var chBox = sender as CheckBox;
			var target = chBox.Name.Equals(KeypointsLeft.Name) ? IMAGE_PANEL.IP_LEFT : IMAGE_PANEL.IP_RIGHT;

			if (chBox.IsChecked ?? false) {
				showKeypoints(target);
			} else {
				// remove all keypoints
				removeKeypoints(target);
			}
		}

		private void Neighbours_Changed(object sender, RoutedEventArgs e) {
			var chBox = sender as CheckBox;
			if (chBox.IsChecked ?? false) {
				showNeighbourConnectionLines();
			} else {
				// remove lines
				removeNeighbours();
			}
		}


		/* if (dataLeft != null && dataRight != null) {
				var matcher = new NeighbourPointsMatcher();
				var ai1 = new A1_NeighbourhoodCompactnessAnalysis();
				var ransac = new A2_RANSAC();

				try {
					pairs = matcher.match(dataLeft, dataRight);
					Console.WriteLine("[Info] Found " + pairs.Count + " pairs");
					//showNeighbourConnectionLines(pairs);

					//var pairs2 =ai1.reduce(pairs, dataLeft, dataRight);
					//Console.WriteLine("[Info] Reduced to " + pairs2.Count + " pairs");
					//showNeighbourConnectionLines(pairs2);

					//var ransacMatrix = ransac.reduce(pairs, dataLeft, dataRight);
					//displayRansacResult(ransacMatrix);


				} catch (Exception ex) {
					Console.WriteLine("[Error] NeighbourPointsMatcher: " + ex.Message);
				}
			} else {
				Console.WriteLine("[Error] Could not analize images, data for either image left or image right was not read");
			}*/
		private void UpdateClosenessFilter_Click(object sender, RoutedEventArgs e) {
			if (dataLeft == null || dataRight == null) {
				Console.WriteLine("[Warning] Could not analize images, data for either image left or image right was not read");
				return;
			}

			// read parameters


			Console.WriteLine(String.Format("[Info] Close"));
		}

		private void RANSAC_Filter_Click(object sender, RoutedEventArgs e) {
			if (dataLeft == null || dataRight == null) {
				Console.WriteLine("[Warning] Could not analize images, data for either image left or image right was not read");
				return;
			}
			Console.WriteLine(String.Format("[Info] RANSAC"));
		}

		#endregion

	}


	#region utils other

	public class HalfValueConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return ((double)value) / 2 - 1;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return (((double)value) + 1) * 2;
		}
	}

	public class HalfValuePlus2Converter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return ((double)value) / 2 + 1;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return (((double)value) + 1) + 1;
		}
	}


	public class ValueDescription {
		//public MainWindow.IMAGES_ENUM Value { get; set; }
		public System.Enum Value { get; set; }
		public string Description { get; set; }
	}

	public static class EnumHelper {
		// http://stackoverflow.com/questions/6145888/how-to-bind-an-enum-to-a-combobox-control-in-wpf

		/// <summary>
		/// Gets the description of a specific enum value.
		/// </summary>
		public static string Description(this Enum eValue) {
			var nAttributes = eValue.GetType().GetField(eValue.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);

			if (!nAttributes.Any()) {
				TextInfo oTI = CultureInfo.CurrentCulture.TextInfo;
				return oTI.ToTitleCase(oTI.ToLower(eValue.ToString().Replace("_", " ")));
			}

			return (nAttributes.First() as DescriptionAttribute).Description;
		}

		/// <summary>
		/// Returns an enumerable collection of all values and descriptions for an enum type.
		/// </summary>
		public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions<TEnum>() where TEnum : struct, IConvertible, IComparable, IFormattable {
			if (!typeof(TEnum).IsEnum)
				throw new ArgumentException("TEnum must be an Enumeration type");

			return from e in Enum.GetValues(typeof(TEnum)).Cast<Enum>()
				   select new ValueDescription() { Value = e, Description = e.Description() };
		}
	}

	#endregion
}
