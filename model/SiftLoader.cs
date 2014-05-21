using System;

namespace AI_4.model {

	public class SiftLoader {

		public ImageData load(string fileName) {
			string[] lines = System.IO.File.ReadAllLines(fileName);
			if (lines.Length < 2) throw new FormatException("#lines < 2 ?! wtf ?");
			int kpsCount;
			bool ok = int.TryParse(lines[1], out kpsCount);
			if (!ok || lines.Length != kpsCount + 2) {
				throw new FormatException(
					string.Format("Detected %d lines in file, according to 2nd line, there should be ( %d +2)", lines.Length, kpsCount));
			}

			// parse
			var builder = new KeyPointDataBuilder(fileName, kpsCount);
			for (int i = 2; i < lines.Length; i++) {
				builder.readKeyPoint(lines[i]);
			}

			return builder.build();
		}
	}


	/*
KeyPoint[] kps = new KeyPoint[kpsCount];
foreach (string line in lines) {
Console.WriteLine("\t" + line);
}
return null;

	 
	public class KeyPoint {
		public int ID { get; private set; }
		public float X { get; private set; }
		public float Y { get; private set; }
		private float[] values = new float[128];

		//internal KeyPoint(int id, float x,float y) { }
	}

	class KeyPointFactory {
		private int nextId = 0;

		public KeyPoint readKeyPoint(string s) {
			KeyPoint res = null;
			return null;
		}
	}*/
}
