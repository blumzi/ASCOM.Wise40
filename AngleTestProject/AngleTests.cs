﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

using ASCOM;
using ASCOM.Wise40.Common;
using ASCOM.Astrometry.AstroUtils;

namespace AngleTests
{
    public static class ExceptionAssert
    {
        public static T Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex;
            }
            Assert.Fail("Exception of type {0} should have been thrown.", typeof(T));

            //  The compiler doesn't know that Assert.Fail
            //  will always throw an exception
            return null;
        }
    }

    [TestClass]
    public class AngleTest
    {
        private AstroUtils astroutils = new AstroUtils();

        [TestMethod]
        public void ShortestDistanceAzimuth()
        {
            ShortestDistanceResult shortest;

            shortest = new Angle(0.0, Angle.Type.Az).ShortestDistance(new Angle(45.0, Angle.Type.Az));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "0 -> 45");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "0 -> 45");

            shortest = new Angle(0.0, Angle.Type.Az).ShortestDistance(new Angle(315.0, Angle.Type.Az));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "0 -> 315");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "0 -> 315");

            shortest = new Angle(45.0, Angle.Type.Az).ShortestDistance(new Angle(90.0, Angle.Type.Az));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "45 -> 90");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "45 -> 90");

            shortest = new Angle(90.0, Angle.Type.Az).ShortestDistance(new Angle(135.0, Angle.Type.Az));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "90 -> 135");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "90 -> 135");

            shortest = new Angle(90.0, Angle.Type.Az).ShortestDistance(new Angle(315.0, Angle.Type.Az));
            Assert.AreEqual(new Angle(135.0), shortest.angle, "90 -> 315");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "90 -> 315");

            shortest = new Angle(315.0, Angle.Type.Az).ShortestDistance(new Angle(90.0, Angle.Type.Az));
            Assert.AreEqual(new Angle(135.0), shortest.angle, "315 -> 90");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "315 -> 90");

            shortest = new Angle(315.0, Angle.Type.Az).ShortestDistance(new Angle(254.0, Angle.Type.Az));
            Assert.AreEqual(new Angle(61.0), shortest.angle, "315 -> 254");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "315 -> 254");
        }

        [TestMethod]
        public void ShortestDistanceDeclination()
        {
            ShortestDistanceResult shortest;

            shortest = new Angle(0.0, Angle.Type.Dec).ShortestDistance(new Angle(45.0, Angle.Type.Dec));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "0 -> 45");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "0 -> 45");

            shortest = new Angle(0.0, Angle.Type.Dec).ShortestDistance(new Angle(-45.0, Angle.Type.Dec));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "0 -> -45");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "0 -> -45");

            shortest = new Angle(45.0, Angle.Type.Dec).ShortestDistance(new Angle(90.0, Angle.Type.Dec));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "45 -> 90");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "45 -> 90");

            shortest = new Angle(45.0, Angle.Type.Dec).ShortestDistance(new Angle(-10.0, Angle.Type.Dec));
            Assert.AreEqual(new Angle(55.0), shortest.angle, "45 -> -10");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "45 -> -10");

            shortest = new Angle(-90.0, Angle.Type.Dec).ShortestDistance(new Angle(-45.0, Angle.Type.Dec));
            Assert.AreEqual(new Angle(45.0), shortest.angle, "-90 -> -45");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "-90 -> -45");

            shortest = new Angle(-45, Angle.Type.Dec).ShortestDistance(new Angle(45.0, Angle.Type.Dec));
            Assert.AreEqual(new Angle(90.0), shortest.angle, "-45 -> 45");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "-45 -> 45");

            shortest = new Angle(90.0, Angle.Type.Dec).ShortestDistance(new Angle(-90.0, Angle.Type.Dec));
            Assert.AreEqual(new Angle(180.0), shortest.angle, "90 -> -90");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "90 -> -90");

            shortest = new Angle(20.0, Angle.Type.Dec).ShortestDistance(new Angle(-30.0, Angle.Type.Dec));
            Assert.AreEqual(new Angle(50.0), shortest.angle, "20 -> -30");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "20 -> -30");

            shortest = new Angle(-20.0, Angle.Type.Dec).ShortestDistance(new Angle(30.0, Angle.Type.Dec));
            Assert.AreEqual(new Angle(50.0), shortest.angle, "-20 -> 30");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "-20 -> 30");
        }

        [TestMethod]
        public void ShortestDistanceRightAscension()
        {
            ShortestDistanceResult shortest;

            shortest = Angle.FromHours(0.0).ShortestDistance(Angle.FromHours(1.25));
            Assert.AreEqual(Angle.FromHours(1.25), shortest.angle, "0 -> 1.25");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "0 -> 1.25");

            shortest = Angle.FromHours(0.0).ShortestDistance(Angle.FromHours(-2.5));
            Assert.AreEqual(Angle.FromHours(2.5), shortest.angle, "0 -> -2.5");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "0 -> -2.5");

            shortest = Angle.FromHours(2.0).ShortestDistance(Angle.FromHours(6.0));
            Assert.AreEqual(Angle.FromHours(4.0), shortest.angle, "2 -> 6");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "2 -> 6");

            shortest = Angle.FromHours(1.0).ShortestDistance(Angle.FromHours(23.0));
            Assert.AreEqual(Angle.FromHours(2.0), shortest.angle, "1 -> 23");
            Assert.AreEqual(Const.AxisDirection.Decreasing, shortest.direction, "1 -> 23");

            shortest = Angle.FromHours(22.0).ShortestDistance(Angle.FromHours(2.0));
            Assert.AreEqual(Angle.FromHours(4.0), shortest.angle, "22 -> 2");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "22 -> 2");

            shortest = new Angle("23h30m00s").ShortestDistance(new Angle("2h30m00s"));
            Assert.AreEqual(Angle.FromHours(3.0), shortest.angle, "23h30m00s -> 2h30m00s");
            Assert.AreEqual(Const.AxisDirection.Increasing, shortest.direction, "23h30m00s -> 2h30m00s");
        }

        [TestMethod]
        public void Azimuth()
        {
            Angle a1, a2;

            Assert.IsTrue(new Angle(15.0, Angle.Type.Az) > new Angle(0.0, Angle.Type.Az));
            Assert.IsTrue(new Angle(15.0, Angle.Type.Az) < new Angle(30.0, Angle.Type.Az));

            Assert.AreEqual(new Angle(-90.0, Angle.Type.Az), new Angle(270.0, Angle.Type.Az));

            Assert.AreEqual(new Angle(350, Angle.Type.Az) + new Angle(20, Angle.Type.Az), new Angle(10, Angle.Type.Az), "350 + 20 => 10");

            a1 = new Angle(350, Angle.Type.Az);
            Assert.AreEqual(a1 += new Angle(20), new Angle(10, Angle.Type.Az), "350 += 20 => 10");

            a1 = new Angle(45, Angle.Type.Az);
            a2 = new Angle(35, Angle.Type.Az);
            Assert.AreEqual(a1 + a2, new Angle(80, Angle.Type.Az), "45 + 35 => 80");

            Assert.AreEqual(new Angle(45, Angle.Type.Az) - new Angle(90, Angle.Type.Az), new Angle(315, Angle.Type.Az), "45 - 90 => 315");

            Assert.AreEqual(new Angle(45, Angle.Type.Az) - new Angle(90), new Angle(315, Angle.Type.Az), "45 - 90deg => 315");
            Assert.AreEqual(new Angle(60, Angle.Type.Az) + new Angle(310), new Angle(10, Angle.Type.Az), "60 + 310deg => 10");
        }

        [TestMethod]
        public void Generic()
        {
            Assert.AreEqual(new Angle(10.0, Angle.Type.Deg), new Angle(370.0, Angle.Type.Deg));
            Assert.AreEqual(new Angle(40.0, Angle.Type.Deg), new Angle(100.0, Angle.Type.Deg) + new Angle(300.0, Angle.Type.Deg));
        }

        [TestMethod]
        public void RightAscension()
        {
            Assert.IsTrue(new Angle(1.0, Angle.Type.RA) > new Angle(0.0, Angle.Type.RA));
            Assert.IsTrue(new Angle(1.0, Angle.Type.RA) < new Angle(3.0, Angle.Type.RA));
            
            Assert.AreEqual(new Angle("23h00m00.0s"), (new Angle("01h00m00.0s") - new Angle("02h00m00.0s")));
        }

        [TestMethod]
        public void Declination()
        {
            //string expected;
            //Exception ex;

            //Assert.AreEqual(Angle.FromDegrees(89.0, Angle.Type.Dec), Angle.FromDegrees(91.0, Angle.Type.Dec));
            //Assert.AreEqual(Angle.FromDegrees(-89.0, Angle.Type.Dec), Angle.FromDegrees(-91.0, Angle.Type.Dec));

            Assert.AreEqual(new Angle(12.5, Angle.Type.Dec) + new Angle(12.5, Angle.Type.Dec), new Angle(25, Angle.Type.Dec), "12.5 + 12.5 => 25");
            Assert.AreEqual(new Angle(-80, Angle.Type.Dec) + new Angle(20, Angle.Type.Dec), new Angle(-60, Angle.Type.Dec), "-80 + 20 => -60");
            Assert.AreEqual(new Angle(-10, Angle.Type.Dec) + new Angle(20, Angle.Type.Dec), new Angle(10, Angle.Type.Dec), "-10 + 20 => 10");
            Assert.AreEqual(new Angle(30, Angle.Type.Dec) - new Angle(45, Angle.Type.Dec), new Angle(-15, Angle.Type.Dec), "30 - 45 => -15");
        }

        [TestMethod]
        public void StringConversions()
        {
            Angle a1 = new Angle("1:2:3.4");
            Angle a2 = new Angle(1.0 + (2.0 / 60) + (3.4 / 60 / 60));
            Assert.AreEqual(a1, a2);

            a1 = new Angle("-1:2:3.4");
            a2 = new Angle(-1.0 - (2.0 / 60) - (3.4 / 60 / 60));
            Assert.AreEqual(a1, a2);

            a1 = new Angle("1h2m3.4s");
            a2 = Angle.FromHours(1.0 + (2.0 / 60) + (3.4 / 60 / 60));
            Assert.AreEqual(a1, a2);
            Assert.AreEqual(a1.ToString(), "01h02m03.4s");
            Assert.AreEqual(a2.ToString(), "01h02m03.4s");

            a1 = new Angle("2d3m4.5s");
            a2 = Angle.FromDegrees(2.0 + (3.0 / 60) + (4.5 / 60 / 60));
            Assert.AreEqual(a1, a2);
            Assert.AreEqual(a1.ToString(), "02:03:04.5");
            Assert.AreEqual(a2.ToString(), "02:03:04.5");
        }

        [TestMethod]
        public void Addition()
        {
            Angle a1, a2;

            a1 = new Angle("01h20m30.0s");
            a2 = new Angle("02h20m10.0s");
            var a3 = a1 + a2;
            var a4 = Angle.FromHours(3.0 + (40.0 / 60) + (40.0 / 60 / 60));
            Assert.AreEqual(a3, a4);

            a1 = new Angle("23h00m00.0s");
            a2 = new Angle("02h00m00.0s");
            Assert.AreEqual(a1 + a2, new Angle("01h00m00.0s"));
            
            Assert.AreEqual(new Angle("23h59m50.0s"), new Angle("00h00m10.0s") - new Angle("00h00m20.0s"), "00h00m10.0s - 00h00m20.0s = 23h59m50.0s");
        }

        //[TestMethod]
        //public void Normalizations()
        //{
        //    Angle a1, a2;

        //    a1 = new Angle(120.0, Angle.Type.Dec);
        //    a2 = new Angle(60.0, Angle.Type.Dec);
        //    Assert.AreEqual(a1, a2);

        //    a1 = new Angle(-110.0, Angle.Type.Dec);
        //    a2 = new Angle(-70.0, Angle.Type.Dec);
        //    Assert.AreEqual(a1, a2);

        //    a1 = new Angle("25h00m00.0s");
        //    a2 = new Angle("01h00m00.0s");
        //    Assert.AreEqual(a1, a2);

        //    a1 = new Angle("-01h00m00.0s");
        //    a2 = new Angle("23h00m00.0s");
        //    Assert.AreEqual(a1, a2);
        //}
    }
}
