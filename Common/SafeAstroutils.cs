using System;

using ASCOM.Astrometry.AstroUtils;
using System.Threading;
using System.Reflection;

namespace ASCOM.Wise40.Common
{
    public class SafeAstroutils: IDisposable
    {
        private AstroUtils astroUtils;
        private Mutex mutex;
        private const int mutexTimeoutMillis = 5000;
        private bool disposed = false;
        private readonly string className;

        public SafeAstroutils()
        {
            className = GetType().Name;
            try
            {
                astroUtils = new AstroUtils();
                mutex = new Mutex(false, Const.Mutexes.AstroUtil);
            }
            catch (Exception ex)
            {
                throw new DriverException($"Exception while creating {className}: {ex.Message}, see inner exception for details.", ex);
            }
        }

        public double JulianDateUT1(double DeltaUT1)
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
                ret = astroUtils.JulianDateUT1(DeltaUT1);
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

        public double ConditionHA(double HA)
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
                ret = astroUtils.ConditionHA(HA);
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

        public double MoonIllumination(double jd)
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
                ret = astroUtils.MoonIllumination(jd);
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


        public double MoonPhase(double jd)
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
                ret = astroUtils.MoonPhase(jd);
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

        public double DeltaT()
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
                ret = astroUtils.DeltaT();
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

        /// <summary>
        /// Dispose of objects used by the wrapper
        /// </summary>
        /// <param name="disposing"></param>
        public virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        astroUtils.Dispose();
                        astroUtils = null;
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
    }
}
