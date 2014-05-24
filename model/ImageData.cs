using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AI_4.model {

	#region KeyPoint

	public class KeyPoint {
		public const int TRAITS_COUNT = 128;

		public int ID { get; private set; }
		public float X { get; private set; }
		public float Y { get; private set; }
		private int[] traitVals = new int[TRAITS_COUNT];


		public int this[int key] {
			get { return traitVals[key]; }
		}

		public KeyPoint(int id, float x, float y, int[] traitVals) {
			if (traitVals.Length != KeyPoint.TRAITS_COUNT)
				throw new ArgumentException("Invalid # of traits in array: provided " + traitVals.Length + " should have been " + TRAITS_COUNT);
			this.ID = id;
			this.X = x; this.Y = y;
			this.traitVals = traitVals;
		}
	}
	#endregion

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

	#region Builder

	class KeyPointDataBuilder {

		readonly public static NumberStyles numberStyles = NumberStyles.Number;
		readonly public static CultureInfo cultureInfo = CultureInfo.InvariantCulture;

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
			float x = float.Parse(tokens[0], numberStyles, cultureInfo);
			float y = float.Parse(tokens[1], numberStyles, cultureInfo);
			int[] data = new int[KeyPoint.TRAITS_COUNT];
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

	#region TraitsLookupMatrix

	public class TraitsLookupMatrix {

		private List<Tuple<int, int>>[] data;
		private bool alreadyInitialized = false;
		private int countOfKeyPoints;


		public TraitsLookupMatrix(int countOfKeyPoints) {
			data = new List<Tuple<int, int>>[KeyPoint.TRAITS_COUNT];
			for (int i = 0; i < KeyPoint.TRAITS_COUNT; i++) {
				data[i] = new List<Tuple<int, int>>(countOfKeyPoints);
			}
			this.countOfKeyPoints = countOfKeyPoints;
		}

		public void set(int keyPointID, int traitId, int traitVal) {
			if (alreadyInitialized)
				throw new InvalidOperationException("Tried to add after TraitsLookupMatrix was initialized ( closed for modification)");

			var arrForTrait = data[traitId];
			arrForTrait.Add(new Tuple<int, int>(traitVal, keyPointID));
		}

		/// <summary>
		/// Call to close matrix for modifications. It is neccessary !
		/// </summary>
		public void postInit() {
			if (alreadyInitialized) return;
			// sort
			for (int i = 0; i < KeyPoint.TRAITS_COUNT; i++) {
				List<Tuple<int, int>> arrForTrait = data[i];
				if (arrForTrait.Count != countOfKeyPoints)
					throw new RankException("List of keypoints for trait #" + i + " has length of " + arrForTrait.Count + " as opposed to " + this.countOfKeyPoints);

				arrForTrait.Sort(delegate(Tuple<int, int> t1, Tuple<int, int> t2) {
					return t1.Item1.CompareTo(t2.Item1); // sort by trait value
				});

				var ordered = arrForTrait.Zip(arrForTrait.Skip(1), (a, b) => new { a, b })
					.All(p => p.a.Item1 <= p.b.Item1);
				if (!ordered)
					throw new SystemException("Lists should be ordered by now. < invalid state >");
			}
			alreadyInitialized = true;
		}

		/// <summary>
		/// Returns id of the KeyPoint that has the closest value for provided trait
		/// </summary>
		/// <returns>int as id</returns>
		public void findClosest(int traitId, int traitVal, List<int> target) {
			target.Clear();

			// binary search
			var arrForTrait = data[traitId];
			int pos = arrForTrait.BinarySearch(new Tuple<int, int>(traitVal, -1));
			var posOk = pos < 0 ? ~pos : pos;
			posOk = Math.Min(posOk, arrForTrait.Count - 1);

			// binary search returns single value, while we are interesed in range
			// ( usually multiple keypoints have the same value)
			int start = posOk;
			while (start > 0 && arrForTrait[start].Item1 == traitVal) {
				start -= 50; // 5 is just to speed up the look up
			}
			var it = Math.Max(0, start);
			while (it < arrForTrait.Count && arrForTrait[it].Item1 <= traitVal) {
				if (arrForTrait[it].Item1 == traitVal)
					target.Add(arrForTrait[it].Item2);
				++it;
			}
			//Console.WriteLine("Found: " + target.Count + " [ " + start + " :" + it + "]");
		}
	}
	#endregion
}
