using Microsoft.VisualStudio.TestTools.UnitTesting;
using AI_4.model;
using System;
using System.Collections.Generic;

namespace Tests {

	[TestClass]
	public class UnitTest1 {

		string testFile = "../../data/test.haraff.sift.txt";

		[TestMethod]
		public void TestSiftLoader() {
			SiftLoader l = new SiftLoader();
			//var imgData = l.load(testFile);

			var arr = new List<Tuple<float, int>>();
			for (int i = 0; i < 128; i++) {
				arr.Add(new Tuple<float, int>(300 - i, i));
			}

			arr.Sort(delegate(Tuple<float, int> t1, Tuple<float, int> t2) {
				return t1.Item1.CompareTo(t2.Item1); // sort by trait value
			});

			for (int i = 0; i < 128; i++) {
				Console.WriteLine(arr[i]);
			}

			Console.WriteLine("---END---");
		}
	}
}
