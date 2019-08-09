using System;
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

namespace ASCOM.Wise40
{
    public sealed class ActivityMonitor : WiseObject
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
        public int _mongoDbId = 0;

        [FlagsAttribute]
        public enum ActivityType
        {
            None = 0,
            TelescopeSlew = (1 << 1),
            Pulsing = (1 << 2),
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

            // activities that affect the observatory's Idle state
            RealActivities = TelescopeSlew | Pulsing | DomeSlew | Handpad | Parking | Shutter | Focuser | FilterWheel | ShuttingDown,
        };


        private static List<ActivityType> _activities = new List<ActivityType> {
            ActivityType.TelescopeSlew,
            ActivityType.Pulsing,
            ActivityType.DomeSlew,
            ActivityType.Handpad,
            ActivityType.GoingIdle,
            ActivityType.Parking,
            ActivityType.Shutter,
            ActivityType.ShuttingDown,
        };


        //public static IMongoDatabase db;

        //public static IMongoCollection<BsonDocument> ActivitiesCollection
        //{
        //    get
        //    {
        //        string collectionName = Debugger.LogDirectory().Remove(0, (Const.topWise40Directory + "Logs/").Length);

        //        if (db == null)
        //            db = (new MongoClient()).GetDatabase("activities");
        //        return db.GetCollection<BsonDocument>(collectionName);
        //    }
        //}


        public void init()
        {
            if (!WiseSite.CurrentProcessIsASCOMServer)
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

            //db = (new MongoClient()).GetDatabase("activities");

            Event(new Event.SafetyEvent(Wise40.Event.SafetyEvent.SafetyState.Unknown));

            initialized = true;
        }

        public void EndActivity(ActivityType type, Activity.EndParams p)
        {
            Activity activity = LookupInProgress(type);
            if (activity == null)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ActivityMonitor:EndActivity: No \"{0}\" inProgress", type.ToString());
                #endregion
                return;
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ActivityMonitor:EndActivity: Calling {0}.End()", type.ToString());
            #endregion
            activity._endTime = DateTime.Now;
            activity.End(p);
        }

        public bool ShuttingDown
        {
            get
            {
                bool ret;

                lock (inProgressActivitiesLock)
                {
                    ret = (inProgressActivities.Find((x) => x._type == ActivityType.ShuttingDown) != default(Activity));
                }
                return ret;
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
            idler.StartGoingIdle(reason);
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
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ActivityMonitor:ObservatoryIsActive: ret: {0} [{1}]",
                ret, string.Join(",", ObservatoryActivities));
            #endregion
            return ret;
        }

        public static List<string> ObservatoryActivities
        {
            get
            {
                List<string> ret = new List<string>();

                lock (inProgressActivitiesLock)
                {
                    string status = idler.Status;
                    if (status.StartsWith("GoingIdle"))
                        ret.Add(status);

                    foreach (Activity a in Instance.inProgressActivities)
                    {
                        if ((a._type & ActivityMonitor.ActivityType.RealActivities) != 0)
                            ret.Add(a._type.ToString());
                    }
                }

                return ret;
            }
        }

        public List<Activity> activityLog = new List<Activity>();
        public List<Activity> inProgressActivities = new List<Activity>();
        public List<Activity> endedActivities = new List<Activity>();
        public static object inProgressActivitiesLock = new object();
        public static Idler idler = Idler.Instance;

        public Activity LookupInProgress(ActivityType type)
        {
            Activity ret;
            string dbg;

            lock (inProgressActivitiesLock)
            {
                #region debug
                dbg = string.Format("ActivityMonitor:LookupInProgress: type: {0}, in progress: [", type);
                foreach (Activity a in inProgressActivities)
                    dbg += string.Format(" {0} ({1})", a._type.ToString(), a.GetHashCode());
                dbg += " ] ";
                #endregion
                ret = inProgressActivities.Find((x) => x._type == type);
            }

            #region debug
            dbg += (ret == default(Activity) ? "NOT found" : "found");
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, dbg);
            #endregion
            return ret;
        }

        public void NewActivity(Activity activity)
        {
            activityLog.Add(activity);
            lock (inProgressActivitiesLock)
            {
                if (!inProgressActivities.Contains(activity))
                    inProgressActivities.Add(activity);
            }

            if (activity.effectOnGoingIdle_AtStart == Activity.EffectOnGoingIdle.Remove)
            {
                string reason = string.Format("Activity {0} started", activity._type.ToString());

                idler.AbortGoingIdle(reason);
            }
        }

        public void Event(Event e)
        {
            e.EmitEvent();
        }
    }

    public abstract class Activity: IEquatable<Activity>
    {
        public ActivityMonitor.ActivityType _type;

        public class Code
        {
            public const int Idle = 0;
            public const int Shutdown = 10;
            public const int Parking = 20;
            public const int Focuser = 30;
            public const int FilterWheel = 40;
            public class Dome
            {
                public const int Slewing = 50;
                public const int Tracking = 60;
                public const int FindHome = 160;
            }
            public class Shutter
            {
                public const int Open = 70;
                public const int Opening = 80;
                public const int Closing = 90;
                public const int Closed = Idle;
            }
            public class Telescope
            {
                public const int Slewing = 100;
                public const int Pulsing = 110;
                public const int Tracking = 120;
            }
            public const int GoingIdle = 130;
            public const int Projector = 140;
            public const int NotSafe = 150;
        }

        public enum State { NotSet, Pending, Succeeded, Failed, Aborted, Idle };
        public State _endState;
        public int _code;
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
        public int _objectId;

        protected static Debugger debugger = Debugger.Instance;
        private static ActivityMonitor monitor = ActivityMonitor.Instance;

        public Activity() { }

        public Activity(ActivityMonitor.ActivityType type)
        {
            _type = type;
            _startTime = DateTime.Now;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                "Activity: \"{0}\" created. start: {1}", type.ToString(), _startTime.ToString(@"dd\:hh\:mm\:ss\.fff"));
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
            _annotation = _startDetails;
            string sql = 
                "insert into activities(time, code, text, tags) " + 
                string.Format("values('{0}', '{1}', '{2}', '{3}');",
                    _startTime.ToString(@"yyyy-MM-dd HH:mm:ss.fff"),
                    _code,
                    _annotation + "\n",
                    string.Join(",", _tags)) + 
                " select last_insert_id();";

            try
            {
                MySqlConnection sqlConn = new MySqlConnection("server=localhost;user=root;database=activities;port=3306;password=@!ab4131!@");
                sqlConn.Open();
                MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConn);
                _objectId = Convert.ToInt32(sqlCmd.ExecuteScalar());
                sqlCmd.Dispose();
                sqlConn.Close();
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
            _tags.RemoveAt(_tags.Count - 1);
            _tags.Add(_endState.ToString());

            _annotation +=
                string.Format("Duration: {0}\n", _duration) +
                string.Format("End:\n") +
                string.Format(" Details: {0}\n", _endDetails) +
                string.Format(" State: {0}\n", _endState.ToString()) +
                string.Format(" Reason: {0}\n", _endReason);

            string sql = string.Format(@"UPDATE activities SET text='{0}', tags='{1}' where id={2};",
                _annotation, string.Join(",", _tags), _objectId);

            sql += string.Format(@"INSERT into activities(time, code) VALUES({0}, {1});", _endTime, _code);

            try
            {
                MySqlConnection sqlConn = new MySqlConnection("server=localhost;user=root;database=activities;port=3306;password=@!ab4131!@");
                sqlConn.Open();
                MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConn);
                sqlCmd.ExecuteNonQuery();
                sqlCmd.Dispose();
                sqlConn.Close();
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Activity.EmitEnd: \nsql: {0}\n failed: {1}", sql, ex.StackTrace);
                #endregion
            }
        }

        public void EndActivity(EndParams par)
        {
            if (par.endState == State.NotSet)
                throw new InvalidValueException(string.Format("Activity:EndActivity: Activity {0}: endState NOT set", this._type.ToString()));
            if (par.endReason == string.Empty)
                throw new InvalidValueException(string.Format("Activity:EndActivity: Activity {0}: endReason NOT set", this._type.ToString()));

            _endTime = DateTime.Now;
            _endState = par.endState;
            _endReason = par.endReason;
            _duration = _endTime - _startTime;

            EmitEnd();

            monitor.endedActivities.Add(this);
            lock (ActivityMonitor.inProgressActivitiesLock)
            {
                monitor.inProgressActivities.Remove(this);
            }

            if (ActivityMonitor.ObservatoryActivities.Count() == 0 && effectOnGoingIdle_AtEnd == EffectOnGoingIdle.Renew)
                ActivityMonitor.idler.StartGoingIdle("no activities in progress");
        }

        public class EndParams
        {
            public Activity.State endState = State.NotSet;  // How did the activity end?
            public string endReason = string.Empty;         // Why did the activity end?
        }

        public abstract void End(EndParams par);

        public class TimeConsumingActivity : Activity
        {
            public TimeConsumingActivity(ActivityMonitor.ActivityType type) : base(type)
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

            public override void End(EndParams par)
            {
                End(par);
            }
        }

        public class TelescopeSlewActivity : TimeConsumingActivity
        {
            public class Coords
            {
                public double ra, dec;
            };

            public Coords _start, _target, _end;

            public class StartParams
            {
                public Coords start, target;
            }

            public new class EndParams : Activity.EndParams
            {
                public Coords end;
            }

            public override void End(Activity.EndParams p)
            {
                TelescopeSlewActivity.EndParams par = p as TelescopeSlewActivity.EndParams;

                _end = new Coords() {
                    ra = par.end.ra,
                    dec = par.end.dec,
                };
                _endDetails = string.Format("endRa: {0}, endDec: {1}",
                    Angle.FromHours(_end.ra).ToNiceString(),
                    Angle.FromDegrees(_end.dec).ToNiceString());
                EndActivity(par);
            }

            public TelescopeSlewActivity(StartParams par) : base(ActivityMonitor.ActivityType.TelescopeSlew)
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
                _code = Activity.Code.Telescope.Slewing;
                _tags = new List<string>() { "Telescope", "Slew", "InProgress" };
                _startDetails = 
                    string.Format("Start: {0}, {1}\n",
                        Angle.FromHours(_start.ra).ToNiceString(),
                        Angle.FromDegrees(_start.dec).ToNiceString()) + 
                    string.Format("Target: {0}, {1}\n",
                        Angle.FromHours(_target.ra).ToNiceString(),
                        Angle.FromDegrees(_target.dec).ToNiceString()
                    );

                EmitStart();
            }
        }

        public class DomeSlewActivity : TimeConsumingActivity
        {
            public enum DomeEventType { FindHome, Slew, Tracking };
            public double _startAz, _targetAz, _endAz;
            public string _reason;

            public class StartParams
            {
                public DomeEventType type;
                public double startAz, targetAz;
                public string reason;
            }

            public new class EndParams : Activity.EndParams
            {
                public double endAz;
            }

            public DomeSlewActivity(StartParams par): base(ActivityMonitor.ActivityType.DomeSlew)
            {
                if (par.type == DomeEventType.FindHome)
                {
                    _code = Code.Dome.FindHome;
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
                        _code = Code.Dome.Tracking;
                        _tags.Add("Tracking");
                    } else
                    {
                        _code = Code.Dome.Slewing;
                        _tags.Add("Slew");
                    }
                    _tags.Add("InProgress");

                    _startDetails =
                        string.Format("Start: {0}\n", Angle.FromDegrees(_startAz, Angle.Type.Az).ToNiceString()) +
                        string.Format("Target: {0}\n" + Angle.FromDegrees(_targetAz, Angle.Type.Az).ToNiceString()) +
                        string.Format("Reason: {0}\n",_reason);
                }

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                DomeSlewActivity.EndParams par = p as DomeSlewActivity.EndParams;

                _endAz = par.endAz;
                _endDetails = string.Format("End: {0}", Angle.FromDegrees(_endAz, Angle.Type.Az).ToNiceString());
                EndActivity(par);
            }
        }

        public class FocuserActivity : TimeConsumingActivity
        {
            public enum Direction { NotSet, Up, Down };
            public Direction _direction;
            public int _start, _target, _end;

            public class StartParams
            {
                public int start, target, intermediateTarget;
                public Direction direction;
            }

            public new class EndParams : Activity.EndParams
            {
                public int end;
            }

            public FocuserActivity(StartParams par): base(ActivityMonitor.ActivityType.Focuser)
            {
                _code = Code.Focuser;
                _tags = new List<string>() { "Focuser", "InProgress" };
                _direction = par.direction;
                _target = par.target;
                _start = par.start;
                _startDetails = string.Format("Start: {0}\n Target: {1}\n Direction: {2}",
                    _start, _target, _direction);

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                FocuserActivity.EndParams par = p as FocuserActivity.EndParams;

                _endDetails = string.Format("End: {0}", par.end);
                EndActivity(par);
            }
        }

        public class ShutterActivity : TimeConsumingActivity
        {
            public int _start, _target, _end;
            public ASCOM.DeviceInterface.ShutterState _operation;

            public class StartParams
            {
                public int start, target;
                public ASCOM.DeviceInterface.ShutterState operation;
            }

            public new class EndParams : Activity.EndParams
            {
                public int percentOpen;
            }

            public ShutterActivity(StartParams par): base (ActivityMonitor.ActivityType.Shutter)
            {
                _operation = par.operation;
                switch (_operation)
                {
                    case DeviceInterface.ShutterState.shutterOpening:
                        _code = Code.Shutter.Opening;
                        break;
                    case DeviceInterface.ShutterState.shutterOpen:
                        _code = Code.Shutter.Open;
                        break;
                    case DeviceInterface.ShutterState.shutterClosing:
                        _code = Code.Shutter.Closing;
                        break;
                    case DeviceInterface.ShutterState.shutterClosed:
                        _code = Code.Shutter.Closed;
                        break;
                }
                _tags = new List<string>() { "Shutter", _code.ToString(), "InProgress ??" };
                _start = par.start;
                _target= par.target;
                _startDetails =
                    string.Format("Start: {0}%", _start.ToString()) +
                    string.Format("Target: {0}%", _target.ToString()) +
                    string.Format("Operation: {0}", _operation.ToString());

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                ShutterActivity.EndParams par = p as ShutterActivity.EndParams;

                _end = par.percentOpen;
                _endDetails = string.Format("End: {0}%",  _end.ToString());

                EndActivity(par);
            }
        }

        public class PulsingActivity : TimeConsumingActivity
        {
            public DeviceInterface.GuideDirections _direction;
            public int _millis;
            public TelescopeSlewActivity.Coords _start, _end;

            public class StartParams
            {
                public TelescopeSlewActivity.Coords _start;
                public DeviceInterface.GuideDirections _direction;
                public int _millis;
            }

            public new class EndParams : Activity.EndParams
            {
                public TelescopeSlewActivity.Coords _end;
            }

            public PulsingActivity(StartParams par): base(ActivityMonitor.ActivityType.Pulsing)
            {
                _code = Code.Telescope.Pulsing;
                _tags = new List<string>() { "Pulsing", "InProgress" };
                _direction = par._direction;
                _millis = par._millis;
                _start = new TelescopeSlewActivity.Coords()
                {
                    ra = par._start.ra,
                    dec = par._start.dec,
                };
                _startDetails =
                    string.Format("Start: {0}, {1}\n",
                        Angle.FromHours(_start.ra).ToNiceString(),
                        Angle.FromDegrees(_start.dec, Angle.Type.Dec).ToNiceString()) +
                    string.Format("Direction: {0}\n", _direction.ToString()) +
                    string.Format("Millis: {0}\n", _millis.ToString());

                EmitStart(); ;
            }

            public override void End(Activity.EndParams p)
            {
                PulsingActivity.EndParams par = p as PulsingActivity.EndParams;

                _end = new TelescopeSlewActivity.Coords()
                {
                    ra = par._end.ra,
                    dec = par._end.dec,
                };
                _endDetails = string.Format("End: {0}, {1}\n",
                    Angle.FromHours(_end.ra, Angle.Type.RA).ToNiceString(),
                    Angle.FromDegrees(_end.dec, Angle.Type.Dec).ToNiceString());

                EndActivity(par);
            }
        }

        public class FilterWheelActivity : TimeConsumingActivity
        {
            public enum Operation { Detect, Move };
            public const int UnknownPosition = int.MinValue;

            public class StartParams
            {
                public Operation operation;
                public string startWheel;
                public int startPosition, targetPosition;
            }

            public new class EndParams: Activity.EndParams
            {
                public string endWheel;
                public int endPosition;
                public string endTag;
            }

            public FilterWheelActivity(StartParams par) : base(ActivityMonitor.ActivityType.FilterWheel)
            {
                _code = Code.FilterWheel;
                _tags = new List<string>() { "FilterWheel", par.operation.ToString(), "InProgress" };
                _startDetails = string.Format("Start: {0}\n", par.operation.ToString());
                _startDetails +=
                    string.Format(" Wheel: {0}\n", par.startWheel == null ? "none" : par.startWheel) +
                    string.Format(" Position: {0}\n", par.startPosition == FilterWheelActivity.UnknownPosition ? 
                        "none" : par.startPosition.ToString());
                if (par.operation == Operation.Move)
                    _startDetails += string.Format(" Target: {0}\n", par.targetPosition.ToString());

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                FilterWheelActivity.EndParams par = p as FilterWheelActivity.EndParams;

                _endDetails = "End:\n" +
                    string.Format("Wheel: {0}\n", par.endWheel) +
                    string.Format("Position: {0}\n", par.endPosition == FilterWheelActivity.UnknownPosition ? 
                        "none" : par.endPosition.ToString()) +
                    string.Format("Tag: {0}\n", par.endTag);

                EndActivity(par);
            }
        }

        public class ProjectorActivity : TimeConsumingActivity
        {
            public bool _onOff;

            public ProjectorActivity() : base(ActivityMonitor.ActivityType.Projector)
            {
                effectOnGoingIdle_AtStart = EffectOnGoingIdle.NoEffect;
                effectOnGoingIdle_AtEnd = EffectOnGoingIdle.NoEffect;

                _onOff = true;
                _code = Code.Projector;
                _tags = new List<string>() { "Projector" };
                _startDetails = string.Format("projector: {0}", _onOff ? "ON" : "OFF");

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                EndActivity(p);
            }
        }

        public class ParkActivity : TimeConsumingActivity
        {
            public TelescopeSlewActivity.Coords _start, _target, _end;
            public double _startAz, _targetAz, _endAz;
            public int _shutterPercentStart, _shutterPercentEnd;
            public ASCOM.DeviceInterface.ShutterState _shutterEndState;

            public class StartParams
            {
                public TelescopeSlewActivity.Coords start, target;
                public double domeStartAz, domeTargetAz;
                public int shutterPercent;
            }

            public new class EndParams : Activity.EndParams
            {
                public TelescopeSlewActivity.Coords end;
                public double domeAz;
                public int shutterPercent;
            }

            public ParkActivity(StartParams par): base(ActivityMonitor.ActivityType.Parking)
            {
                _start = new TelescopeSlewActivity.Coords()
                {
                    ra = par.start.ra,
                    dec = par.start.dec,
                };
                _startAz = par.domeStartAz;
                _shutterPercentStart = par.shutterPercent;

                _target = new TelescopeSlewActivity.Coords()
                {
                    ra = par.target.ra,
                    dec = par.start.dec,
                };
                _targetAz = par.domeTargetAz;
                _code = Code.Parking;
                _tags = new List<string>() { "Parking", "InProgress" };

                _startDetails = "Start:\n" +
                    string.Format(" Telescope: {0}, {1}\n",
                        Angle.FromHours(_start.ra, Angle.Type.RA).ToNiceString(),
                        Angle.FromDegrees(_start.dec, Angle.Type.Dec).ToNiceString()) +
                    string.Format(" Dome: {0}\n", Angle.FromDegrees(_startAz, Angle.Type.Az).ToNiceString()) +
                    string.Format(" Shutter: {0}%\n", _shutterPercentStart.ToString()) +
                    "Target:\n" +
                    string.Format(" Telescope: {0}, {1}\n",
                        Angle.FromHours(_target.ra, Angle.Type.RA).ToNiceString(),
                        Angle.FromDegrees(_target.dec, Angle.Type.Dec).ToNiceString()) +
                    string.Format(" Dome: {0}\n", Angle.FromDegrees(_targetAz, Angle.Type.Az).ToNiceString()) +
                    string.Format(" Shutter: {0}%\n", "100");

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                ParkActivity.EndParams par = p as ParkActivity.EndParams;

                _end = new TelescopeSlewActivity.Coords()
                {
                    ra = par.end.ra,
                    dec = par.end.dec,
                };
                _endAz = par.domeAz;
                _shutterPercentEnd = par.shutterPercent;

                _endDetails = "End:\n" +
                    string.Format(" Telescope: {0}, {1}\n",
                        Angle.FromHours(_end.ra, Angle.Type.RA).ToNiceString(),
                        Angle.FromDegrees(_end.dec, Angle.Type.Dec).ToNiceString()) +
                    string.Format(" Dome: {0}\n", Angle.FromDegrees(_endAz, Angle.Type.Az).ToNiceString()) +
                    string.Format(" Shutter: {0}%\n", _shutterPercentEnd.ToString());

                EndActivity(par);
            }
        }

        public class ShutdownActivity : TimeConsumingActivity
        {
            public string _reason;

            public ShutdownActivity(string reason): base(ActivityMonitor.ActivityType.ShuttingDown)
            {
                _reason = reason;
                _code = Code.Shutdown;
                _tags = new List<string>() { "Shutdown", "InProgress" };

                _startDetails = string.Format("Reason: {0}\n", _reason);
                effectOnGoingIdle_AtStart = EffectOnGoingIdle.Remove;
                effectOnGoingIdle_AtEnd = EffectOnGoingIdle.Remove;
                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                ShutdownActivity.EndParams par = p as ShutdownActivity.EndParams;

                EndActivity(par);
                Idler.BecomeIdle("end of ShuttingDown");
            }
        }

        public class HandpadActivity : TimeConsumingActivity
        {
            public DeviceInterface.TelescopeAxes _axis;
            public double _rate;
            public double _start, _end;

            public class StartParams
            {
                public DeviceInterface.TelescopeAxes axis;
                public double rate;
                public double start;
            }

            public new class EndParams : Activity.EndParams
            {
                public double end;
            }

            public HandpadActivity(StartParams par) : base(ActivityMonitor.ActivityType.Handpad)
            {
                _axis = par.axis;
                _rate = par.rate;
                _startDetails = string.Format("axis: {0}, start: {1}, rate: {2}",
                    _axis.ToString().Remove(0, "rate".Length),
                    (_axis == DeviceInterface.TelescopeAxes.axisPrimary) ?
                        Angle.FromHours(_start, Angle.Type.RA).ToNiceString() :
                        Angle.FromDegrees(_start, Angle.Type.Dec).ToNiceString(),
                    RateName(_rate));

                EmitStart(); ;
            }

            public override void End(Activity.EndParams p)
            {
                HandpadActivity.EndParams par = p as HandpadActivity.EndParams;

                _end = par.end;
                _endDetails = string.Format("end: {0}",
                    (_axis == DeviceInterface.TelescopeAxes.axisPrimary) ?
                        Angle.FromHours(_end, Angle.Type.RA).ToNiceString() :
                        Angle.FromDegrees(_end, Angle.Type.Dec).ToNiceString()
                    );
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

        public override void End(Activity.EndParams endParams) { }
        public void init()
        {
            StartGoingIdle("init()");
        }

        public void AbortGoingIdle(string reason)
        {
            _due = DateTime.MinValue;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            IdlerState prevState = _idlerState;
            _idlerState = IdlerState.ActivitiesInProgress;
            if (prevState != IdlerState.GoingIdle)
                return;

            _endDetails = string.Format("GoingIdle: aborted, reason: {0}",reason);

            EndActivity(new EndParams
            {
                endReason = reason,
                endState = Activity.State.Aborted,
            });
        }

        /// <summary>
        /// Start GoingIdle.  This can end either by Abortion (when
        ///  an activity starts, End() is called) or by Success (when
        ///  the timer expires (BecomeIdle is called))
        /// </summary>
        /// <param name="reason"></param>
        public void StartGoingIdle(string reason)
        {
            _type = ActivityMonitor.ActivityType.GoingIdle;
            _code = Code.GoingIdle;
            _tags = new List<string>() { "GoingIdle", "InProgress" };

            _startTime = DateTime.Now;
            startReason = reason;
            _startDetails = string.Format("Reason: {0}\n", startReason);
            EmitStart();

            _idlerState = IdlerState.GoingIdle;
            _due = DateTime.Now.AddMilliseconds(ActivityMonitor.millisToInactivity);
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer.Change(ActivityMonitor.millisToInactivity, Timeout.Infinite);
        }

        private static void onTimer(object state)
        {
            BecomeIdle(string.Format("GoingIdle: Became idle after {0} seconds", ActivityMonitor.millisToInactivity / 1000));
        }

        public static void BecomeIdle(string reason)
        {
            ActivityMonitor.idler.EndActivity(new EndParams()
            {
                endState = Activity.State.Idle,
                endReason = reason,
            });

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

                if (_idlerState == IdlerState.GoingIdle) {
                        TimeSpan ts = RemainingTime;

                        ret = "GoingIdle in ";
                        if (ts != TimeSpan.MaxValue)
                        {
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

        private static Debugger debugger = Debugger.Instance;

        public Event(EventType type)
        {
            _type = type;
            _time = DateTime.Now;
            _tags = new List<string>() { "Event", _type.ToString() };
        }

        public void EmitEvent()
        {
            string sql = "insert into activities(time, text, tags) " +
             string.Format("values('{0}', '{1}', '{2}');",
                 _time.ToString(@"yyyy-MM-dd HH:mm:ss.fff"),
                 _annotation + "\n",
                 string.Join(",", _tags));

            try
            {
                MySqlConnection sqlConn = new MySqlConnection("server=localhost;user=root;database=activities;port=3306;password=@!ab4131!@");
                sqlConn.Open();
                MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConn);
                sqlCmd.ExecuteNonQuery();
                sqlCmd.Dispose();
                sqlConn.Close();
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

                _annotation = string.Format("sensor: {0}Sensor, details: {1}, before: {2}, after: {3}",
                    sensor, details, before, after);
                _tags.Add(sensor);
                _tags.Add(before.ToString() + "->" + after.ToString());
            }

            public SafetyEvent(SafetyState newState): base(EventType.Safety)
            {
                _safetyState = newState;
                _annotation = string.Format("safetyState: {0}", _safetyState.ToString());
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
    }
}
