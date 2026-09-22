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
    /// c:\Wise40\Settings.json instead of the registry.
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
            Const.topWise40Directory.Replace('/', '\\'), "Settings.json");

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

        //
        // The document itself, not a dictionary-of-dictionaries.
        //
        // Values are real JSON types rather than strings.  The registry could only hold strings, so
        //  booleans arrived as whatever the writer happened to produce - "True" from bool.ToString()
        //  in one place and a literal "true" in another, side by side in the same section.  This is
        //  our file now, so someone editing it sees "Enabled": true.  Safe because nothing compares
        //  these as strings: every boolean read goes through Convert.ToBoolean or bool.TryParse,
        //  both case-insensitive.
        //
        // A section holds its driver-level settings DIRECTLY, and a sub-key is simply a nested
        //  object beside them:
        //
        //      "SafeToOperate": {
        //          "Bypassed": false,                       <- a driver-level setting, a leaf
        //          "Wind": { "Enabled": true, "Max": 40 }   <- a sub-key, an object
        //      }
        //
        // The previous shape forced every section through a "(root)" wrapper so that all three
        //  levels were uniform.  It made the file read like a data structure rather than a
        //  settings file, and "(root)" existed only because the empty string - ASCOM's own name
        //  for "no sub-key" - is a property name PowerShell refuses to parse.
        //
        // Leaf-or-object is what distinguishes the two, so the lookups below type-check rather
        //  than assume: an object found where a value was asked for reads as absent, and vice
        //  versa.  Verified before the change that no section has a setting sharing a name with
        //  a sub-key, so nothing is ambiguous today.
        //
        private static JObject _store;

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
        // null means "the section itself", not a sub-key.  ASCOM spells that as the empty string,
        //  which JSON permits as a property name but PowerShell 5.1 refuses to parse - it rejects
        //  the whole document.  A "(root)" placeholder was used for a while to dodge that; the
        //  section now holds its own settings directly and needs no placeholder at all.
        //
        private static string Sub(string subKey)
        {
            return string.IsNullOrEmpty(subKey) ? null : subKey.Replace('\\', '/').Trim('/');
        }

        /// <summary>
        /// The object holding the values for (section, sub) - the section itself when sub is null.
        /// </summary>
        //
        // Type-checked rather than assumed.  A section mixes leaves (its own settings) with objects
        //  (its sub-keys), so finding the wrong kind means the caller asked for something that is
        //  not there - report absent rather than coerce, which would either read a sub-key as a
        //  value or, worse on the write path, overwrite a whole sub-key with a scalar.
        //
        private static JObject Container(string section, string sub, bool create)
        {
            if (!(_store[section] is JObject sec))
            {
                if (!create || _store[section] != null) return null;
                sec = new JObject();
                _store[section] = sec;
            }
            if (sub == null)
                return sec;

            if (!(sec[sub] is JObject inner))
            {
                if (!create || sec[sub] != null) return null;
                inner = new JObject();
                sec[sub] = inner;
            }
            return inner;
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
                JObject c = Container(Section(driverID), Sub(subKey), false);
                JToken v = c?[name];

                // An object here is a sub-key of the same name, not this setting's value.
                if (v != null && v.Type != JTokenType.Object)
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

                    JObject c = Container(section, sub, true);
                    if (c == null)
                    {
                        // Something of the wrong kind is in the way - a scalar where a sub-key
                        //  belongs.  Refuse rather than replace it: silently discarding settings
                        //  is worse than not writing one.
                        try
                        {
                            Debugger.Instance.WriteLine(Debugger.DebugLevel.DebugExceptions,
                                $"WiseProfile: cannot write {section}/{sub ?? "(section)"}/{name} - " +
                                "a value already occupies that path");
                        }
                        catch { }
                        return;
                    }

                    if (c[name] is JObject)
                    {
                        // Writing here would delete a whole sub-key of the same name.
                        try
                        {
                            Debugger.Instance.WriteLine(Debugger.DebugLevel.DebugExceptions,
                                $"WiseProfile: refusing to overwrite the sub-key {section}/{name} with a value");
                        }
                        catch { }
                        return;
                    }

                    c[name] = ToToken(value);
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
                    _store = JObject.Parse(File.ReadAllText(SettingsFile));
                    _loadedStamp = stamp;

                    //
                    // A "(root)" object means this file predates the flattening of 2026-09-22, so
                    //  every driver-level setting in that section is sitting where nothing looks
                    //  for it any more.  Do not silently half-read it - the reads would return
                    //  code defaults and look like a working startup.
                    //
                    foreach (var sec in _store.Properties())
                    {
                        if ((sec.Value as JObject)?["(root)"] != null)
                        {
                            try
                            {
                                Debugger.Instance.WriteLine(Debugger.DebugLevel.DebugExceptions,
                                    $"WiseProfile: {SettingsFile} section '{sec.Name}' still has a " +
                                    "'(root)' block - this file predates the 2026-09-22 flattening and " +
                                    "its driver-level settings WILL NOT BE READ. Flatten it or restore a current backup.");
                            }
                            catch { }
                        }
                    }
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
                _store = new JObject();

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
                        "(Settings.json.predeploy) if this was not intended.");
                }
                catch { }
            }
        }


        /// <summary>
        /// A copy with properties ordered: values before nested objects, each alphabetically.
        /// </summary>
        private static JObject Sorted(JObject o, bool valuesFirst)
        {
            JObject result = new JObject();
            IEnumerable<JProperty> props = o.Properties();

            foreach (JProperty p in props.Where(x => !(x.Value is JObject)).OrderBy(x => x.Name))
                result[p.Name] = p.Value;
            foreach (JProperty p in props.Where(x => x.Value is JObject).OrderBy(x => x.Name))
                result[p.Name] = Sorted((JObject)p.Value, valuesFirst);

            return result;
        }

        /// <summary>
        /// Move <paramref name="tmp"/> onto <paramref name="target"/> atomically.
        /// </summary>
        //
        // Retried, because a rename fails while anything else holds the target open - a reader
        //  mid-ReadAllText, a text editor, a backup tool.  Reads are brief, so a few attempts
        //  clear it.  If they do not, the caller's catch reports the write as lost, which is the
        //  honest outcome: better a logged failure than a torn file.
        //
        private static void Swap(string tmp, string target)
        {
            const int attempts = 5;
            for (int i = 1; ; i++)
            {
                try
                {
                    if (File.Exists(target))
                        File.Replace(tmp, target, null);      // atomic, keeps the target's ACLs
                    else
                        File.Move(tmp, target);               // first write: nothing to replace
                    return;
                }
                catch (IOException) when (i < attempts)
                {
                    Thread.Sleep(40);
                }
            }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));

                // Sorted, so a diff of this file shows what changed rather than what moved.  Within
                //  a section the driver's own settings come first and its sub-keys after, each
                //  alphabetically - which is also how someone reads it.
                JObject ordered = Sorted(_store, true);

                //
                // WRITE TO A TEMPORARY FILE, THEN SWAP IT IN ATOMICALLY.
                //
                // This used File.Copy, which writes into the destination in place.  Readers do not
                //  take the cross-process mutex - deliberately, so that a settings read never waits
                //  on a kernel object - so a reader in another process could observe the file
                //  half-written, fail to parse it, and fall back to an empty store.  Every setting
                //  would then read as its code default until the next read retried.  It
                //  self-corrected, and the defaults are the stricter direction, but "the safety
                //  system briefly ran on defaults" is not a thing to leave to chance.
                //
                // A rename is atomic: a reader sees the whole old file or the whole new one, never
                //  a mixture.  File.Replace also preserves the destination's ACLs, which matters
                //  if this file is ever locked down.
                //
                string tmp = SettingsFile + ".tmp";
                File.WriteAllText(tmp, ordered.ToString(Formatting.Indented));
                Swap(tmp, SettingsFile);

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
