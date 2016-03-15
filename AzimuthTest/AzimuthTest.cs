using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASCOM.Wise40;
using ExtendedDouble;

namespace AzimuthTest
{
    [TestClass]
    public class AzimuthTest
    {
        [TestMethod]
        public void Normalized_370()
        {
            double az = 370;
            
            Assert.AreEqual(10.0, az.Normalized());
        }

        [TestMethod]
        public void Normalized_582()
        {
            double az = 582;

            Assert.AreEqual(582 - 360, az.Normalized());
        }

        [TestMethod]
        public void Inc_350_and_25()
        {
            double az = 350;

            Assert.AreEqual(15, az.Inc(25));
        }

        [TestMethod]
        public void Inc_20_and_25()
        {
            double az = 20;

            Assert.AreEqual(45, az.Inc(25));
        }

        [TestMethod]
        public void minDelta_20_and_340()
        {
            double az = 20;

            Assert.AreEqual(40, az.minDelta(340));
        }

        [TestMethod]
        public void minDelta_180_and_359()
        {
            double az = 180;

            Assert.AreEqual(179, az.minDelta(359));
        }

        [TestMethod]
        public void DeltaCW_10_to_270()
        {
            double az = 10;

            Assert.AreEqual(260, az.DeltaCW(270));
        }

        [TestMethod]
        public void DeltaCW_270_to_10()
        {
            double az = 270;

            Assert.AreEqual(100, az.DeltaCW(10));
        }

        [TestMethod]
        public void DeltaCCW_10_to_270()
        {
            double az = 10;

            Assert.AreEqual(100, az.DeltaCCW(270));
        }

        [TestMethod]
        public void DeltaCCW_270_to_10()
        {
            double az = 270;

            Assert.AreEqual(260, az.DeltaCCW(10));
        }

        public static void Main()
        {            
        }
    }
}
