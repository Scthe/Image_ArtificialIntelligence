using System;
using System.Collections.Generic;

namespace AI_4.model {

	public class NeighbourPointsMatcher {

		/// <summary>
		/// Takes data generated from 2 images and returns list of indices
		/// that create the 'closest neighbour list'
		/// </summary>
		/// <returns>List of tuples, each tuple consists of 2 indices</returns>
		public List<Tuple<int, int>> match(
			KeyPoint[] kps1, TraitsLookupMatrix lookup1,
			KeyPoint[] kps2, TraitsLookupMatrix lookup2) {

			var res = new List<Tuple<int, int>>();
			res.Capacity = Math.Max(kps1.Length, kps2.Length);
			var a = findClose(kps1, lookup2);
			var b = findClose(kps2, lookup1);
			foreach (int idA in a) {
				int idB = a[idA];
				if (b[idB] == idA) // if idA points to idB and idB points to idA
					res.Add(new Tuple<int, int>(idA, idB));
			}

			return res;
		}

		/// <summary>
		/// Takes keyPoints from one image and lookup table from the other.
		/// Then proceeds to match for every point the closes matching point
		/// from the other image.
		/// </summary>
		/// <returns>Array of indices to the image2.
		/// For each keypoint from image1 there is guarranted to be one.</returns>
		private int[] findClose(KeyPoint[] img1KPs, TraitsLookupMatrix img2lookup) {
			int[] res = new int[img1KPs.Length];
			var freqForOtherKeypoint = new Dictionary<int, int>(); // should have used heap

			for (int keyPoint1_i = 0; keyPoint1_i < img1KPs.Length; keyPoint1_i++) {
				KeyPoint keyPoint = img1KPs[keyPoint1_i];
				int bestId = 0, bestIdFreq = 0;
				for (int i = 0; i < 128; i++) {
					// get closest keypoint from alien image in respect to cecha
					float cechaVal = keyPoint[i];
					int otherImgClosestPointIDForCecha = img2lookup.findClosest(i, cechaVal);
					// look up how many times this alien keyPoint happened before
					int currentFreq;
					bool alreadyExists = freqForOtherKeypoint.TryGetValue(otherImgClosestPointIDForCecha, out currentFreq);
					if (!alreadyExists)
						currentFreq = 1;
					// increase alien key frequency, set as best if necessary
					freqForOtherKeypoint[otherImgClosestPointIDForCecha] = currentFreq;
					if (currentFreq > bestIdFreq) {
						bestIdFreq = currentFreq; bestId = otherImgClosestPointIDForCecha;
					}
				}
				res[keyPoint1_i] = bestId;
				freqForOtherKeypoint.Clear();
			}

			return res;
		}
	}

}
