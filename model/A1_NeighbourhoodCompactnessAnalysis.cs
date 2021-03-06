﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AI_4.model {

	public class A1_NeighbourhoodCompactnessAnalysis {

		public float RequiredMinPercentage { set; get; }
		public int N { set; get; } // neighbourhood size

		public A1_NeighbourhoodCompactnessAnalysis() {
			RequiredMinPercentage = 10f;
			N = 40;
		}


		public List<Tuple<int, int>> reduce(List<Tuple<int, int>> pairs, ImageData img1, ImageData img2) {
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			var res = new Tuple<int, int>[pairs.Count];

			Parallel.For(0, pairs.Count, pair_i => {
				var ks1 = img1.Keypoints;
				var ks2 = img2.Keypoints;
				var pair = pairs[pair_i];

				var neighboursOf_1 = GetClosestToKeyPoint(pair.Item1, ks1);
				var neighboursOf_2 = GetClosestToKeyPoint(pair.Item2, ks2);
				int neighboursClose = 0;
				foreach (int idA in neighboursOf_1) {
					// get pair for this keyPoint ( it is not guaranteed that the closes point does in fact have a pair)
					Tuple<int, int> currentPair = pairs.FirstOrDefault((p) => p.Item1 == idA);
					if (currentPair != null) {
						int idB = currentPair.Item2;
						if (neighboursOf_2.Contains(idB)) {
							// match !
							++neighboursClose;
						}
					}
				}
				if (neighboursClose * 100.0f / N >= this.RequiredMinPercentage)
					res[pair_i] = pair;
			});

			// combine result
			var result = new List<Tuple<int, int>>(res.Length);
			for (int i = 0; i < res.Length; i++) {
				if (res[i] != null) result.Add(res[i]);
			}

			stopwatch.Stop();
			Console.WriteLine("\t[PROFILE]Fin NeighbourhoodCompactness: " + stopwatch.ElapsedMilliseconds);

			return result;
		}

		private IEnumerable<int> GetClosestToKeyPoint(int id, List<KeyPoint> ks) {
			// http://stackoverflow.com/questions/9113780/fast-algorithm-to-find-the-x-closest-points-to-a-given-point-on-a-plane
			KeyPoint k = ks[id];
			//KeyPoint k = ks.First((p) => p.ID == id);
			var closestPoints = ks.Where(point => point != k)
						   .OrderBy(point => Math.Pow(k.X - point.X, 2) + Math.Pow(k.Y - point.Y, 2))
						   .Take(N)
						   .Select(p => p.ID);
			return closestPoints;
		}

	}


	/*
	private bool isNeighbour() {
		return false;
	}
		
	/// <summary>
	/// Sort in respect to the left top corner
	/// </summary>
	private List<KeyPoint> createSortedCache(ImageData img) {
		var a = img.Keypoints;
		var res = new List<KeyPoint>(a);
		res.Sort(delegate(KeyPoint t1, KeyPoint t2) {
			return (t1.X * t1.X + t1.Y * t1.Y).CompareTo(t2.X * t2.X + t2.Y * t2.Y);
		});
		return res;
	}
	*/

}
