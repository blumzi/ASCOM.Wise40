using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Security.AccessControl;
using System.Security.Principal;

using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASCOM.Wise40.Common
{
    /// <summary>
    /// Wise40's own settings store: a drop-in for ASCOM.Utilities.Profile, backed by
    /// c:\Wise40\settings.json instead of the registry.
    /// </summary>
    //
    // WHY NOT THE ASCOM PROFILE
    //
    // Because a build deletes it.  RegisterForComInterop makes regasm run the driver's
    //  [ComUnregisterFunction] before re-registering, that calls Profile.Unregister, and ASCOM
    //  documents Unregister as deleting "the entire device profile tree, including the DriverID
    //  root key" - every value with it.  ReadProfile then re-creates the settings from their CODE
    //  DEFAULTS, so everything looks present and correct while quietly holding whatever the source
    //  says rather than what the observatory was set to.  That is how a deliberate switch to
    //  EncodersInUse = New was lost without anyone noticing.
    //
    // The ASCOM Initiative reached the same conclusion: Platform 6.6 SP1 removed "unnecessary COM
    //  registration functions" from the C# driver templates, and the modern ASCOM Library ships
    //  ASCOM.Tools.XMLProfile, which stores settings in a FILE under ProgramData.  This is the
    //  same move, with JSON and a path that matches the rest of Wise40.
    //
    // WHAT STAYS IN THE REGISTRY
    //
    // Registration.  Profile.Register/Unregister/IsRegistered still go to the real ASCOM Profile,
    //  because that is what puts a driver in the Chooser.  Only the VALUES move here.
    //
    // THE OBSERVATORY MONITOR
    //
    // It has no natural ASCOM device type, so it was registered twice - once under "SafetyMonitor
    //  Drivers" and once under "Telescope Drivers" - and its settings landed in whichever tree the
    //  caller happened to name.  Sections here are OURS, so it gets exactly one, and the aliases
    //  below fold the old identities into it.
    //
    public class WiseProfile : IDisposable
    {
        public static readonly string SettingsFile = Path.Combine(
            Const.topWise40Directory.Replace('/', '\\'), "settings.json");

        //
        // Cross-process, because the RemoteServer, the Dash, the ObservatoryMonitor and the
        //  watcher can all write.  The registry serialised this for us; a file does not.
        //
        private static readonly Mutex fileMutex = CreateFileMutex();
        private static readonly object memoryLock = new object();

        //
        // The chain spans accounts: Wise40Watcher runs as LocalSystem and launches the drivers,
        //  while the Dash and the ASCOM hubs run as the logged-in user.  A "Global\" kernel object
        //  created without an explicit DACL is reachable only by its creator's account, so
        //  whichever side started second got UnauthorizedAccessException here.  This runs in a
        //  static initializer, so that throw poisoned WiseProfile, then WiseSite, then Exceptor,
        //  and took every driver in the process with it - which is how both ObservingConditions
        //  drivers died on 2026-09-21.  It only reproduces ACROSS accounts, which is why every
        //  same-user test passed.
        //
        // So: grant Everyone access at creation, and never let this method throw.
        //
        private static Mutex CreateFileMutex()
        {
            try
            {
                MutexSecurity security = new MutexSecurity();
                security.AddAccessRule(new MutexAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    MutexRights.Synchronize | MutexRights.Modify | MutexRights.ReadPermissions,
                    AccessControlType.Allow));

                bool createdNew;
                return new Mutex(false, @"Global\Wise40Settings", out createdNew, security);
            }
            catch (Exception)
            {
                // Either this account may not create in the global namespace, or the object
                //  already exists with a restrictive DACL from a pre-fix process.  Degrade to a
                //  session-local lock rather than leave the driver unusable: writes are
                //  write-temp-then-copy, so the file still cannot be torn - only cross-session
                //  serialisation is lost, and that is recoverable.  A dead driver is not.
                try { return new Mutex(false, @"Local\Wise40Settings"); }
                catch (Exception) { return new Mutex(false); }
            }
        }

        // section -> subKey (RootSub, i.e. "(root)", for the section root) -> name -> value
        //
        // JToken, not string.  The registry could only hold strings, so booleans arrived as
        //  whatever the writer happened to produce - "True" from bool.ToString() in one place and
        //  a literal "true" in another, sitting next to each other in the same section.  This is
        //  our file now, so it holds real JSON types: a human editing it sees
        //  "Enabled": true rather than "Enabled": "True".
        //
        // Safe because nothing compares these as strings - all 22 boolean reads go through
        //  Convert.ToBoolean or bool.TryParse, both case-insensitive.
        //
        private static Dictionary<string, Dictionary<string, Dictionary<string, JToken>>> _store;

        // write time of the file as last read, so an external change is noticed - see Load()
        private static DateTime _loadedStamp = DateTime.MinValue;

        /// <summary>
        /// Several ProgIDs, one set of settings.  See the note above about ObservatoryMonitor.
        /// </summary>
        private static readonly Dictionary<string, string> aliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "ASCOM.Wise40.ObservatoryMonitor",               "ObservatoryMonitor" },
                { "ASCOM.Wise40.ObservatoryMonitor.SafetyMonitor", "ObservatoryMonitor" },
                { "ASCOM.Wise40SafeToOperate.SafetyMonitor",       "SafeToOperate" },
                { "ASCOM.WiseSafeToOperate.SafetyMonitor",         "SafeToOperate" },
                { "ASCOM.Wise40.Telescope",                        "Telescope" },
                { "ASCOM.Wise40.Dome",                             "Dome" },
                { "ASCOM.Wise40.Focuser",                          "Focuser" },
                { "ASCOM.Wise40.FilterWheel",                      "FilterWheel" },
                { "ASCOM.Wise40.VantagePro.ObservingConditions",   "VantagePro" },
                { "ASCOM.Wise40.Boltwood.ObservingConditions",     "Boltwood" },
                { "ASCOM.Wise40.TessW.ObservingConditions",        "TessW" },
                { "ASCOM.Wise40.ComputerControl.SafetyMonitor",    "ComputerControl" },
            };

        /// <summary>
        /// Accepted and ignored.  ASCOM needs it to find the right registry tree; our sections are
        /// named for the driver, so there is nothing for it to select.
        /// </summary>
        public string DeviceType { get; set; }

        private static string Section(string driverID)
        {
            if (string.IsNullOrEmpty(driverID))
                return "Unknown";
            return aliases.TryGetValue(driverID, out string s) ? s : driverID;
        }

        //
        // The name for "this driver's settings, not in a sub-key".
        //
        // It was the empty string, which is what ASCOM's subKey parameter means - and which JSON
        //  permits as a property name.  Newtonsoft reads it happily; PowerShell 5.1 does not, and
        //  refuses the whole document:
        //
        //      ConvertFrom-Json : Cannot process argument because the value of argument
        //                         "name" is not valid.
        //
        // This file is meant to be read and edited by people and scripts, so one unreadable
        //  property name that poisons the entire document is not a good trade for matching ASCOM's
        //  internal convention.  Parentheses cannot collide with a real sub-key: the ones in use
        //  are names like Station0, Interval, Max and Wheel4/Position1.
        //
        public const string RootSub = "(root)";

        private static string Sub(string subKey)
        {
            return string.IsNullOrEmpty(subKey) ? RootSub : subKey.Replace('\\', '/').Trim('/');
        }

        /// <summary>
        /// A string from a caller becomes the JSON type it describes - bool, number or string.
        /// </summary>
        //
        // ROUND-TRIP GUARDED.  A value is only stored as a number when formatting it back yields
        //  exactly the original text, so "1.0" stays the string "1.0" rather than becoming 1 and
        //  reading back as "1", and "007" keeps its leading zeros.  Anything that does not survive
        //  that test is left alone, which is the safe direction: an unrecognised setting is stored
        //  verbatim rather than reinterpreted.
        //
        private static JToken ToToken(string value)
        {
            if (value == null)
                return string.Empty;

            if (bool.TryParse(value, out bool b))           // accepts "true", "True", "TRUE"
                return b;

            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l) &&
                l.ToString(CultureInfo.InvariantCulture) == value)
                return l;

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) &&
                d.ToString("R", CultureInfo.InvariantCulture) == value)
                return d;

            return value;
        }

        /// <summary>
        /// And back again, in the form the drivers parse.
        /// </summary>
        //
        // Booleans come back as "True"/"False" - .NET's own Boolean.ToString() spelling, which is
        //  what the writers produced before this change.  Every reader uses Convert.ToBoolean or
        //  bool.TryParse and so does not care, but matching the old spelling keeps a Write-then-Read
        //  round trip identical for anything that does.
        //
        private static string FromToken(JToken t)
        {
            if (t == null)
                return string.Empty;

            switch (t.Type)
            {
                case JTokenType.Boolean: return (bool)t ? "True" : "False";
                case JTokenType.Integer: return ((long)t).ToString(CultureInfo.InvariantCulture);
                case JTokenType.Float:   return ((double)t).ToString("R", CultureInfo.InvariantCulture);
                case JTokenType.Null:    return string.Empty;
                default:                 return t.ToString();
            }
        }

        public string GetValue(string driverID, string name, string subKey = "", string defaultValue = null)
        {
            lock (memoryLock)
            {
                Load();
                string section = Section(driverID), sub = Sub(subKey);

                if (_store.TryGetValue(section, out var subs) &&
                    subs.TryGetValue(sub, out var values) &&
                    values.TryGetValue(name, out JToken v))
                    return FromToken(v);
            }

            //
            // A READ DOES NOT WRITE.  Removed 2026-09-22 (Arie's decision).
            //
            // This used to persist the default when a value was absent.  That was a defence
            //  against the ASCOM Profile vanishing at build time - regasm's unregister deleting
            //  the device tree - so a read that re-created what the build had destroyed looked
            //  like a repair.  The file is the source of truth now and a build cannot touch it, so
            //  the defence has nothing left to defend and only its costs remain:
            //
            //   . a read mutates the store, which is a side effect nobody calling GetValue expects
            //   . it makes "present in settings.json" mean "something once asked for it" rather
            //     than "somebody set it", so the file stops being a record of decisions
            //   . worst, it fights the timestamp cache in Load(): rewriting the file bumps its
            //     write time, which invalidates the cached copy in EVERY OTHER PROCESS.  A read
            //     would trigger a reload chain-wide.
            //
            // Absent now simply means "use the caller's default".  Values reach the file when
            //  something deliberately writes them - WriteProfile, or a setup dialog.
            //
            return defaultValue ?? string.Empty;
        }

        public void WriteValue(string driverID, string name, string value, string subKey = "")
        {
            string section = Section(driverID), sub = Sub(subKey);

            // AbandonedMutexException means a previous holder died without releasing - we DID get
            //  the lock, so carry on.  Unhandled it would propagate out of whatever callback thread
            //  happened to be writing and terminate the process.
            try
            {
                if (!fileMutex.WaitOne(TimeSpan.FromSeconds(10)))
                    return;                 // ten seconds of contention on a settings file is a bug elsewhere
            }
            catch (AbandonedMutexException) { }
            try
            {
                lock (memoryLock)
                {
                    //
                    // Re-read before writing.  Several processes share this file, and without this
                    //  the last one to save would clobber everything another had changed since it
                    //  loaded.
                    //
                    _store = null;
                    Load();

                    if (!_store.ContainsKey(section))
                        _store[section] = new Dictionary<string, Dictionary<string, JToken>>();
                    if (!_store[section].ContainsKey(sub))
                        _store[section][sub] = new Dictionary<string, JToken>();

                    _store[section][sub][name] = ToToken(value);
                    Save();
                }
            }
            finally { fileMutex.ReleaseMutex(); }
        }

        //
        // ANOTHER PROCESS MAY HAVE CHANGED THE FILE SINCE WE LAST READ IT.
        //
        // The registry gave us this for free: every GetValue went to the registry, so a setting
        //  changed anywhere was visible everywhere, immediately.  A cached file does not - and
        //  that is exactly the setup flow.  An inproc COM driver's setup dialog runs in the
        //  CALLER's process (the ASCOM Chooser, or whichever hub opened Properties), never in the
        //  RemoteServer process where the live driver instance sits.  Caching for the life of the
        //  process would mean a changed setting silently not taking effect until a restart.
        //
        // So compare the file's write time and re-read when it moves.  One stat per GetValue is
        //  far cheaper than the registry call it replaces.
        //
        private static void Load()
        {
            DateTime stamp = DateTime.MinValue;
            try { if (File.Exists(SettingsFile)) stamp = File.GetLastWriteTimeUtc(SettingsFile); }
            catch { }

            if (_store != null && stamp == _loadedStamp)
                return;

            try
            {
                if (File.Exists(SettingsFile))
                {
                    _store = JsonConvert.DeserializeObject<
                        Dictionary<string, Dictionary<string, Dictionary<string, JToken>>>>(
                            File.ReadAllText(SettingsFile));
                    _loadedStamp = stamp;
                }
            }
            catch (Exception ex)
            {
                //
                // A corrupt settings file must not stop the observatory.  Falling back to an empty
                //  store means every GetValue returns its caller's default, which is the same
                //  behaviour as a freshly wiped ASCOM profile - survivable, and loud in the log.
                //
                try
                {
                    Debugger.Instance.WriteLine(Debugger.DebugLevel.DebugExceptions,
                        $"WiseProfile: could not read {SettingsFile}: {ex.Message} - using defaults");
                }
                catch { }
            }

            if (_store == null)
                _store = new Dictionary<string, Dictionary<string, Dictionary<string, JToken>>>();

            //
            //
            // NO SEEDING FROM THE REGISTRY.  Removed 2026-09-22 (Arie's decision).
            //
            // This used to import the ASCOM Profile when the file was absent, on the reasoning
            //  that the registry held what the observatory was actually set to.  That stopped
            //  being true the moment the file became the source of truth: the registry is a
            //  snapshot frozen at migration, and it does not even share this file's shape any
            //  more - SafeToOperate was re-keyed from [attribute][sensor] to [sensor][attribute],
            //  so a re-seed would produce a section the drivers cannot read and every sensor
            //  would quietly fall back to its code default.  A stale seed is worse than no seed,
            //  because it looks like data.
            //
            // So an absent file means every driver runs on its code defaults.  Nothing repopulates
            //  it either - GetValue no longer writes defaults back - so the file stays missing
            //  until something deliberately writes, and the observatory quietly runs on source
            //  defaults in the meantime.  That is a real loss of settings: SAY SO LOUDLY rather
            //  than letting it pass as ordinary startup.
            //
            if (!File.Exists(SettingsFile))
            {
                try
                {
                    Debugger.Instance.WriteLine(Debugger.DebugLevel.DebugExceptions,
                        $"WiseProfile: {SettingsFile} DOES NOT EXIST - every setting will come from its " +
                        "code default, and nothing will repopulate the file. Restore it from a backup " +
                        "(settings.json.predeploy) if this was not intended.");
                }
                catch { }
            }
        }


        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));

                // Sorted, so a diff of this file shows what changed rather than what moved.
                var ordered = _store.OrderBy(s => s.Key).ToDictionary(
                    s => s.Key,
                    s => s.Value.OrderBy(k => k.Key).ToDictionary(
                        k => k.Key,
                        k => k.Value.OrderBy(v => v.Key).ToDictionary(v => v.Key, v => v.Value)));

                // Write-then-replace: a crash mid-write leaves the old file, not half a new one.
                string tmp = SettingsFile + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(ordered, Formatting.Indented));
                File.Copy(tmp, SettingsFile, true);
                File.Delete(tmp);

                // Our own write is already in _store, so record its stamp rather than re-reading
                //  the file on the next GetValue.
                _loadedStamp = File.GetLastWriteTimeUtc(SettingsFile);
            }
            catch (Exception ex)
            {
                try
                {
                    Debugger.Instance.WriteLine(Debugger.DebugLevel.DebugExceptions,
                        $"WiseProfile: could not write {SettingsFile}: {ex.Message}");
                }
                catch { }
            }
        }

        /// <summary>
        /// Drops the in-memory copy, so the next read comes from disk.
        /// </summary>
        public static void Reload()
        {
            lock (memoryLock) { _store = null; }
        }

        public void Dispose()
        {
            // Nothing held open: writes are flushed as they happen, and the store is static.
            GC.SuppressFinalize(this);
        }
    }
}
