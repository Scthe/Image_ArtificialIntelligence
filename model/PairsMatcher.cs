﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AI_4.model {

	public class PairsMatcher {

		/// <summary>
		/// Takes data generated from 2 images and returns list of indices
		/// that create the 'closest neighbour list'
		/// </summary>
		/// <returns>List of tuples, each tuple consists of 2 indices</returns>
		public List<Tuple<int, int>> match(ImageData imgData1, ImageData imgData2, CancellationTokenSource cts) {
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			var kps1 = imgData1.Keypoints;
			var kps2 = imgData2.Keypoints;
			var lookup1 = imgData1.LookupMatrix;
			var lookup2 = imgData2.LookupMatrix;
			var res = new List<Tuple<int, int>>(Math.Min(kps1.Count, kps2.Count));

			var neighboursForPointsOnImg1 = findClose(kps1, lookup2, kps2.Count);
			var neighboursForPointsOnImg2 = findClose(kps2, lookup1, kps1.Count);

			Console.WriteLine("\t[PROFILE]Generated neighboursForPointsOnImg: " + stopwatch.ElapsedMilliseconds);

			foreach (int idB in neighboursForPointsOnImg1) {
				if (cts != null)
					cts.Token.ThrowIfCancellationRequested();

				int idA = neighboursForPointsOnImg2[idB];
				if (neighboursForPointsOnImg1[idA] == idB)
					// if idA points to idB and idB points to idA
					res.Add(new Tuple<int, int>(idA, idB));
			}
			stopwatch.Stop();
			Console.WriteLine("\t[PROFILE]Fin neighboursForPointsOnImg: " + stopwatch.ElapsedMilliseconds);

			return res;
		}

		/// <summary>
		/// Takes keyPoints from one image and lookup table from the other.
		/// Then proceeds to match for every point the closes matching point
		/// from the other image.
		/// </summary>
		/// <returns>Array of indices to the image2.
		/// For each keypoint from image1 there is guarranted to be one.</returns>
		private int[] findClose(List<KeyPoint> img1KPs, TraitsLookupMatrix img2lookup, int img2KPCount) {
			int[] res = new int[img1KPs.Count];

			// http://msdn.microsoft.com/en-us/library/dd460713(v=vs.110).aspx
			Parallel.For(0, img1KPs.Count, keyPoint1_i => {
				int[] freqForOtherKeypoint = new int[img2KPCount];
				List<int> tmpList = new List<int>(img2KPCount);

				KeyPoint keyPoint = img1KPs[keyPoint1_i];
				int bestId = 0, bestIdFreq = 0;
				for (int ii = 0; ii < img2KPCount; ii++)
					freqForOtherKeypoint[ii] = 0;

				for (int i = 0; i < KeyPoint.TRAITS_COUNT; i++) {
					// get closest keypoint from alien image in respect to the trait
					int traitVal = keyPoint[i];

					img2lookup.findClosest(i, traitVal, tmpList);

					foreach (var otherImgClosestPointIDForTrait in tmpList) {
						// look up how many times this alien keyPoint happened before
						int currentFreq = freqForOtherKeypoint[otherImgClosestPointIDForTrait] + 1;
						// increase alien key frequency, set as best if necessary
						freqForOtherKeypoint[otherImgClosestPointIDForTrait] = currentFreq;
						if (currentFreq > bestIdFreq) {
							bestIdFreq = currentFreq; bestId = otherImgClosestPointIDForTrait;
						}
					}
				}
				res[keyPoint1_i] = bestId;
			});
			return res;
		}
	}
}
