using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AI_4 {

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		/*
		 canvas contains children - remove children of type lines etc.
		 */

		private readonly string dataDir;

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
			//drawImage("6.jpg", Image2);
		}


		private void drawImage(string imgName, Image target) {
			//http://stackoverflow.com/questions/12866758/placing-bitmap-in-canvas-in-c-sharp
			var path = dataDir + imgName;
			if (File.Exists(path)) {
				//Console.WriteLine("found '" + Directory.GetCurrentDirectory() + "'");
				BitmapImage bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.UriSource = new Uri(path);
				bitmap.EndInit();
				target.Source = bitmap;
			} else {
				Console.WriteLine("[Error] Could not find '" + imgName + "'\nSearched in: '" + path + "'");
			}


		}

	}



}
