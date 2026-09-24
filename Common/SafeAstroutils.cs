using System;

using ASCOM.Astrometry.AstroUtils;
using System.Threading;
using System.Reflection;

namespace ASCOM.Wise40.Common
{
    public class SafeAstroutils: IDisposable
    {
        //
        // ONE PER PROCESS, AND NEVER RELEASED.
        //
        // ASCOM.Astrometry's NOVAS31 and AstroUtils are thin managed facades over a single
        //  native library holding PROCESS-WIDE state, including the open ephemeris.  Tearing
        //  down any one instance tears it down for everybody:
        //
        //      NOVAS31.Dispose()    while another thread is in the library -> 0xC0000409
        //      AstroUtils.Dispose() while another thread is in the library -> 0xC0000005
        //      merely letting one be COLLECTED (no Dispose at all)         -> ExecutionEngine
        //
        //  All three measured on 2026-09-24, each inside a second, with one thread looping on
        //  MoonIllumination and another creating and dropping instances.  Instant process death,
        //  no managed stack, nothing in any log - which is precisely how ASCOM.RemoteServer was
        //  dying whenever a moon-position action met a driver tearing down for a client.
        //
        // So: one instance, static, created once, and Dispose does NOT touch it.  A wrapper is
        //  cheap to make and safe to drop; what must never happen is the native side going away
        //  while the process still lives.
        //
        private static readonly AstroUtils astroUtils = new AstroUtils();
        private Mutex mutex;
        private const int mutexTimeoutMillis = 5000;
        private bool disposed = false;
        private readonly string className;

        public SafeAstroutils()
        {
            className = GetType().Name;
            try
            {
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
            string methodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                gotMutex = mutex.WaitOne(mutexTimeoutMillis, false);
            }
            catch (AbandonedMutexException)
            {
                //
                // The previous owner died without releasing.  THIS EXCEPTION MEANS WE NOW OWN
                //  IT, so carry on; the finally below releases it.
                //
                // Throwing here - the old behaviour - left the mutex held forever, because the
                //  thrower never released what it had just been granted.  Every replacement
                //  process then took the same path, so the chain could not recover without a
                //  full service stop.  Watched happening on 2026-09-24, telescope included.
                //
                gotMutex = true;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    ex = ex.InnerException;
                throw new DriverException($"{className} - Exception acquiring Mutex for {className}.{methodName}", ex);
            }

            // Test whether we have the mutex
            if (!gotMutex) // Exit if we failed to get the mutex within the timeout period
            {
                throw new DriverException($"{className} - Timed out waiting for AstroUtils mutex after {mutexTimeoutMillis}ms in {className}.{methodName}.");
            }

            double ret;
            try
            {
                ret = astroUtils.JulianDateUT1(DeltaUT1);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    ex = ex.InnerException;
                throw new DriverException($"{className} - Exception calling method {className}.{methodName}: {ex.Message}", ex);
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
            catch (AbandonedMutexException)
            {
                //
                // The previous owner died without releasing.  THIS EXCEPTION MEANS WE NOW OWN
                //  IT, so carry on; the finally below releases it.
                //
                // Throwing here - the old behaviour - left the mutex held forever, because the
                //  thrower never released what it had just been granted.  Every replacement
                //  process then took the same path, so the chain could not recover without a
                //  full service stop.  Watched happening on 2026-09-24, telescope included.
                //
                gotMutex = true;
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
            catch (AbandonedMutexException)
            {
                //
                // The previous owner died without releasing.  THIS EXCEPTION MEANS WE NOW OWN
                //  IT, so carry on; the finally below releases it.
                //
                // Throwing here - the old behaviour - left the mutex held forever, because the
                //  thrower never released what it had just been granted.  Every replacement
                //  process then took the same path, so the chain could not recover without a
                //  full service stop.  Watched happening on 2026-09-24, telescope included.
                //
                gotMutex = true;
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
            catch (AbandonedMutexException)
            {
                //
                // The previous owner died without releasing.  THIS EXCEPTION MEANS WE NOW OWN
                //  IT, so carry on; the finally below releases it.
                //
                // Throwing here - the old behaviour - left the mutex held forever, because the
                //  thrower never released what it had just been granted.  Every replacement
                //  process then took the same path, so the chain could not recover without a
                //  full service stop.  Watched happening on 2026-09-24, telescope included.
                //
                gotMutex = true;
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
            catch (AbandonedMutexException)
            {
                //
                // The previous owner died without releasing.  THIS EXCEPTION MEANS WE NOW OWN
                //  IT, so carry on; the finally below releases it.
                //
                // Throwing here - the old behaviour - left the mutex held forever, because the
                //  thrower never released what it had just been granted.  Every replacement
                //  process then took the same path, so the chain could not recover without a
                //  full service stop.  Watched happening on 2026-09-24, telescope included.
                //
                gotMutex = true;
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
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // astroUtils is deliberately NOT disposed - see the field above.
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
