using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace AngleTestProject
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class WiseCalcDomePos
    {
        DomeSlaveDriver domeSlaveDriver;
        ASCOM.Wise40.Common.Debugger debugger = WiseTele.Instance.debugger;

        public WiseCalcDomePos()
        {
            domeSlaveDriver = new DomeSlaveDriver();
            domeSlaveDriver.init();
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestMethod1()
        {
            Angle Az, Ha, Dec;
            Angle LST = WiseSite.Instance.LocalSiderealTime;

            Ha = LST;
            Dec = Angle.FromDegrees(65, Angle.Type.Dec);
            Az = domeSlaveDriver.CalculateDomeAzimuth(Ha, Dec);
            Console.WriteLine(string.Format("{0}, {1} => {2}", Ha.ToString(), Dec.ToString(), Az.ToNiceString()));

            Ha = LST;
            Dec = Angle.FromDegrees(25, Angle.Type.Dec);
            Az = domeSlaveDriver.CalculateDomeAzimuth(Ha, Dec);
            Console.WriteLine(string.Format("{0}, {1} => {2}", Ha.ToString(), Dec.ToString(), Az.ToNiceString()));

            Ha = LST;
            Dec = Angle.FromDegrees(-15, Angle.Type.Dec);
            Az = domeSlaveDriver.CalculateDomeAzimuth(Ha, Dec);
            Console.WriteLine(string.Format("{0}, {1} => {2}", Ha.ToString(), Dec.ToString(), Az.ToNiceString()));

            Ha = LST;
            Dec = Angle.FromDegrees(0, Angle.Type.Dec);
            Az = domeSlaveDriver.CalculateDomeAzimuth(Ha, Dec);
            Console.WriteLine(string.Format("{0}, {1} => {2}", Ha.ToString(), Dec.ToString(), Az.ToNiceString()));


            Ha = LST + Angle.FromHours(4);
            Dec = Angle.FromRadians(0, Angle.Type.Dec);
            Az = domeSlaveDriver.CalculateDomeAzimuth(Ha, Dec);
            Console.WriteLine(string.Format("{0}, {1} => {2}", Ha.ToString(), Dec.ToString(), Az.ToNiceString()));

            Ha = Angle.FromHours(-3.25);
            Dec = Angle.FromRadians(0, Angle.Type.Dec);
            Az = domeSlaveDriver.CalculateDomeAzimuth(Ha, Dec);
            Console.WriteLine(string.Format("{0}, {1} => {2}", Ha.ToString(), Dec.ToString(), Az.ToNiceString()));
        }
    }
}
