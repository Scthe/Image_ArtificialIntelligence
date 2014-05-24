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
using System.Threading;

namespace AI_4 {

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

		readonly public static NumberStyles numberStyles = NumberStyles.Number;
		readonly public static CultureInfo cultureInfo = CultureInfo.InvariantCulture;
		readonly private static SolidColorBrush greenBrush = new SolidColorBrush(
			(Color)ColorConverter.ConvertFromString("#FF0FD433")); // #FF0FD433
		readonly private static SolidColorBrush redBrush = new SolidColorBrush(
			(Color)ColorConverter.ConvertFromString("#FFD4330F"));

		private enum IMAGE_PANEL { IP_LEFT, IP_RIGHT }

		private CancellationTokenSource cts;

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
		private A1_NeighbourhoodCompactnessAnalysis a1_matcher = new A1_NeighbourhoodCompactnessAnalysis();
		private List<Tuple<int, int>> neighbourhoodCompactnessPairs;


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

			N.Text = a1_matcher.N.ToString();
			MinPercentage.Value = a1_matcher.RequiredMinPercentage;

			SetImage("sintel_render.png", IMAGE_PANEL.IP_LEFT);
		}

		private void SetImage(string imgName, IMAGE_PANEL target) {
			//http://stackoverflow.com/questions/12866758/placing-bitmap-in-canvas-in-c-sharp

			RemoveKeypoints(target);
			RemoveNeighbours();

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

		private async Task LoadImgData(string fullImagePath, IMAGE_PANEL target) {
			// http://blog.stephencleary.com/2012/02/async-and-await.html

			State.Fill = redBrush;

			if (cts != null) {
				cts.Cancel();
			}
			cts = new CancellationTokenSource();
			try {
				pairs = null;
				if (target == IMAGE_PANEL.IP_LEFT) dataLeft = null;
				else dataRight = null;

				var path = fullImagePath + ".haraff.sift";
				bool ok = false;
				ImageData imgData = null;
				if (File.Exists(path)) {
					try {
						SiftLoader l = new SiftLoader();
						await Task.Run(() => l.load(path));
						imgData = l.Result;
						ok = true;
						Console.WriteLine("[Info] HARAFF SIFT parse success: " + imgData.Keypoints.Count + " keypoints");
					} catch (Exception e) {
						Console.WriteLine("[Error] Loading HARAFF SIFT error: " + e.Message + "\n\tFile: '" + path + "'");
					}

				} else {
					Console.WriteLine("[Error] Could not find HARAFF SIFT \n\tSearched in: '" + path + "'");
				}

				// TODO at this point we could allow to display keypoints
				cts.Token.ThrowIfCancellationRequested();

				// if everything went ok - update neighbours
				if (ok) {
					if (target == IMAGE_PANEL.IP_LEFT) dataLeft = imgData;
					else dataRight = imgData;

					await Task.Run(() => ReloadPairs(cts));
				}
			} catch (OperationCanceledException) {
			}
			cts = null;

			State.Fill = greenBrush;
		}

		private void ReloadPairs(CancellationTokenSource cts) {
			if (dataLeft != null && dataRight != null) {
				var matcher = new PairsMatcher();
				try {
					pairs = matcher.match(dataLeft, dataRight, cts);
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
		private static void GetRealImageSize(Image imgView, BitmapImage img, out double outW, out double outH) {
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
		private void GetDrawTransformation(IMAGE_PANEL target,
			out double outBaseX, out double outBaseY,
			out double outScaleX, out double outScaleY) {

			var data = target == IMAGE_PANEL.IP_LEFT ? dataLeft : dataRight;
			var img = target == IMAGE_PANEL.IP_LEFT ? imgLeft : imgRight;
			if (data == null || img == null)
				throw new InvalidOperationException("Tried to get transformation for " + target.ToString() + ", while either image keypoint data was null or could not find BitmapImage object to read dimansions from");

			// read image view values
			Image imgView = target == IMAGE_PANEL.IP_LEFT ? Image1 : Image2;
			double w, h;
			GetRealImageSize(imgView, img, out w, out h);
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

		private void ShowKeypoints(IMAGE_PANEL target) {
			var data = target == IMAGE_PANEL.IP_LEFT ? dataLeft : dataRight;
			if (cts != null || data == null) {
				Console.WriteLine("[Error] Could not display keypoints - data not valid / running async");
				return;
			}
			double baseX, baseY;
			double scaleX, scaleY;
			GetDrawTransformation(target, out baseX, out baseY, out scaleX, out scaleY);

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

		private void ShowNeighbourConnectionLines(List<Tuple<int, int>> pairs) {
			if (cts != null || pairs == null) {
				Console.WriteLine("[Error] Could not display neighbours - list is not valid / running async");
				return;
			}

			//Console.WriteLine("[Debug] Displaying " + pairs.Count + " pairs");
			var kp1 = dataLeft.Keypoints;
			var kp2 = dataRight.Keypoints;

			// get transformation set
			double baseX_1, baseY_1, baseX_2, baseY_2;
			double scaleX_1, scaleY_1, scaleX_2, scaleY_2;
			GetDrawTransformation(IMAGE_PANEL.IP_LEFT, out baseX_1, out baseY_1, out scaleX_1, out scaleY_1);
			GetDrawTransformation(IMAGE_PANEL.IP_RIGHT, out baseX_2, out baseY_2, out scaleX_2, out scaleY_2);

			foreach (var idPair in pairs) {
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

		private void RemoveNeighbours() {
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

		private void RemoveKeypoints(IMAGE_PANEL target) {
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
				ShowKeypoints(target);
			} else {
				// remove all keypoints
				RemoveKeypoints(target);
			}
		}

		private void Neighbours_Changed(object sender, RoutedEventArgs e) {
			var chBox = sender as CheckBox;
			if (chBox.IsChecked ?? false) {
				ShowNeighbourConnectionLines(pairs);
			} else {
				// remove lines
				RemoveNeighbours();
			}
		}

		private void UpdateClosenessFilter_Click(object sender, RoutedEventArgs e) {
			if (cts != null || dataLeft == null || dataRight == null) {
				Console.WriteLine("[Warning] Could not analize images, data for either image left or image right was not read");
				return;
			}

			// read parameters
			int Nval;
			if (!int.TryParse(N.Text, numberStyles, cultureInfo, out Nval)) {
				Console.WriteLine("[Error] Could not read N value '" + N.Text + "' as int");
				return;
			}
			double minPercentval = MinPercentage.Value;
			RemoveNeighbours();

			// run
			a1_matcher.N = Nval;
			a1_matcher.RequiredMinPercentage = (float)minPercentval;
			Console.WriteLine(String.Format("[Info] Closeness filter {{ N={0}; MinPercent={1} }}", Nval, minPercentval));

			try {
				neighbourhoodCompactnessPairs = a1_matcher.reduce(pairs, dataLeft, dataRight);
				ShowNeighbourConnectionLines(neighbourhoodCompactnessPairs);
				Console.WriteLine("[Info] Closeness filter found " + neighbourhoodCompactnessPairs.Count + "  / " + pairs.Count + "pairs");
			} catch (Exception ee) {
				Console.WriteLine("[Error] Closeness filter error: " + ee.Message);
			}
		}

		private void RANSAC_Filter_Click(object sender, RoutedEventArgs e) {
			if (dataLeft == null || dataRight == null) {
				Console.WriteLine("[Warning] Could not analize images, data for either image left or image right was not read");
				return;
			}
			//var ransac = new A2_RANSAC();
			//var ransacMatrix = ransac.reduce(pairs, dataLeft, dataRight);
			//displayRansacResult(ransacMatrix);
			Console.WriteLine("[Info] RANSAC");
		}

		#endregion

		private void textBox_NumbersOnly_i(object sender, System.Windows.Input.TextCompositionEventArgs e) {
			if (!Char.IsDigit(e.Text[0]))
				e.Handled = true;
		}
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
