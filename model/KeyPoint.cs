using System;
using System.Collections.Generic;
using System.Linq;

namespace AI_4.model {

	#region KeyPoint

	public class KeyPoint {
		public const int TRAITS_COUNT = 128;

		public int ID { get; private set; }
		public float X { get; private set; }
		public float Y { get; private set; }
		private float[] traitVals = new float[TRAITS_COUNT];


		public float this[int key] {
			get { return traitVals[key]; }
		}

		public KeyPoint(int id, float x, float y, float[] traitVals) {
			if (traitVals.Length != KeyPoint.TRAITS_COUNT)
				throw new ArgumentException("Invalid # of traits in array: provided " + traitVals.Length + " should have been " + TRAITS_COUNT);
			this.ID = id;
			this.X = x; this.Y = y;
			this.traitVals = traitVals;
		}
	}
	#endregion


	#region TraitsLookupMatrix

	public class TraitsLookupMatrix {

		private List<Tuple<float, int>>[] data;
		private bool alreadyInitialized = false;
		private int countOfKeyPoints;


		public TraitsLookupMatrix(int countOfKeyPoints) {
			data = new List<Tuple<float, int>>[KeyPoint.TRAITS_COUNT];
			for (int i = 0; i < KeyPoint.TRAITS_COUNT; i++) {
				data[i] = new List<Tuple<float, int>>(countOfKeyPoints);
			}
			this.countOfKeyPoints = countOfKeyPoints;
		}

		public void set(int keyPointID, int traitId, float traitVal) {
			if (alreadyInitialized)
				throw new InvalidOperationException("Tried to add after TraitsLookupMatrix was initialized ( closed for modification)");

			var arrForTrait = data[traitId];
			arrForTrait.Add(new Tuple<float, int>(traitVal, keyPointID));
		}

		/// <summary>
		/// Call to close matrix for modifications. It is neccessary !
		/// </summary>
		public void postInit() {
			if (alreadyInitialized) return;
			// sort
			for (int i = 0; i < KeyPoint.TRAITS_COUNT; i++) {
				List<Tuple<float, int>> arrForTrait = data[i];
				if (arrForTrait.Count != countOfKeyPoints)
					throw new RankException("List of keypoints for trait #" + i + " has length of " + arrForTrait.Count + " as opposed to " + this.countOfKeyPoints);

				arrForTrait.Sort(delegate(Tuple<float, int> t1, Tuple<float, int> t2) {
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
		public int findClosest(int traitId, float traitVal) {
			// binary search
			var arrForTrait = data[traitId];
			int pos = arrForTrait.BinarySearch(new Tuple<float, int>(traitVal, -1));
			if (pos < 0) pos = ~pos;
			return arrForTrait[pos].Item2;
		}
	}
	#endregion


	/*
//data = new List<Tuple<float, int>>[countOfKeyPoints];
//for (int i = 0; i < countOfKeyPoints; i++) {
//var a = new List<Tuple<float, int>>();
//a.Capacity = 128;
//data[i] = a;
//}

//data = new Tuple<float, int>[countOfKeyPoints][];
//for (int i = 0; i < countOfKeyPoints; i++) {
//data[i] = new Tuple<float, int>[128];
//}
	 
	 
	 
//private static int binarySearch(float searchVal, Tuple<float, int>[] arr, int start, int end) {
//return 0;
//}
	 */
}
