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
using MongoDB.Bson;
using MongoDB.Driver;

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


        public static IMongoDatabase db;

        public static IMongoCollection<BsonDocument> ActivitiesCollection
        {
            get
            {
                string collectionName = Debugger.LogDirectory().Remove(0, (Const.topWise40Directory + "Logs/").Length);

                if (db == null)
                    db = (new MongoClient()).GetDatabase("activities");
                return db.GetCollection<BsonDocument>(collectionName);
            }
        }


        public void init()
        {
            if (!WiseSite.CurrentProcessIsASCOMServer)
                return;

            if (initialized)
                return;

            int defaultMinutesToIdle = (int) TimeSpan.FromMilliseconds(defaultRealMillisToInactivity).TotalMinutes;
            int minutesToIdle;

            using (Profile p = new Profile() { DeviceType = "Telescope" })
                minutesToIdle = Convert.ToInt32(p.GetValue(Const.wiseTelescopeDriverID,
                    Const.ProfileName.Telescope_MinutesToIdle,
                    string.Empty,
                    defaultMinutesToIdle.ToString()));

            realMillisToInactivity = (int) TimeSpan.FromMinutes(minutesToIdle).TotalMilliseconds;
            millisToInactivity = WiseObject.Simulated ?
                ActivityMonitor.simulatedlMillisToInactivity :
                ActivityMonitor.realMillisToInactivity;

            db = (new MongoClient()).GetDatabase("activities");

            NewActivity(new Activity.GoingIdleActivity("ActivityMonitor init"));
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
            Activity activity = LookupInProgress(ActivityType.GoingIdle);

            if (activity != null)
                (activity as Activity.GoingIdleActivity).RestartTimer(reason);
        }

        public TimeSpan RemainingTime
        {
            get
            {
                Activity activity = LookupInProgress(ActivityType.GoingIdle);
                if (activity != null)
                    return (activity as Activity.GoingIdleActivity).RemainingTime;
                return TimeSpan.MaxValue;
            }
        }

        public bool ObservatoryIsActive()
        {
            List<string> activities = ObservatoryActivities;
            bool ret = activities.Count() != 0;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ActivityMonitor:ObservatoryIsActive: ret: {0} [{1}]",
                ret, string.Join(",", activities));
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
                    foreach (Activity a in ActivityMonitor.Instance.inProgressActivities)
                    {
                        if (a._type == ActivityType.GoingIdle)
                        {
                            Activity.GoingIdleActivity gia = (a as Activity.GoingIdleActivity);
                            TimeSpan ts = gia.RemainingTime;

                            string s = a._type.ToString();
                            if (ts != TimeSpan.MaxValue)
                            {
                                s += " in ";
                                if (ts.TotalMinutes > 0)
                                    s += string.Format("{0:D2}m", (int)ts.TotalMinutes);
                                s += string.Format("{0:D2}s", ts.Seconds);
                            }
                            ret.Add(s);
                        }
                        else if ((a._type & ActivityMonitor.ActivityType.RealActivities) != 0)
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
                string reason = string.Format("{0} started", activity._type.ToString());

                EndActivity(ActivityType.GoingIdle, new Activity.GoingIdleActivity.EndParams()
                {
                    endState = Activity.State.Aborted,
                    endReason = reason,
                    reason = reason,
                });
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

        public enum State { NotSet, Pending, Succeeded, Failed, Aborted, Idle };
        public State _state;

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
        public MongoDB.Bson.ObjectId _objectId;

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
            var document = new BsonDocument
            {
                { "Type", "ACTIVITY" },
                { "ActivityType", _type.ToString() },
                { "StartTime", _startTime },
                { "StartDetails", _startDetails },
            };
            ActivityMonitor.ActivitiesCollection.InsertOne(document);
            _objectId = (ObjectId) document.GetValue("_id");

            string msg = string.Format("log4net: START: _activity: {0} ({1})", _type.ToString(), _objectId.ToString());
            if (_startDetails != string.Empty)
            {
                msg += string.Format(", details: {0}", _startDetails);
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, msg);
            #endregion
        }

        public void EmitEnd()
        {
            string msg = string.Format("log4net:   END: _activity: {0} ({1})", _type.ToString(), _objectId.ToString());
            TimeSpan ts = _endTime - _startTime;
            msg += string.Format(", duration: {0}", ts.ToString(@"dd\:hh\:mm\:ss\.fff"));
            if (_endDetails != string.Empty)
            {
                msg += string.Format(", _details: {0}", _endDetails);
            }
            msg += string.Format(", _completionState: {0}, _completionReason: {1}", _state, _endReason);

            FilterDefinition<BsonDocument> filter = new BsonDocument("_id", _objectId);

            var update = Builders<BsonDocument>.Update.
                Set("EndTime", _endTime).
                Set("Duration", _duration).
                Set("EndDetails", _endDetails).
                Set("EndReason", _endReason).
                Set("EndState", _state.ToString());

            var result = ActivityMonitor.ActivitiesCollection.FindOneAndUpdate(filter, update);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, msg);
            #endregion
        }

        public void EndActivity(EndParams par)
        {
            if (par.endState == State.NotSet)
                throw new InvalidValueException(string.Format("Activity:EndActivity: Activity {0}: endState NOT set", this._type.ToString()));
            if (par.endReason == string.Empty)
                throw new InvalidValueException(string.Format("Activity:EndActivity: Activity {0}: endReason NOT set", this._type.ToString()));

            _endTime = DateTime.Now;
            _state = par.endState;
            _endReason = par.endReason;
            _duration = _endTime - _startTime;

            EmitEnd();

            monitor.endedActivities.Add(this);
            lock (ActivityMonitor.inProgressActivitiesLock)
            {
                monitor.inProgressActivities.Remove(this);
            }

            if (this._type != ActivityMonitor.ActivityType.GoingIdle && 
                ActivityMonitor.ObservatoryActivities.Count() == 0 &&
                effectOnGoingIdle_AtEnd == EffectOnGoingIdle.Renew)
            {
                monitor.NewActivity(new GoingIdleActivity("no activities in progress"));
            }
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
                effectOnGoingIdle_AtStart = EffectOnGoingIdle.Remove;
                effectOnGoingIdle_AtEnd = EffectOnGoingIdle.Renew;
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
                _startDetails = string.Format("startRa: {0}, startDec: {1}, targetRa: {2}, targetDec: {3}",
                    Angle.FromHours(_start.ra).ToNiceString(),
                    Angle.FromDegrees(_start.dec).ToNiceString(),
                    Angle.FromHours(_target.ra).ToNiceString(),
                    Angle.FromDegrees(_target.dec).ToNiceString()
                    );

                EmitStart();
            }
        }

        public class DomeSlewActivity : TimeConsumingActivity
        {
            public enum DomeEventType { FindHome, Slew };
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
                if (par.type == DomeEventType.Slew)
                {
                    _startAz = par.startAz;
                    _targetAz = par.targetAz;
                    _reason = par.reason;
                    _startDetails = string.Format("startAz: {0}, targetAz: {1}, reason: {2}",
                        Angle.FromDegrees(_startAz, Angle.Type.Az).ToNiceString(),
                        Angle.FromDegrees(_targetAz, Angle.Type.Az).ToNiceString(),
                        _reason);
                }
                else
                    _startDetails = "FindHome";

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                DomeSlewActivity.EndParams par = p as DomeSlewActivity.EndParams;

                _endAz = par.endAz;
                _endDetails = string.Format("endAz: {0}",
                    Angle.FromDegrees(_endAz, Angle.Type.Az).ToNiceString());
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
                _direction = par.direction;
                _target = par.target;
                _start = par.start;
                _startDetails = string.Format("start: {0}, target: {1}, direction: {2}",
                    _start, _target, _direction);

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                FocuserActivity.EndParams par = p as FocuserActivity.EndParams;

                _endDetails = string.Format("end: {0}", par.end);
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
                _start = par.start;
                _target= par.target;
                _startDetails = string.Format("operation: {0}, startPercent: {1}, targetPercent: {2}",
                    _operation.ToString(),
                    _start.ToString(),
                    _target.ToString());

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                ShutterActivity.EndParams par = p as ShutterActivity.EndParams;

                _end = par.percentOpen;
                _endDetails = string.Format("endPercent: {0}",  _end.ToString());

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
                _direction = par._direction;
                _millis = par._millis;
                _start = new TelescopeSlewActivity.Coords()
                {
                    ra = par._start.ra,
                    dec = par._start.dec,
                };
                _startDetails = string.Format("startRa: {0}, startDec: {1}, direction: {2}, millis: {3}",
                    Angle.FromHours(_start.ra).ToNiceString(),
                    Angle.FromDegrees(_start.dec, Angle.Type.Dec).ToNiceString(),
                    _direction.ToString(),
                    _millis.ToString());

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
                _endDetails = string.Format("endRa: {0}, endDec: {1}",
                    Angle.FromHours(_end.ra, Angle.Type.RA).ToNiceString(),
                    Angle.FromDegrees(_end.dec, Angle.Type.Dec).ToNiceString());

                EndActivity(par);
            }
        }

        public class FilterWheelActivity : TimeConsumingActivity
        {
            public int _startPos, _targetPos, _endPos;

            public class StartParams
            {
                public int start, target;
            }

            public new class EndParams: Activity.EndParams
            {
                public int end;
            }

            public FilterWheelActivity(StartParams par) : base(ActivityMonitor.ActivityType.FilterWheel)
            {
                _startPos = par.start;
                _targetPos = par.target;
                _startDetails = string.Format("start: {0}, target: {1}",
                    _startPos.ToString(),
                    _targetPos.ToString());

                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                FilterWheelActivity.EndParams par = p as FilterWheelActivity.EndParams;

                _endPos = par.end;
                _endDetails = string.Format("end: {0}", _endPos);

                EndActivity(par);
            }
        }

        public class GoingIdleActivity : TimeConsumingActivity
        {
            public DateTime _due;
            private System.Threading.Timer _timer = new System.Threading.Timer(BecomeIdle);
            public string startReason, endReason;

            public GoingIdleActivity(string reason) : base(ActivityMonitor.ActivityType.GoingIdle)
            {
                effectOnGoingIdle_AtStart = EffectOnGoingIdle.NoEffect;

                RestartTimer(reason);
            }

            public new class EndParams : Activity.EndParams
            {
                public string reason;
            }

            public override void End(Activity.EndParams p)
            {
                GoingIdleActivity.EndParams par = p as GoingIdleActivity.EndParams;

                _due = DateTime.MinValue;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _endReason = par.reason;
                _endDetails = "";

                EndActivity(par);
            }

            public void RestartTimer(string reason)
            {
                startReason = reason;
                _startDetails = string.Format("restart: {0}", startReason);
                EmitStart();

                _due = DateTime.Now.AddMilliseconds(ActivityMonitor.millisToInactivity);
                _timer.Change(ActivityMonitor.millisToInactivity, Timeout.Infinite);
            }

            private static void BecomeIdle(object state)
            {
                monitor.EndActivity(ActivityMonitor.ActivityType.GoingIdle, new EndParams() {
                    endState = State.Idle,
                    endReason = "Time is up",
                    reason = "Time is up",
                });
            }

            public TimeSpan RemainingTime
            {
                get
                {
                    if (_due == DateTime.MinValue)
                        return TimeSpan.MaxValue;
                    return _due.Subtract(DateTime.Now);
                }
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
                
                _startDetails = string.Format("start: [ra: {0}, dec: {1}, az: {2}, percent: {3}], target: [ra: {4}, dec: {5}, az: {6}, percent: {7}]",
                    Angle.FromHours(_start.ra, Angle.Type.RA).ToNiceString(),
                    Angle.FromDegrees(_start.dec, Angle.Type.Dec).ToNiceString(),
                    Angle.FromDegrees(_startAz, Angle.Type.Az).ToNiceString(),
                    _shutterPercentStart.ToString(),
                    Angle.FromHours(_target.ra, Angle.Type.RA).ToNiceString(),
                    Angle.FromDegrees(_target.dec, Angle.Type.Dec).ToNiceString(),
                    Angle.FromDegrees(_targetAz, Angle.Type.Az).ToNiceString(),
                    100.ToString()
                    );

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
                _endDetails = string.Format("end: [ra: {0}, dec: {1}, az: {2}, percent: {3}]",
                    Angle.FromHours(_end.ra, Angle.Type.RA).ToNiceString(),
                    Angle.FromDegrees(_end.dec, Angle.Type.Dec).ToNiceString(),
                    Angle.FromDegrees(_endAz, Angle.Type.Az).ToNiceString(),
                    _shutterPercentEnd.ToString()
                    );

                EndActivity(par);
            }
        }

        public class ShutdownActivity : TimeConsumingActivity
        {
            public string _reason;

            public ShutdownActivity(string reason): base(ActivityMonitor.ActivityType.ShuttingDown)
            {
                _reason = reason;
                _startDetails = string.Format("reason: {0}", _reason);
                effectOnGoingIdle_AtStart = EffectOnGoingIdle.Remove;
                effectOnGoingIdle_AtEnd = EffectOnGoingIdle.Remove;
                EmitStart();
            }

            public override void End(Activity.EndParams p)
            {
                ShutdownActivity.EndParams par = p as ShutdownActivity.EndParams;

                EndActivity(par);
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

    public class Event
    {
        public enum EventType { NotSet, Safety, Generic };

        public EventType _type = EventType.NotSet;
        public DateTime _utcTime;
        public string _details;

        private static Debugger debugger = Debugger.Instance;

        public Event(EventType type)
        {
            _type = type;
            _utcTime = DateTime.UtcNow;
        }

        public void EmitEvent()
        {
            string msg = string.Format("log4net: EVENT: {0}", _type.ToString());
            if (_details != string.Empty)
            {
                msg += string.Format(", details: {0}", _details);
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, msg);

            ActivityMonitor.ActivitiesCollection.InsertOne(new BsonDocument {
                { "Type", "EVENT" },
                { "EventType", _type.ToString() },
                { "Time", _utcTime },
                { "Details", _details },
            });
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

                _details = string.Format("sensor: {0}Sensor, details: {1}, before: {2}, after: {3}",
                    sensor, details, before, after);
            }

            public SafetyEvent(SafetyState newState): base(EventType.Safety)
            {
                _safetyState = newState;
                _details = string.Format("safetyState: {0}", _safetyState.ToString());
            }
        }

        public class GlobalEvent : Event
        {
            public GlobalEvent(string details) : base(EventType.Generic)
            {
                if (details == string.Empty)
                    throw new InvalidValueException("GlobalEvent:ctor: bad args");

                _details = details;
            }
        }
    }
}
