using ASCOM.Astrometry;
using ASCOM.Astrometry.NOVAS;
using System;
using System.Reflection;
using System.Threading;

namespace ASCOM.Wise40.Common
{
    /// <summary>
    /// A thread-safe wrapper for novas31
    /// </summary>
    public class SafeNovas31 : IDisposable
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
        private static readonly NOVAS31 novas31 = new NOVAS31();
        private Mutex mutex;
        private const int mutexTimeout = 5000;
        private bool disposed = false;
        private readonly string className;

        public SafeNovas31()
        {
            className = GetType().Name;

            try
            {
                mutex = new Mutex(false, Const.Mutexes.Novas31);
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
                    // novas31 is deliberately NOT disposed - see the field above.
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

        public short Place(
            double JdTt,
            Object3 CelObject,
            Observer Location,
            double DeltaT,
            CoordSys CoordSys,
            Accuracy Accuracy,
            ref SkyPos Output
        )
        {
            bool gotMutex;

            try
            {
                gotMutex = mutex.WaitOne(mutexTimeout, false);
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
                throw new DriverException($"{className} - Timed out waiting for Novas31 mutex after {mutexTimeout}ms in method {MethodBase.GetCurrentMethod().Name}.");
            }

            short ret;
            try
            {
                ret = novas31.Place(JdTt, CelObject, Location, DeltaT, CoordSys, Accuracy, ref Output);
            }
            catch (Exception ex)
            {
                throw new DriverException($"{className} - Exception calling method Novas31.{MethodBase.GetCurrentMethod().Name}: {ex.Message}. See inner exception for details", ex);
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return ret;
        }

        public void Equ2Hor(
            double Jd_Ut1,
            double DeltT,
            Accuracy Accuracy,
            double xp,
            double yp,
            OnSurface Location,
            double Ra,
            double Dec,
            RefractionOption RefOption,
            ref double Zd,
            ref double Az,
            ref double RaR,
            ref double DecR
        )
        {
            bool gotMutex;

            try
            {
                gotMutex = mutex.WaitOne(mutexTimeout, false);
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
                throw new DriverException($"{className} - Timed out waiting for Novas31 mutex after {mutexTimeout}ms in method {MethodBase.GetCurrentMethod().Name}.");
            }

            try
            {
                novas31.Equ2Hor(Jd_Ut1, DeltT, Accuracy,
                        xp, yp,
                        Location, Ra, Dec, RefOption,
                        ref Zd, ref Az, ref RaR, ref DecR
                    );
            }
            catch (Exception ex)
            {
                throw new DriverException($"{className} - Exception calling method Novas31.{MethodBase.GetCurrentMethod().Name}: {ex.Message}. See inner exception for details", ex);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public short SiderealTime(
            double JdHigh,
            double JdLow,
            double DeltaT,
            GstType GstType,
            Method Method,
            Accuracy Accuracy,
            ref double Gst
        )
        {
            bool gotMutex;

            try
            {
                gotMutex = mutex.WaitOne(mutexTimeout, false);
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
                throw new DriverException($"{className} - Timed out waiting for Novas31 mutex after {mutexTimeout}ms in method {MethodBase.GetCurrentMethod().Name}.");
            }

            short ret;
            try
            {
                ret = novas31.SiderealTime(JdHigh, JdLow, DeltaT, GstType, Method, Accuracy, ref Gst);
            }
            catch (Exception ex)
            {
                throw new DriverException($"{className} - Exception calling method Novas31.{MethodBase.GetCurrentMethod().Name}: {ex.Message}. See inner exception for details", ex);
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return ret;
        }

        public void MakeOnSurface(
            double Latitude,
            double Longitude,
            double Height,
            double Temperature,
            double Pressure,
            ref OnSurface ObsSurface
        )
        {
            bool gotMutex;

            try
            {
                gotMutex = mutex.WaitOne(mutexTimeout, false);
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
                throw new DriverException($"{className} - Timed out waiting for Novas31 mutex after {mutexTimeout}ms in method {MethodBase.GetCurrentMethod().Name}.");
            }

            try
            {
                novas31.MakeOnSurface(Latitude, Longitude, Height, Temperature, Pressure, ref ObsSurface);
            }
            catch (Exception ex)
            {
                throw new DriverException($"{className} - Exception calling method Novas31.{MethodBase.GetCurrentMethod().Name}: {ex.Message}. See inner exception for details", ex);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public short MakeObject(
            ObjectType Type,
            short Number,
            string Name,
            CatEntry3 StarData,
            ref Object3 CelObj
        )
        {
            bool gotMutex;

            try
            {
                gotMutex = mutex.WaitOne(mutexTimeout, false);
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
                throw new DriverException($"{className} - Timed out waiting for Novas31 mutex after {mutexTimeout}ms in method {MethodBase.GetCurrentMethod().Name}.");
            }

            short ret;
            try
            {
                ret = novas31.MakeObject(Type, Number, Name, StarData, ref CelObj);
            }
            catch (Exception ex)
            {
                throw new DriverException($"{className} - Exception calling method Novas31.{MethodBase.GetCurrentMethod().Name}: {ex.Message}. See inner exception for details", ex);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return ret;
        }

        public void MakeObserverOnSurface(
            double Latitude,
            double Longitude,
            double Height,
            double Temperature,
            double Pressure,
            ref Observer ObsOnSurface
        )
        {
            bool gotMutex;

            try
            {
                gotMutex = mutex.WaitOne(mutexTimeout, false);
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
                throw new DriverException($"{className} - Timed out waiting for Novas31 mutex after {mutexTimeout}ms in method {MethodBase.GetCurrentMethod().Name}.");
            }

            try
            {
                novas31.MakeObserverOnSurface(Latitude, Longitude, Height, Temperature, Pressure, ref ObsOnSurface);
            }
            catch (Exception ex)
            {
                throw new DriverException($"{className} - Exception calling method Novas31.{MethodBase.GetCurrentMethod().Name}: {ex.Message}. See inner exception for details", ex);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        public short TopoPlanet(
            double JdTt,
            Object3 SsBody,
            double DeltaT,
            OnSurface Position,
            Accuracy Accuracy,
            ref double Ra,
            ref double Dec,
            ref double Dis
        )
        {
            bool gotMutex;

            try
            {
                gotMutex = mutex.WaitOne(mutexTimeout, false);
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
                throw new DriverException($"{className} - Timed out waiting for Novas31 mutex after {mutexTimeout}ms in method {MethodBase.GetCurrentMethod().Name}.");
            }

            short ret;
            try
            {
                ret = novas31.TopoPlanet(JdTt, SsBody, DeltaT, Position, Accuracy, ref Ra, ref Dec, ref Dis);
            }
            catch (Exception ex)
            {
                throw new DriverException($"{className} - Exception calling method Novas31.{MethodBase.GetCurrentMethod().Name}: {ex.Message}. See inner exception for details", ex);
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return ret;
        }
    }
}