using System;
using System.Collections.Generic;

using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;

namespace AI_4.model {

	class RANSAC_RESULT {
		internal KeyPoint[] kp1 = new KeyPoint[3];
		internal KeyPoint[] kp2 = new KeyPoint[3];
		internal Matrix<double> matrix;
	}

	class A2_RANSAC {

		public int IterCount { set; private get; }
		// all values are in pixels or pixelsSquare
		public double MaxError { set; private get; }
		public double R { set; private get; }
		public double r { set; private get; }

		public A2_RANSAC() {
			IterCount = 50;
			MaxError = 50;
			R = 400;
			r = 20;
		}

		public RANSAC_RESULT reduce(List<Tuple<int, int>> pairs, ImageData img1, ImageData img2) {
			var ks1 = img1.Keypoints;
			var ks2 = img2.Keypoints;
			//var tmpKPs = new List<Tuple<int, int>>(pairs.Capacity);

			RANSAC_RESULT bestModel = null;
			double bestScore = double.MinValue;
			for (int i = 0; i < IterCount; i++) {
				//Console.WriteLine("[Debug] ransac iter: " + i);
				RANSAC_RESULT model = createModelNonOptimized(pairs, ks1, ks2);

				// iterate over all points and calculate respective error
				double score = 0;
				foreach (var pair in pairs) {
					var kpA = ks1[pair.Item1];
					var kpB = ks2[pair.Item2];
					double expectedX, expectedY;
					apply(kpA.X, kpA.Y, model, out expectedX, out expectedY);
					if (errorMetric(expectedX, expectedY, kpB.X, kpB.Y) < MaxError)
						score += 1;
				}

				Console.WriteLine("[Debug] ransac iter: " + i + " score " + score + " /" + pairs.Count);
				// score
				if (score > bestScore) {
					bestScore = score;
					bestModel = model;
				}
			}

			return bestModel;
		}

		#region utils main

		/// <summary>
		/// Keypoint that is paired with the provided pointSrc
		/// </summary>
		/// <returns>Should always return resulte</returns>
		//private static KeyPoint getPairedKeypoint(KeyPoint pointSrc, List<Tuple<int, int>> pairs, List<KeyPoint> pointsDst) {
		//Tuple<int, int> currentPair = pairs.FirstOrDefault((p) => p.Item1 == pointSrc.ID);
		//return currentPair == null ? null : pointsDst[currentPair.Item2];
		//}

		private static double errorMetric(double x1, double y1, double x2, double y2) {
			return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
		}

		#endregion

		#region private

		/// <summary>
		/// Create base matrix that transforms from arbitrary triangle from img1 to img2
		/// </summary>
		/// <returns></returns>
		private RANSAC_RESULT createModel(List<Tuple<int, int>> pairs, List<KeyPoint> l1, List<KeyPoint> l2) {
			Random rnd = new Random();
			RANSAC_RESULT model = new RANSAC_RESULT();
			do {
				var p1 = pairs[rnd.Next(pairs.Count)];
				var p2 = pairs[rnd.Next(pairs.Count)];
				var p3 = pairs[rnd.Next(pairs.Count)];
				model.kp1[0] = l1[p1.Item1];
				model.kp1[1] = l1[p2.Item1];
				model.kp1[2] = l1[p3.Item1];
				model.kp2[0] = l2[p1.Item2];
				model.kp2[1] = l2[p2.Item2];
				model.kp2[2] = l2[p3.Item2];
			} while (!checkModelConstraints(model));
			model.matrix = createAffinicMatrix(l1, l2, model);
			return model;
		}

		private RANSAC_RESULT createModelNonOptimized(List<Tuple<int, int>> pairs, List<KeyPoint> l1, List<KeyPoint> l2) {
			Random rnd = new Random();
			RANSAC_RESULT model = new RANSAC_RESULT();
			var p1 = pairs[rnd.Next(pairs.Count)];
			model.kp1[0] = l1[p1.Item1];
			model.kp2[0] = l2[p1.Item2];
			do {
				var p2 = pairs[rnd.Next(pairs.Count)];
				var p3 = pairs[rnd.Next(pairs.Count)];
				model.kp1[1] = l1[p2.Item1];
				model.kp1[2] = l1[p3.Item1];
				model.kp2[1] = l2[p2.Item2];
				model.kp2[2] = l2[p3.Item2];
			} while (model.kp1[2] == model.kp1[0] || model.kp1[1] == model.kp1[0] || model.kp1[2] == model.kp1[1]
				|| model.kp2[2] == model.kp2[0] || model.kp2[1] == model.kp2[0] || model.kp2[2] == model.kp2[1]);
			model.matrix = createAffinicMatrix(l1, l2, model);
			return model;
		}

		/// <summary>
		/// Apply transformation from model to point (x,y).
		/// Can be used to display result - given set of keypoints from img1 apply transformation and then display expected results
		/// </summary>
		public static void apply(float x, float y, RANSAC_RESULT model, out double outX, out double outY) {
			Vector<double> vec = new DenseVector(new double[] { x, y, 1 });
			var v = model.matrix * vec;
			outX = v[0];
			outY = v[1];
		}

		#endregion

		#region other

		private bool checkModelConstraints(RANSAC_RESULT model) {
			//if (model.kp1[0] == model.kp1[1] || model.kp1[0] == model.kp1[2] || model.kp1[2] == model.kp1[1] ||
			//model.kp2[0] == model.kp2[1] || model.kp2[0] == model.kp2[2] || model.kp2[2] == model.kp2[1])
			//return false; // will fail anyway at dist > r

			double d1 = errorMetric(model.kp1[0].X, model.kp1[0].Y, model.kp1[1].Y, model.kp1[1].Y);
			double d2 = errorMetric(model.kp1[0].X, model.kp1[0].Y, model.kp1[2].Y, model.kp1[2].Y);
			double d3 = errorMetric(model.kp1[1].X, model.kp1[1].Y, model.kp1[2].Y, model.kp1[2].Y);
			double d4 = errorMetric(model.kp2[0].X, model.kp2[0].Y, model.kp2[1].Y, model.kp2[1].Y);
			double d5 = errorMetric(model.kp2[0].X, model.kp2[0].Y, model.kp2[2].Y, model.kp2[2].Y);
			double d6 = errorMetric(model.kp2[1].X, model.kp2[1].Y, model.kp2[2].Y, model.kp2[2].Y);
			double r2 = r * r, R2 = R * R;
			return
				d1 > r2 && d1 < R2 &&
				d2 > r2 && d2 < R2 &&
				d3 > r2 && d3 < R2 &&
				d4 > r2 && d4 < R2 &&
				d5 > r2 && d5 < R2 &&
				d6 > r2 && d6 < R2;
		}

		private static Matrix<double> createAffinicMatrix(List<KeyPoint> kp1, List<KeyPoint> kp2, RANSAC_RESULT model) {
			float x1 = model.kp1[0].X;
			float y1 = model.kp1[0].Y;
			float x2 = model.kp1[1].X;
			float y2 = model.kp1[1].Y;
			float x3 = model.kp1[2].X;
			float y3 = model.kp1[2].Y;
			float u1 = model.kp2[0].X;
			float v1 = model.kp2[0].Y;
			float u2 = model.kp2[1].X;
			float v2 = model.kp2[1].Y;
			float u3 = model.kp2[2].X;
			float v3 = model.kp2[2].Y;

			// http://numerics.mathdotnet.com/docs/
			Matrix<double> mat = DenseMatrix.OfArray(new double[,] {
				{ x1,  y1,	1,		0,	0,	0},
				{ x2,  y2,	1,		0,	0,	0},
				{ x3,  y3,	1,		0,	0,	0},
				{ 0,	0,	0,		x1,	y1,	1},
				{ 0,	0,	0,		x2,	y2,	1},
				{ 0,	0,	0,		x3,	y3,	1}
			});
			var matInv = mat.Inverse();

			Vector<double> vec = new DenseVector(new double[] {
				u1,u2,u3,	v1,v2,v3 });

			var r = matInv * vec; // vector_abcdef
			Matrix<double> A = DenseMatrix.OfArray(new double[,] {
				{ r[0],		r[1],	r[2]},
				{ r[3],		r[4],	r[5]},
				{ 0,		0,		1	},
			});
			return A;
		}

		#endregion
	}

	/*
	Matrix<double> A = DenseMatrix.OfArray(new double[,] {
		{1,1,1,1},
		{1,2,3,4},
		{4,3,2,1}});
	Vector<double>[] nullspace = A.Kernel();

	model.kp1[0] = l1[rnd.Next(l1.Count)];
	model.kp1[1] = l1[rnd.Next(l1.Count)];
	model.kp1[2] = l1[rnd.Next(l1.Count)];
	model.kp2[0] = getPairedKeypoint(model.kp1[0], pairs, l2);
	model.kp2[1] = getPairedKeypoint(model.kp1[1], pairs, l2);
	model.kp2[2] = getPairedKeypoint(model.kp1[2], pairs, l2);
	 
	foreach (var kpA in ks1) {
		double expectedX, expectedY;
		apply(kpA.X, kpA.Y, model, out expectedX, out expectedY);
		KeyPoint kpB = getPairedKeypoint(kpA, pairs, ks2);
		if (errorMetric(expectedX, expectedY, kpB.X, kpB.Y) < MaxError)
			score += 1;
	}

	KeyPoint k11 = kp1.First((p) => p.ID == t1.Item1);
	KeyPoint k12 = kp1.First((p) => p.ID == t2.Item1);
	KeyPoint k13 = kp1.First((p) => p.ID == t3.Item1);
	KeyPoint k21 = kp2.First((p) => p.ID == t1.Item2);
	KeyPoint k22 = kp2.First((p) => p.ID == t2.Item2);
	KeyPoint k23 = kp2.First((p) => p.ID == t3.Item2);
	float x1 = k11.X;
	float y1 = k11.Y;
	float x2 = k12.X;
	float y2 = k12.Y;
	float x3 = k13.X;
	float y3 = k13.Y;
	float u1 = k21.X;
	float v1 = k21.Y;
	float u2 = k22.X;
	float v2 = k22.Y;
	float u3 = k23.X;
	float v3 = k23.Y;
	*/

}
