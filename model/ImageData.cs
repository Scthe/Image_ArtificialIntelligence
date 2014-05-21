using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_4.model {

	public class ImageData {

		public string Name { get; private set; }
		public TraitsLookupMatrix LookupMatrix { get; private set; }
		public List<KeyPoint> Keypoints { get; private set; }

		internal ImageData(string name, TraitsLookupMatrix lookupMatrix, List<KeyPoint> keypoints) {
			Name = name;
			LookupMatrix = lookupMatrix;
			Keypoints = keypoints;
		}

	}

	#region TraitsLookupMatrix

	class KeyPointDataBuilder {

		private int nextId = 0;
		private string name;
		private TraitsLookupMatrix matrix;
		private List<KeyPoint> keypoints;
		private bool done = false;

		public KeyPointDataBuilder(string name, int countOfKeyPoints) {
			matrix = new TraitsLookupMatrix(countOfKeyPoints);
			keypoints = new List<KeyPoint>(countOfKeyPoints);
			this.name = name;
		}

		public void readKeyPoint(string s) {
			if (done)
				throw new InvalidOperationException("Called readKeyPoint() on builder that has already build the image data object");

			var tokens = s.Trim().Split(' ');
			if (tokens.Length != 133)
				throw new FormatException("Could not parse ( #tokens=" + tokens.Length + "): '" + s + "'");

			// parse
			float x = int.Parse(tokens[0]),
				y = int.Parse(tokens[1]);
			float[] data = new float[KeyPoint.TRAITS_COUNT];
			const int startPos = 5;
			for (int i = 0; i < KeyPoint.TRAITS_COUNT; i++) {
				data[i] = int.Parse(tokens[startPos + i]);
			}
			// assign data
			var kp = new KeyPoint(nextId, x, y, data);
			for (int i = 0; i < KeyPoint.TRAITS_COUNT; i++) {
				matrix.set(kp.ID, i, kp[i]);
			}
			keypoints.Add(kp);
			++nextId;
		}

		public ImageData build() {
			done = true;
			matrix.postInit();
			return new ImageData(name, matrix, keypoints);
		}
	}

	#endregion
}
