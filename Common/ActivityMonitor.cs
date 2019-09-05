﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Utilities;
using System.IO;

using MySql.Data;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using ASCOM.DeviceInterface;

namespace ASCOM.Wise40
{
    public class ActivityMonitor : WiseObject
    {
        // start Singleton
        private static readonly Lazy<ActivityMonitor> lazy = new Lazy<ActivityMonitor>(() => new ActivityMonitor()); // Singleton

        public static ActivityMonitor Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
            }
        }

        private ActivityMonitor() { }
        static ActivityMonitor() { }
        // end Singleton

        public static int defaultRealMillisToInactivity = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;
        public static int realMillisToInactivity;
        public static readonly int simulatedlMillisToInactivity = (int)TimeSpan.FromMinutes(3).TotalMilliseconds;
        public static Debugger debugger = Debugger.Instance;
        private DateTime _due = DateTime.MinValue;                  // not set
        private WiseSite wisesite = WiseSite.Instance;
        private static bool initialized = false;
        public static int millisToInactivity;

        [FlagsAttribute]
        public enum ActivityType
        {
            None = 0,
            TelescopeSlew = (1 << 1),
            PulsingRa = (1 << 2),
            DomeSlew = (1 << 3),
            Handpad = (1 << 4),
            GoingIdle = (1 << 5),
            Parking = (1 << 6),
            Shutter = (1 << 7),
            ShuttingDown = (1 << 8),
            Focuser = (1 << 9),
            FilterWheel = (1 << 10),
            Projector = (1 << 11),
            Safety = (1 << 12),
            DomeTracking = (1 << 13),
            PulsingDec = (1 << 14),

            // activities that affect the observatory's Idle state
            RealActivities = TelescopeSlew | PulsingRa | PulsingDec | DomeSlew | Handpad | Parking | Shutter | Focuser | FilterWheel | ShuttingDown,
        };

        public void init()
        {
            if (!Simulated && !WiseSite.CurrentProcessIsASCOMServer)
                return;

            if (initialized)
                return;

            int defaultMinutesToIdle = (int) TimeSpan.FromMilliseconds(defaultRealMillisToInactivity).TotalMinutes;
            int minutesToIdle;

            using (Profile p = new Profile() { DeviceType = "Telescope" })
                minutesToIdle = Convert.ToInt32(p.GetValue(Const.WiseDriverID.Telescope,
                    Const.ProfileName.Telescope_MinutesToIdle,
                    string.Empty,
                    defaultMinutesToIdle.ToString()));

            realMillisToInactivity = (int) TimeSpan.FromMinutes(minutesToIdle).TotalMilliseconds;
            millisToInactivity = WiseObject.Simulated ?
                ActivityMonitor.simulatedlMillisToInactivity :
                ActivityMonitor.realMillisToInactivity;

            initialized = true;
        }

        
        public void EndActivity(ActivityType type, Activity.GenericEndParams par)
        {
            Activity inProgress = LookupInProgress(type);

            if (inProgress == null)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"ActivityMonitor:EndActivity: No \"{type}\" inProgress");
                #endregion
                return;
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"ActivityMonitor:EndActivity: Calling {type}.End()");
            #endregion
            inProgress._endTime = par.endTime == null ? DateTime.UtcNow : par.endTime;
            inProgress.End(par);
        }

        public bool ShuttingDown
        {
            get
            {
                return inProgressDict.ContainsKey(ActivityType.ShuttingDown);
            }
        }

        public bool InProgress(ActivityType a)
        {
            return LookupInProgress(a) != null;
        }

        /// <summary>
        /// Called by activities that reset the idle-time counter (e.g. AbortSlew or setting the target)
        /// </summary>
        /// <param name="reason"></param>
        public void StayActive(string reason)
        {
            idler.StayActive(new Idler.StartParams() { reason = reason });
        }

        public TimeSpan RemainingTime
        {
            get
            {
                return idler.RemainingTime;
            }
        }

        public bool ObservatoryIsActive()
        {
            bool ret = !idler.Idle;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"ActivityMonitor:ObservatoryIsActive: idler.Status: {idler.Status}, ret: {ret}");
            #endregion
            return ret;
        }

        public static List<string> ObservatoryActivities
        {
            get
            {
                List<string> ret = new List<string>();

                string status = idler.Status;
                if (status.StartsWith("GoingIdle"))
                    ret.Add(status);

                foreach (ActivityType key in Instance.inProgressDict.Keys)
                    ret.Add(key.ToString());
                return ret;
            }
        }

        public ConcurrentDictionary<ActivityType, Activity> inProgressDict = new ConcurrentDictionary<ActivityType, Activity>(); 
        public static Idler idler = Idler.Instance;

        public Activity LookupInProgress(ActivityType type)
        {
            string dbg = string.Format($"ActivityMonitor:LookupInProgress: type: {type}, in progress: [{inProgressDict.ToCSV()}] => ");

            if (inProgressDict.TryGetValue(type, out Activity ret))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, dbg + "Found");
                #endregion
            } else
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, dbg + "Not found");
                #endregion
            }
            return ret;
        }

        public Activity NewActivity(Activity activity)
        {
            if (inProgressDict.ContainsKey(activity._type))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"inProgressDict already contains {activity._type}");
                #endregion
                throw new InvalidDataException(string.Format($"inProgressDict already contains {activity._type}"));
            }

            inProgressDict[activity._type] = activity;

            if (activity.effectOnGoingIdle_AtStart == Activity.EffectOnGoingIdle.Remove)
            {
                idler.AbortGoingIdle(new Idler.EndParams() { endReason = string.Format($"{activity._type} activity started") });
            }

            return activity;
        }

        public void Event(Event e)
        {
            e.EmitEvent();
        }

        public class Tracer
        {
            public static string MySqlActivitiesConnectionString = "server=localhost;user=root;database=activities;port=3306;password=@!ab4131!@";
            private string _line;
            private string _name;

            public const int resetValue = 0;

            public Tracer(string line, string name)
            {
                _line = line;
                _name = name;
            }

            public string Line
            {
                get
                {
                    return _line;
                }
            }

            public string Name
            {
                get
                {
                    return _name;
                }
            }

            public class Safety : Tracer
            {
                public enum Code { NotSafe = 150 };

                public Safety() : base("safety", "Safety") { }
            }
            public static Safety safety = new Safety();

            public class Projector : Tracer
            {
                public Projector() : base("projector", "Projector") { }
                public enum Code { On = 140 };
            }
            public static Projector projector = new Projector();

            public class Tracking : Tracer
            {
                public Tracking() : base("tracking", "Tracking") { }
                public enum Code { On = 120 };
            }
            public static Tracking tracking = new Tracking();

            public class Telescope : Tracer
            {
                public Telescope() : base("telescope", "Telescope") { }
                public enum Code { Slewing = 100, Pulsing = 110 };
            }
            public static Telescope telescope = new Telescope();

            public class Dome : Tracer
            {
                public Dome() : base("dome", "Dome") { }
                public enum Code { Idle = 0, FindingHome = 160, Slewing = 50, Tracking = 60 };
            }
            public static Dome dome = new Dome();

            public class Shutter : Tracer
            {
                public Shutter() : base("shutter", "Shutter") { }
                public enum Code { Opening = 80, Closing = 90, Open = 70 };
            }
            public static Shutter shutter = new Shutter();

            public class Parking : Tracer
            {
                public Parking() : base("parking", "Parking") { }
                public enum Code { Active = 20 };
            }
            public static Parking parking = new Parking();

            public class Shutdown : Tracer
            {
                public Shutdown() : base("shutdown", "Shutdown") { }
                public enum Code { Idle = 0, Active = 170 };
            }
            public static Shutdown shutdown = new Shutdown();

            public class Focuser : Tracer
            {
                public Focuser() : base("focuser", "Focuser") { }
                public enum Code { Moving = 30 };
            }
            public static Focuser focuser = new Focuser();

            public class FilterWheel : Tracer
            {
                public FilterWheel() : base("focuser", "FilterWheel") { }
                public enum Code { Moving = 40 };
            }
            public static FilterWheel filterwheel = new FilterWheel();

            public class Idler : Tracer
            {
                public Idler() : base("idler", "Idler") { }
                public enum Code { GoingIdle = 130, Idle =  180 };
            }
            public static Idler idler = new Idler();

            //public static void Reset(Tracer t, string reason)
            //{
            //    string sql = string.Format("insert into activities(time, line, code, text, tags) values('{0}', '{1}', '{2}', '{3}', '{4}'); ",
            //            DateTime.UtcNow.ToMySqlDateTime(),
            //            t.Line,
            //            Tracer.resetValue,
            //            t.Name + " " + reason,
            //            t.Name + "," + reason);
            //    try
            //    {
            //        using (var sqlConn = new MySqlConnection(MySqlActivitiesConnectionString))
            //        {
            //            sqlConn.Open();
            //            using (var sqlCmd = new MySqlCommand(sql, sqlConn))
            //            {
            //                sqlCmd.ExecuteNonQuery();
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        #region debug
            //        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Tracking.ResetTracer: \nsql: {0}\n failed: {1}", sql, ex.StackTrace);
            //        #endregion
            //    }
            //}
        }
    }


    public abstract class Activity: IEquatable<Activity>
    {
        public ActivityMonitor.ActivityType _type;
        
        public enum State { NotSet, Pending, Succeeded, Failed, Aborted, Idle, Ignored };
        public State _endState;
        public int _code;
        public string _line;
        public string _annotation;
        public List<string> _tags;

        public enum EffectOnGoingIdle  {
            NotSet,     // default, needs to be changed
            NoEffect,   // does not affect the GoingIdleActivity (usually for weather or log-only activities)
            Remove,     // removes the GoingIdleActivity
            Renew,      // restarts the GoingIdleActivity (usually onEnd)
        };
        public EffectOnGoingIdle effectOnGoingIdle_AtStart = EffectOnGoingIdle.NotSet;
        public EffectOnGoingIdle effectOnGoingIdle_AtEnd = EffectOnGoingIdle.NotSet;

        public DateTime _startTime;
        public string _startDetails;

        public DateTime _endTime;
        public string _endDetails;
        public string _endReason;

        public TimeSpan _duration;
        public long _activityId;

        protected static Debugger debugger = Debugger.Instance;
        private static ActivityMonitor monitor = ActivityMonitor.Instance;
        
        public Activity() { }

        public Activity(ActivityMonitor.ActivityType type)
        {
            _type = type;
            _startTime = DateTime.UtcNow;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                $"Activity: {_type} created. start: {_startTime.ToMySqlDateTime()}");
            #endregion
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Activity objAsActivity = obj as Activity;
            if (objAsActivity == null)
                return false;
            else
                return Equals(objAsActivity);
        }

        public override int GetHashCode()
        {
            return (int) _type;
        }

        public bool Equals(Activity other)
        {
            if (other == null)
                return false;
            return (_type.Equals(other._type));
        }

        public void EmitStart()
        {
            if (_startTime == DateTime.MinValue)
                _startTime = DateTime.UtcNow;

            _annotation = _startDetails;
            string sql = 
                "insert into activities(time, line, code, text, tags) " + 
                string.Format($"values('{_startTime.ToMySqlDateTime()}', '{_line}', '{_code}', '{_annotation}', '{_tags.ToCSV()}')");

            try
            {
                using (var sqlConn = new MySqlConnection(ActivityMonitor.Tracer.MySqlActivitiesConnectionString))
                {
                    sqlConn.Open();                    
                    using (var sqlCmd = new MySqlCommand(sql, sqlConn))
                    {
                        sqlCmd.ExecuteNonQuery();
                        _activityId = sqlCmd.LastInsertedId;
                    }
                }
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Activity.EmitStart: \nsql: {0}\n failed: {1}", sql, ex.StackTrace);
                #endregion
            }
        }

        public void EmitEnd()
        {
            string endState = _endState.ToString();

            if (_tags[0] == "Idler" && _tags[1] == "GoingIdle" && endState == "Aborted")
            {
                if (Idler.Instance.Idle)
                    return;
                endState = "Stopped";
            }

            if (_tags[_tags.Count - 1] == "InProgress")
                _tags.RemoveAt(_tags.Count - 1);
            _tags.Add(endState);

            if (_endTime == DateTime.MinValue)
                _endTime = DateTime.UtcNow;

            _duration = _endTime > _startTime ? _endTime.Subtract(_startTime) : TimeSpan.Zero;
            string s = _duration.ToMinimalString();
            if (s != null)
                _annotation += string.Format($"Duration: {s}\n");
            _annotation += _endDetails +
                string.Format($"End state: {endState}\n") +
                string.Format($"End reason: {_endReason}\n");

            string sql = string.Format($@"update activities set text='{_annotation}', tags='{_tags.ToCSV()}' where id={_activityId};");
            sql += string.Format($"insert into activities(time, code, text, line, tags) values('{_endTime.ToMySqlDateTime()}', '{ActivityMonitor.Tracer.resetValue}', '{_annotation}', '{_line}', '{_tags.ToCSV()}');");

            try
            {
                using (var sqlConn = new MySqlConnection(ActivityMonitor.Tracer.MySqlActivitiesConnectionString))
                {
                    sqlConn.Open();
                    using (var sqlCmd = new MySqlCommand(sql, sqlConn))
                    {
                        sqlCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"Activity.EmitEnd: \nsql: {sql}\n caught: {ex.Message} at {ex.StackTrace}");
                #endregion
            }
        }

        public void EndActivity(GenericEndParams par)
        {
            if (par.endState == State.NotSet)
                throw new InvalidValueException(string.Format($"Activity:EndActivity: Activity {_type}: endState NOT set"));
            if (par.endReason == string.Empty)
                throw new InvalidValueException(string.Format($"Activity:EndActivity: Activity {_type}: endReason NOT set"));

            _endTime = par.endTime == DateTime.MinValue ? DateTime.UtcNow : par.endTime;
            _endState = par.endState;
            _endReason = par.endReason;

            EmitEnd();

            if (monitor.inProgressDict.ContainsKey(_type) && !monitor.inProgressDict.TryRemove(_type, out Activity ret))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"!!! ActivityMonitor:EndActivity: Could not remove {_type} from inProgressDict");
                #endregion
                throw new InvalidOperationException(string.Format($"Could not remove {_type} from inProgressDict"));
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                $"ActivityMonitor:EndActivity: removed {_type} from inProgressDict => [{monitor.inProgressDict.ToCSV()}]");
            #endregion

            if (monitor.inProgressDict.Count() == 0 && effectOnGoingIdle_AtEnd == EffectOnGoingIdle.Renew)
                ActivityMonitor.idler.StartGoingIdle(new Idler.StartParams() { reason = "no activities in progress" });
        }

        public class GenericStartParams
        {
            public DateTime startTime = DateTime.MinValue;
        }

        public class GenericEndParams
        {
            public Activity.State endState = State.NotSet;  // How did the activity end?
            public string endReason = string.Empty;         // Why did the activity end?
            public DateTime endTime = DateTime.MinValue;
        }

        public abstract void End(GenericEndParams par);

        public abstract class TimeConsuming : Activity
        {
            public TimeConsuming(ActivityMonitor.ActivityType type) : base(type)
            {
                if (type == ActivityMonitor.ActivityType.DomeTracking)
                {
                    effectOnGoingIdle_AtStart = EffectOnGoingIdle.NoEffect;
                    effectOnGoingIdle_AtEnd = EffectOnGoingIdle.NoEffect;
                }
                else
                {
                    effectOnGoingIdle_AtStart = EffectOnGoingIdle.Remove;
                    effectOnGoingIdle_AtEnd = EffectOnGoingIdle.Renew;
                }
            }

            public override void End(GenericEndParams par)
            {
                End(par);
            }
        }

        public class TelescopeSlew : TimeConsuming
        {
            public class Coords
            {
                public double ra, dec;
            };

            public Coords _start, _target, _end;

            public class StartParams: Activity.GenericStartParams
            {
                public Coords start, target;
            }

            public class EndParams : GenericEndParams
            {
                public Coords end;
            }

            public override void End(GenericEndParams p)
            {
                TelescopeSlew.EndParams par = p as TelescopeSlew.EndParams;

                _end = new Coords() {
                    ra = par.end.ra,
                    dec = par.end.dec,
                };
                _endDetails = string.Format($"End: {Angle.FromHours(_end.ra, Angle.Type.RA)}, {Angle.FromDegrees(_end.dec, Angle.Type.Dec)}\n");
                EndActivity(par);
            }

            public TelescopeSlew(StartParams par) : base(ActivityMonitor.ActivityType.TelescopeSlew)
            {
                _start = new Coords() {
                    ra = par.start.ra,
                    dec = par.start.dec,
                };
                _target = new Coords()
                {
                    ra = par.target.ra,
                    dec = par.target.dec,
                };
                _code = (int) ActivityMonitor.Tracer.Telescope.Code.Slewing;
                _line = ActivityMonitor.Tracer.Telescope.telescope.Line;
                _tags = new List<string>() { "Telescope", "Slew", "InProgress" };
                _startDetails = 
                    string.Format($"Start: {Angle.FromHours(_start.ra, Angle.Type.RA)}, {Angle.FromDegrees(_start.dec, Angle.Type.Dec)}\n") + 
                    string.Format($"Target: {Angle.FromHours(_target.ra, Angle.Type.RA)}, {Angle.FromDegrees(_target.dec, Angle.Type.Dec)}\n");

                EmitStart();
            }
        }

        public class DomeSlew : TimeConsuming
        {
            public enum DomeEventType { FindHome, Slew, Tracking };
            public double _startAz, _targetAz, _endAz;
            public string _reason;

            public class StartParams : Activity.GenericStartParams
            {
                public DomeEventType type;
                public double startAz, targetAz;
                public string reason;
            }

            public class EndParams : Activity.GenericEndParams
            {
                public double endAz;
            }

            public DomeSlew(StartParams par): base(ActivityMonitor.ActivityType.DomeSlew)
            {
                _line = ActivityMonitor.Tracer.Dome.dome.Line;
                if (par.type == DomeEventType.FindHome)
                {
                    _code = (int) ActivityMonitor.Tracer.Dome.Code.FindingHome;
                    _tags = new List<string>() { "Dome", "FindHome", "InProgress" };
                    _reason = "FindHome";
                    _startDetails = "Reason: " + _reason;
                }
                else if (par.type == DomeEventType.Tracking || par.type == DomeEventType.Slew)
                {
                    _startAz = par.startAz;
                    _targetAz = par.targetAz;
                    _reason = par.reason;
                    _tags = new List<string>() { "Dome" };
                    if (par.type == DomeEventType.Tracking)
                    {
                        _code = (int) ActivityMonitor.Tracer.Dome.Code.Tracking;
                        _tags.Add("Tracking");
                    } else
                    {
                        _code = (int) ActivityMonitor.Tracer.Dome.Code.Slewing;
                        _tags.Add("Slew");
                    }
                    _tags.Add("InProgress");

                    _startDetails =
                        string.Format($"Start: {Angle.FromDegrees(_startAz, Angle.Type.Az).ToNiceString()}\n") +
                        string.Format($"Target: {Angle.FromDegrees(_targetAz, Angle.Type.Az).ToNiceString()}\n") +
                        string.Format($"Reason: {_reason}\n");
                }

                EmitStart();
            }

            public override void End(Activity.GenericEndParams p)
            {
                DomeSlew.EndParams par = p as DomeSlew.EndParams;

                _endAz = par.endAz;
                _endDetails = string.Format($"End: {Angle.FromDegrees(_endAz, Angle.Type.Az).ToNiceString()}\n");
                EndActivity(par);
            }
        }

        public class Focuser : TimeConsuming
        {
            public enum Direction { NotSet, Up, Down };
            public Direction _direction;
            public int _start, _target, _end;

            public class StartParams : Activity.GenericStartParams
            {
                public int start, target, intermediateTarget;
                public Direction direction;
            }

            public class EndParams : Activity.GenericEndParams
            {
                public int end;
            }

            public Focuser(StartParams par): base(ActivityMonitor.ActivityType.Focuser)
            {
                _code = (int) ActivityMonitor.Tracer.Focuser.Code.Moving;
                _line = ActivityMonitor.Tracer.Focuser.focuser.Line;
                _tags = new List<string>() { "Focuser", "InProgress" };
                _direction = par.direction;
                _target = par.target;
                _start = par.start;
                _startDetails = string.Format($"Start: {_start}\n Target: {_target}\n Direction: {_direction}");

                EmitStart();
            }

            public override void End(Activity.GenericEndParams p)
            {
                Focuser.EndParams par = p as Focuser.EndParams;

                _endDetails = string.Format($"End: {par.end}\n");
                EndActivity(par);
            }
        }

        public class Shutter : TimeConsuming
        {
            public int _start, _target, _end;
            public ASCOM.DeviceInterface.ShutterState _operation;

            public class StartParams : Activity.GenericStartParams
            {
                public int start, target;
                public ASCOM.DeviceInterface.ShutterState operation;
            }

            public class EndParams : Activity.GenericEndParams
            {
                public int percentOpen;
            }

            public Shutter(StartParams par): base (ActivityMonitor.ActivityType.Shutter)
            {
                _operation = par.operation;
                string op = _operation.ToString().Remove(0, "shutter".Length);

                switch (_operation)
                {
                    case DeviceInterface.ShutterState.shutterOpening:
                        _code = (int) ActivityMonitor.Tracer.Shutter.Code.Opening;
                        break;
                    case DeviceInterface.ShutterState.shutterOpen:
                        _code = (int) ActivityMonitor.Tracer.Shutter.Code.Open;
                        break;
                    case DeviceInterface.ShutterState.shutterClosing:
                        _code = (int) ActivityMonitor.Tracer.Shutter.Code.Closing;
                        break;
                    case DeviceInterface.ShutterState.shutterClosed:
                        _code = (int) ActivityMonitor.Tracer.resetValue;
                        break;
                }
                _line = ActivityMonitor.Tracer.Shutter.shutter.Line;
                _tags = new List<string>() { "Shutter", op, "InProgress" };
                _start = par.start;
                _target= par.target;
                _startDetails =
                    string.Format($"Start: {_start}%\n") +
                    string.Format($"Target: {_target}%\n") +
                    string.Format($"Operation: {op}\n");

                EmitStart();
            }

            public override void End(Activity.GenericEndParams p)
            {
                Shutter.EndParams par = p as Shutter.EndParams;

                _end = par.percentOpen;
                _endDetails = string.Format($"End: {_end}%\n");

                EndActivity(par);
            }
        }

        public class PulsingEndParams :  Activity.GenericEndParams
        {
            public TelescopeSlew.Coords _end;
        }

        public class PulsingRa : TimeConsuming
        {
            public DeviceInterface.GuideDirections _direction;
            public int _millis;
            public TelescopeSlew.Coords _start, _end;
            public TelescopeAxes _axis = TelescopeAxes.axisPrimary;

            public class StartParams : Activity.GenericStartParams
            {
                public TelescopeSlew.Coords _start;
                public DeviceInterface.GuideDirections _direction;
                public int _millis;
            }

            public PulsingRa(StartParams par): base(ActivityMonitor.ActivityType.PulsingRa)
            {
                _line = ActivityMonitor.Tracer.Telescope.telescope.Line; ;
                _code = (int) ActivityMonitor.Tracer.Telescope.Code.Pulsing;
                _tags = new List<string>() { "Telescope", "PulsingRa", "InProgress" };
                _direction = par._direction;
                _millis = par._millis;
                _start = new TelescopeSlew.Coords()
                {
                    ra = par._start.ra,
                    dec = par._start.dec,
                };

                _startDetails =
                    string.Format($"Start: {Angle.FromHours(_start.ra, Angle.Type.RA)}, {Angle.FromDegrees(_start.dec, Angle.Type.Dec)}\n") +
                    string.Format($"Axis: Ra\n") + 
                    string.Format($"Direction: {_direction.ToString().Remove(0, "guide".Length)}\n") +
                    string.Format($"Millis: {_millis}\n");

                EmitStart(); ;
            }

            public override void End(Activity.GenericEndParams p)
            {
                PulsingEndParams par = p as PulsingEndParams;

                _end = new TelescopeSlew.Coords()
                {
                    ra = par._end.ra,
                    dec = par._end.dec,
                };
                _endDetails = string.Format($"End: {Angle.FromHours(_end.ra, Angle.Type.RA)}, {Angle.FromDegrees(_end.dec, Angle.Type.Dec)}\n");

                EndActivity(par);
            }
        }
        public class PulsingDec : TimeConsuming
        {
            public DeviceInterface.GuideDirections _direction;
            public int _millis;
            public TelescopeSlew.Coords _start, _end;
            public TelescopeAxes _axis = TelescopeAxes.axisSecondary;

            public class StartParams : Activity.GenericStartParams
            {
                public TelescopeSlew.Coords _start;
                public DeviceInterface.GuideDirections _direction;
                public int _millis;
            }

            public PulsingDec(StartParams par) : base(ActivityMonitor.ActivityType.PulsingDec)
            {
                _line = ActivityMonitor.Tracer.Telescope.telescope.Line; ;
                _code = (int)ActivityMonitor.Tracer.Telescope.Code.Pulsing;
                _tags = new List<string>() { "Telescope", "PulsingDec", "InProgress" };
                _direction = par._direction;
                _millis = par._millis;
                _start = new TelescopeSlew.Coords()
                {
                    ra = par._start.ra,
                    dec = par._start.dec,
                };

                _startDetails =
                    string.Format($"Start: {Angle.FromHours(_start.ra, Angle.Type.RA)}, {Angle.FromDegrees(_start.dec, Angle.Type.Dec)}\n") +
                    string.Format($"Axis: Dec\n") +
                    string.Format($"Direction: {_direction.ToString().Remove(0, "guide".Length)}\n") +
                    string.Format($"Millis: {_millis}\n");

                EmitStart(); ;
            }

            public override void End(Activity.GenericEndParams p)
            {
                PulsingEndParams par = p as PulsingEndParams;

                _end = new TelescopeSlew.Coords()
                {
                    ra = par._end.ra,
                    dec = par._end.dec,
                };
                _endDetails = string.Format($"End: {Angle.FromHours(_end.ra, Angle.Type.RA)}, {Angle.FromDegrees(_end.dec, Angle.Type.Dec)}\n");

                EndActivity(par);
            }
        }

        public class FilterWheel : TimeConsuming
        {
            public enum Operation { Detect, Move };
            public const int UnknownPosition = int.MinValue;

            public class StartParams : Activity.GenericStartParams
            {
                public Operation operation;
                public string startWheel;
                public int startPosition, targetPosition;
            }

            public class EndParams: Activity.GenericEndParams
            {
                public string endWheel;
                public int endPosition;
                public string endTag;
            }

            public FilterWheel(StartParams par) : base(ActivityMonitor.ActivityType.FilterWheel)
            {
                _line = ActivityMonitor.Tracer.FilterWheel.filterwheel.Line;
                _code = (int) ActivityMonitor.Tracer.FilterWheel.Code.Moving;
                _tags = new List<string>() { "FilterWheel", par.operation.ToString(), "InProgress" };
                _startDetails = string.Format($"Start: {par.operation}\n");
                _startDetails +=
                    string.Format(" Wheel: {0}\n", par.startWheel == null ? "none" : par.startWheel) +
                    string.Format(" Position: {0}\n", par.startPosition == FilterWheel.UnknownPosition ? 
                        "none" : par.startPosition.ToString());
                if (par.operation == Operation.Move)
                    _startDetails += string.Format($" Target: {par.targetPosition}\n");

                EmitStart();
            }

            public override void End(Activity.GenericEndParams p)
            {
                FilterWheel.EndParams par = p as FilterWheel.EndParams;

                _endDetails = "End:\n" +
                    string.Format($"Wheel: {par.endWheel}\n") +
                    string.Format("Position: {0}\n", par.endPosition == FilterWheel.UnknownPosition ? 
                        "none" : par.endPosition.ToString()) +
                    string.Format($"Tag: {par.endTag}\n");

                EndActivity(par);
            }
        }

        public class Park : TimeConsuming
        {
            public TelescopeSlew.Coords _start, _target, _end;
            public double _startAz, _targetAz, _endAz;
            public int _shutterPercentStart, _shutterPercentEnd;
            public ASCOM.DeviceInterface.ShutterState _shutterEndState;

            public class StartParams: Activity.GenericStartParams
            {
                public TelescopeSlew.Coords start, target;
                public double domeStartAz, domeTargetAz;
                public int shutterPercent;
            }

            public class EndParams : Activity.GenericEndParams
            {
                public TelescopeSlew.Coords end;
                public double domeAz;
                public int shutterPercent;
            }

            public Park(StartParams par): base(ActivityMonitor.ActivityType.Parking)
            {
                _start = new TelescopeSlew.Coords()
                {
                    ra = par.start.ra,
                    dec = par.start.dec,
                };
                _startAz = par.domeStartAz;
                _shutterPercentStart = par.shutterPercent;

                _target = new TelescopeSlew.Coords()
                {
                    ra = par.target.ra,
                    dec = par.start.dec,
                };
                _targetAz = par.domeTargetAz;
                _code = (int) ActivityMonitor.Tracer.Parking.Code.Active;
                _line = "parking";
                _tags = new List<string>() { "Parking", "InProgress" };

                _startDetails = "Start:\n" +
                    string.Format($" Telescope: {Angle.FromHours(_start.ra, Angle.Type.RA)}, {Angle.FromDegrees(_start.dec, Angle.Type.Dec)}\n") +
                    string.Format($" Dome: {Angle.FromDegrees(_startAz, Angle.Type.Az)}\n") +
                    string.Format($" Shutter: {_shutterPercentStart}%\n") +
                    "Target:\n" +
                    string.Format($" Telescope: {Angle.FromHours(_target.ra, Angle.Type.RA)}, {Angle.FromDegrees(_target.dec, Angle.Type.Dec)}\n") +
                    string.Format($" Dome: {Angle.FromDegrees(_targetAz, Angle.Type.Az)}\n") +
                    string.Format($" Shutter: 100%\n");

                EmitStart();
            }

            public override void End(Activity.GenericEndParams p)
            {
                Park.EndParams par = p as Park.EndParams;

                _end = new TelescopeSlew.Coords()
                {
                    ra = par.end.ra,
                    dec = par.end.dec,
                };
                _endAz = par.domeAz;
                _shutterPercentEnd = par.shutterPercent;

                _endDetails = "End:\n" +
                    string.Format($" Telescope: {Angle.FromHours(_end.ra, Angle.Type.RA)}, {Angle.FromDegrees(_end.dec, Angle.Type.Dec)}\n") +
                    string.Format($" Dome: {Angle.FromDegrees(_endAz, Angle.Type.Az)}\n") +
                    string.Format($" Shutter: {_shutterPercentEnd}%\n");

                EndActivity(par);
            }
        }

        public class Shutdown : TimeConsuming
        {
            public string _reason;

            public class StartParams: Activity.GenericStartParams
            {
                public string reason;
            }

            public Shutdown(StartParams par): base(ActivityMonitor.ActivityType.ShuttingDown)
            {
                _reason = par.reason;
                _code = (int) ActivityMonitor.Tracer.Shutdown.Code.Active;
                _line = ActivityMonitor.Tracer.Shutdown.shutdown.Line;
                _tags = new List<string>() { "Shutdown", "InProgress" };

                _startDetails = string.Format($"Reason: {_reason}\n");
                effectOnGoingIdle_AtStart = EffectOnGoingIdle.Remove;
                effectOnGoingIdle_AtEnd = EffectOnGoingIdle.Remove;
                EmitStart();
            }

            public override void End(Activity.GenericEndParams p)
            {
                Shutdown.GenericEndParams par = p as Shutdown.GenericEndParams;

                EndActivity(par);
                Idler.BecomeIdle("End of Shuttdown");
            }
        }

        public class Handpad : TimeConsuming
        {
            public DeviceInterface.TelescopeAxes _axis;
            public double _rate;
            public double _start, _end;

            public class StartParams : Activity.GenericStartParams
            {
                public DeviceInterface.TelescopeAxes axis;
                public double rate;
                public double start;
            }

            public class EndParams : Activity.GenericEndParams
            {
                public double end;
            }

            public Handpad(StartParams par) : base(ActivityMonitor.ActivityType.Handpad)
            {
                _axis = par.axis;
                _rate = par.rate;
                Angle a = (_axis == DeviceInterface.TelescopeAxes.axisPrimary) ?
                        Angle.FromHours(_start, Angle.Type.RA) :
                        Angle.FromDegrees(_start, Angle.Type.Dec);

                _startDetails = string.Format($"Axis: {_axis.ToString().Remove(0, "rate".Length)}\n") +
                    string.Format($"Start: {a.ToString()}\n") +
                    string.Format($"Rate: {RateName(_rate)}\n");

                EmitStart(); ;
            }

            public override void End(Activity.GenericEndParams p)
            {
                Handpad.EndParams par = p as Handpad.EndParams;

                _end = par.end;
                Angle a = (_axis == DeviceInterface.TelescopeAxes.axisPrimary) ?
                        Angle.FromHours(_end, Angle.Type.RA) :
                        Angle.FromDegrees(_end, Angle.Type.Dec);

                _endDetails = string.Format($"End: {a}");
            }

            public static string RateName(double rate)
            {
                Dictionary<double, string> names = new Dictionary<double, string> {
                { Const.rateStopped,  "rateStopped" },
                { Const.rateSlew,  "rateSlew" },
                { Const.rateSet,  "rateSet" },
                { Const.rateGuide,  "rateGuide" },
                { -Const.rateSlew,  "-rateSlew" },
                { -Const.rateSet,  "-rateSet" },
                { -Const.rateGuide,  "-rateGuide" },
                { Const. rateTrack, "rateTrack" },
            };

                if (names.ContainsKey(rate))
                    return names[rate];
                return rate.ToString();
            }
        }
    }

    public sealed class Idler : Activity
    {
        public DateTime _due;
        private System.Threading.Timer _timer = new System.Threading.Timer(onTimer);
        public string startReason, endReason;
        public enum IdlerState { GoingIdle, Idle, ActivitiesInProgress }
        private IdlerState _idlerState;

        // start Singleton
        private static readonly Lazy<Idler> lazy = new Lazy<Idler>(() => new Idler()); // Singleton

        public static Idler Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
            }
        }

        private Idler() { }
        static Idler() { }
        // end Singleton

        public class StartParams: GenericStartParams
        {
            public string reason;
        }

        public class EndParams : GenericEndParams
        {
        }

        public override void End(GenericEndParams endParams) { }

        public void init()
        {
            //StartGoingIdle(new StartParams() { reason = "Initial GoingIdle" });
            _idlerState = IdlerState.Idle;
        }

        public void AbortGoingIdle(EndParams par)
        {
            if (_idlerState != IdlerState.GoingIdle)
                return;

            _due = DateTime.MinValue;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            IdlerState prevState = _idlerState;
            _idlerState = ActivityMonitor.ObservatoryActivities.Count == 0 ? IdlerState.Idle : IdlerState.ActivitiesInProgress;

            if (prevState == IdlerState.GoingIdle)
                EndActivity(new EndParams
                    {
                        endReason = par.endReason,
                        endState = Activity.State.Aborted,
                    });
        }

        public void StayActive(StartParams par)
        {
            if (_idlerState == IdlerState.GoingIdle)
            {
                AbortGoingIdle(new EndParams() { endReason = par.reason, endState = State.Aborted }); 
            }
            StartGoingIdle(par);
        }

        /// <summary>
        /// Start GoingIdle.  This can end either by Abortion (when
        ///  an activity starts, End() is called) or by Success (when
        ///  the timer expires (BecomeIdle is called))
        /// </summary>
        /// <param name="reason"></param>
        public void StartGoingIdle(StartParams par)
        {
            _type = ActivityMonitor.ActivityType.GoingIdle;
            _code = (int) ActivityMonitor.Tracer.Idler.Code.GoingIdle;
            _line = ActivityMonitor.Tracer.Idler.idler.Line;
            _tags = new List<string>() { "Idler", "GoingIdle", "InProgress" };

            _startTime = par.startTime == DateTime.MinValue ? DateTime.UtcNow : par.startTime;
            startReason = par.reason;
            _startDetails = string.Format("Started GoingIdle because: {0}\n", startReason);
            EmitStart();

            _idlerState = IdlerState.GoingIdle;
            if (ActivityMonitor.millisToInactivity == 0)
                ActivityMonitor.Instance.init();
            _due = DateTime.Now.AddMilliseconds(ActivityMonitor.millisToInactivity);
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer.Change(ActivityMonitor.millisToInactivity, Timeout.Infinite);
        }

        private static void onTimer(object state)
        {
            BecomeIdle(string.Format("No activity for {0} seconds", ActivityMonitor.millisToInactivity / 1000));
        }

        public static void BecomeIdle(string reason = "")
        {
            ActivityMonitor.idler.EndActivity(new EndParams()
            {
                endState = Activity.State.Idle,
                endReason = reason,
            });

            ActivityMonitor.Instance.Event(
                new Event.IdlerEvent("Wise40 is idle", (int) ActivityMonitor.Tracer.Idler.Code.Idle));

            Instance._idlerState = IdlerState.Idle;
        }

        public TimeSpan RemainingTime
        {
            get
            {
                if (_idlerState == IdlerState.GoingIdle)
                    return _due.Subtract(DateTime.Now);
                return TimeSpan.MaxValue;
            }
        }

        public string Status
        {
            get
            {
                string ret = "";

                if (_idlerState == IdlerState.GoingIdle)
                {
                    TimeSpan ts = RemainingTime;

                    if (ts != TimeSpan.MaxValue)
                    {
                        ret = "GoingIdle in ";
                        if (ts.TotalMinutes > 0)
                            ret += string.Format("{0:D2}m", (int)ts.TotalMinutes);
                        ret += string.Format("{0:D2}s", ts.Seconds);
                    }
                }

                return ret;
            }
        }

        public bool Idle
        {
            get
            {
                return _idlerState == IdlerState.Idle;
            }
        }
    }

    public class Event
    {
        public enum EventType { NotSet, Safety, Generic };

        public EventType _type = EventType.NotSet;
        public DateTime _time;
        public string _annotation;
        public List<string> _tags;
        public int _code;
        public string _line;

        private static Debugger debugger = Debugger.Instance;

        public Event(EventType type)
        {
            _type = type;
            _time = DateTime.UtcNow;
            _tags = new List<string>() { _type.ToString() };
        }

        public void EmitEvent()
        {
            string sql = "insert into activities(time, text, tags, code, line) " +
             string.Format($"values('{_time.ToMySqlDateTime()}', '{_annotation + "\n"}', '{_tags.ToCSV()}', {_code}, '{_line}');");

            try
            {
                using (var sqlConn = new MySqlConnection(ActivityMonitor.Tracer.MySqlActivitiesConnectionString))
                {
                    sqlConn.Open();
                    using (var sqlCmd = new MySqlCommand(sql, sqlConn))
                    {
                        sqlCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Activity.EmitStart: \nsql: {0}\n failed: {1}", sql, ex.StackTrace);
                #endregion
            }
        }

        public class SafetyEvent : Event
        {
            public enum SafetyState { Unknown, Safe, Unsafe }
            public enum SensorState {  NotSet, Init, NotReady, Ready, Safe, NotSafe };
            public string _sensor;
            public SensorState _before = SensorState.NotSet;
            public SensorState _after = SensorState.NotSet;
            public SafetyState _safetyState;

            public SafetyEvent() : base (EventType.NotSet) { }
            static SafetyEvent() { }

            public static SensorState ToSensorSafety(bool b)
            {
                return b ? SensorState.Safe : SensorState.NotSafe;
            }

            public SafetyEvent(string sensor, string details, SensorState before, SensorState after) : base(EventType.Safety)
            {
                if (sensor == string.Empty)
                    throw new InvalidValueException("SensorSafetyEvent:ctor: empty \"sensor\"");
                if (details == string.Empty)
                    throw new InvalidValueException(string.Format("SensorSafetyEvent:ctor: empty \"details\" for {0}", sensor));
                if (after == SensorState.NotSet)
                    throw new InvalidValueException(string.Format("SensorSafetyEvent:ctor: \"after\" NotSet for {0}", sensor));

                _sensor = sensor;
                _before = before;
                _after = after;

                _code = 0;
                _annotation = string.Format($"Sensor: {sensor}\nEvent: {details}\nBefore: {before}\nAfter: {after}");
                _tags.Add(sensor);
                _tags.Add(before.ToString() + "->" + after.ToString());
                _line = ActivityMonitor.Tracer.safety.Line;
            }

            public SafetyEvent(SafetyState newState, string reason = "") : base(EventType.Safety)
            {
                _safetyState = newState;
                _code = _safetyState == SafetyState.Safe ?
                    ActivityMonitor.Tracer.resetValue :
                    (int) ActivityMonitor.Tracer.Safety.Code.NotSafe;
                _line = ActivityMonitor.Tracer.safety.Line;
                _annotation = string.Format("New state: {0}\n", _safetyState.ToString());
                if (reason != "")
                    _annotation += string.Format($"Reason: {reason}\n");
                _tags = new List<string>() { "Safety", _safetyState.ToString() };
            }
        }

        public class GlobalEvent : Event
        {
            public GlobalEvent(string details) : base(EventType.Generic)
            {
                if (details == string.Empty)
                    throw new InvalidValueException("GlobalEvent:ctor: bad args");

                _annotation = details;
            }
        }

        public class IdlerEvent : Event
        {
            public IdlerEvent(string details, int code = ActivityMonitor.Tracer.resetValue) : base(EventType.Generic)
            {
                if (details == string.Empty)
                    throw new InvalidValueException("IdlerEvent:ctor: bad args");

                _annotation = details;
                _line = ActivityMonitor.Tracer.idler.Line;
                _code = code;
                _tags = new List<string>() { "Idler" };
            }
        }

        public class ProjectorEvent : Event
        {
            public ProjectorEvent(bool value) : base(EventType.Generic)
            {
                string s = value ? "On" : "Off";

                _annotation = string.Format($"Projector {s}");
                _line = ActivityMonitor.Tracer.projector.Line;
                _code = value ? (int) ActivityMonitor.Tracer.Projector.Code.On : ActivityMonitor.Tracer.resetValue;
                _tags = new List<string>() { "Projector", s };
            }
        }

        public class ShutterEvent : Event
        {
            public ShutterEvent(bool value) : base(EventType.Generic)
            {
                string s = value ? "Open" : "Closed";

                _annotation = string.Format($"Shutter {s}");
                _line = ActivityMonitor.Tracer.shutter.Line;
                _code = value ? (int)ActivityMonitor.Tracer.Shutter.Code.Open : ActivityMonitor.Tracer.resetValue;
                _tags = new List<string>() { "Shutter", s };
            }
        }

        public class TrackingEvent : Event
        {
            public TrackingEvent(bool value) : base(EventType.Generic)
            {
                string s = value ? "On" : "Off";

                _annotation = string.Format($"Tracking {s}");
                _line = ActivityMonitor.Tracer.tracking.Line;
                _code = value ? (int)ActivityMonitor.Tracer.Tracking.Code.On : ActivityMonitor.Tracer.resetValue;
                _tags = new List<string>() { "Tracking", s };
            }
        }

        public class DriverConnectEvent : Event
        {
            public DriverConnectEvent(string driverName, bool connected, string line = "") : base(EventType.Generic)
            {
                if (driverName == string.Empty || line == "" || driverName == "")
                    throw new InvalidValueException("DriverConnectEvent:ctor: bad args");

                string con = connected ? "Connected" : "Disconnected";
                _annotation = string.Format("{0} {1}\n", driverName, con);
                _line = line;
                _code = ActivityMonitor.Tracer.resetValue;
                _tags = new List<string>() { "Driver", driverName, con };
            }
        }
    }
}
