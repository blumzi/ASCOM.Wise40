using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

using ASCOM.Wise40.Common;

namespace AngleTests
{
    [TestClass]
    public class AngleTest
    {
        [TestMethod]
        public void ShortestDistance()
        {
            ShortestDistanceResult shortest;

            shortest = new Angle(0.0).ShortestDistance(new Angle(45.0));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "0 -> 45");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "0 -> 45");

            shortest = new Angle(0.0).ShortestDistance(new Angle(315.0));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "0 -> 315");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "0 -> 315");
            
            shortest = new Angle(45.0).ShortestDistance(new Angle(90.0));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "45 -> 90");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "45 -> 90");

            shortest = new Angle(90.0).ShortestDistance(new Angle(135.0));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "90 -> 135");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "90 -> 135");

            shortest = new Angle(90.0).ShortestDistance(new Angle(315.0));
            Assert.AreEqual(new Angle(135.0), shortest.angle, "90 -> 315");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "90 -> 315");

            shortest = new Angle(315.0).ShortestDistance(new Angle(90.0));
            Assert.AreEqual(new Angle(135.0), shortest.angle, "315 -> 90");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "315 -> 90");

            shortest = new Angle(315.0).ShortestDistance(new Angle(254.0));
            Assert.AreEqual(new Angle(61.0), shortest.angle, "315 -> 254");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "315 -> 254");
        }
    }
}
