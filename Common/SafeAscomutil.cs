using System;
using System.Threading;
using System.Reflection;

using ASCOM.Utilities;

namespace ASCOM.Wise40.Common
{
    public class SafeAscomutil : IDisposable
    {
        private Util util;
        private Mutex mutex;
        private const int mutexTimeoutMillis = 5000;
        private bool disposed = false;
        private readonly string className;

        public SafeAscomutil()
        {
            className = GetType().Name;

            try
            {
                util = new Util();
                mutex = new Mutex(false, Const.Mutexes.AscomUtil);
            }
            catch (Exception ex)
            {
                throw new DriverException($"Exception while creating {className}: {ex.Message}, see inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Dispose of objects used by the wrapper
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        util.Dispose();
                        util = null;
                    }
                    catch { }

                    try
                    {
                        mutex.Dispose();
                        mutex = null;
                    }
                    catch { }
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put clean-up code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public double JulianDate
        {
            get
            {
                bool gotMutex;

                try
                {
                    gotMutex = mutex.WaitOne(mutexTimeoutMillis, false);
                }
                catch (AbandonedMutexException ex)
                {
                    throw new DriverException($"{className} - Abandoned Mutex Exception for method {MethodBase.GetCurrentMethod().Name}. See inner exception for detail", ex);
                }
                catch (Exception ex)
                {
                    throw new DriverException($"{className} - Exception acquiring Mutex for method {MethodBase.GetCurrentMethod().Name}. See inner exception for detail", ex);
                }

                // Test whether we have the mutex
                if (!gotMutex) // Exit if we failed to get the mutex within the timeout period
                {
                    throw new DriverException($"{className} - Timed out waiting for AstroUtils mutex after {mutexTimeoutMillis}ms in method {MethodBase.GetCurrentMethod().Name}.");
                }

                double ret;
                try
                {
                    ret = util.JulianDate;
                }
                catch (Exception ex)
                {
                    throw new DriverException($"{className} - Exception calling method AstroUtils.{MethodBase.GetCurrentMethod().Name}: {ex.Message}. See inner exception for details", ex);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }

                return ret;
            }
        }
    }
}
