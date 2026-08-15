using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ASCOM.Wise40
{
    /// <summary>
    /// Reads the filter names the observer maintains in MaxIm DL.  Wise40 no
    ///  longer keeps its own list of which filter sits where - MaxIm does, and
    ///  this is where MaxIm keeps it:
    ///
    ///     Documents\MaxIm DL &lt;version&gt;\Settings\MaxIm CCD\SetupFilterWheel.txt
    ///
    /// One line per slot, tab separated, the name quoted:
    ///
    ///     Camera0_ASCOMFilterF1    "U"
    ///     Camera0_ASCOMFilterF2    "B"
    ///     ...
    ///     Camera0_ASCOMFilterF9    ""        (past the end of the wheel)
    ///
    /// Two things about that file will catch you out:
    ///
    ///  - The slot numbers are 1-based (F1..F64) while our positions are 0-based.
    ///
    ///  - The lines are sorted as text, so they run F1, F10, F11 ... F2, F20 ...
    ///     Never read this file by line order; the key is the only thing that
    ///     says which slot a name belongs to.
    ///
    /// MaxIm keeps a list per camera, not per wheel, and knows nothing about
    ///  which of our two wheels is mounted.  So the eight position wheel takes
    ///  slots 1..8 and the four position wheel takes slots 1..4, of the same
    ///  list - which is exactly how the observer fills it in.
    ///
    /// Two things Diffraction Limited's own documentation warns about, neither
    ///  of which bites us today but both of which would be silent if they did:
    ///
    ///  - "ASCOMFilter" in the key is the filter wheel FAMILY, not a constant.
    ///     Point MaxIm at a native driver instead of an ASCOM one and the keys
    ///     become Camera0_ApogeeUSBFilterF1 and the like, and we would find
    ///     nothing.  Camera0_SelectedFilterWheelName says which is in use.
    ///
    ///  - MaxIm can save several NAMED configurations, each a whole settings
    ///     set.  If the observatory ever uses that, the filter list in play may
    ///     not be the one in Settings\.  There was only one set as of Aug 2026.
    ///
    /// Reading is best effort: no MaxIm, no file, or a file that says nothing
    ///  about a slot all come back as null, and the caller shows what it has.
    ///
    /// See https://cdn.diffractionlimited.com/help/maximdl/Configuration.htm -
    ///  "All configuration information is stored under My Documents/MaxIm DL 7/
    ///  Settings", and it is files, not the registry.
    /// </summary>
    public static class MaxImFilterNames
    {
        /// <summary>
        /// Which camera's list to read.  MaxIm numbers them from 0 and keeps a
        ///  separate list for each; ours is the first one.
        /// </summary>
        public static int CameraIndex { get; set; } = 0;

        /// <summary>
        /// Where to look, newest MaxIm first.  The FIRST one that exists wins.
        ///
        /// Not the most recently written one, which is the obvious thing to try and
        ///  is wrong: upgrading leaves the old version's settings behind, and the
        ///  old files keep being touched by whatever still opens them.  On the
        ///  development machine in August 2026 the MaxIm DL 6 file was a week newer
        ///  than the MaxIm DL 7 file, while the observatory runs 7 - so "newest
        ///  file" would have read the wrong version's filter names, and quietly.
        ///
        /// Add new MaxIm versions at the front of this list.
        /// </summary>
        private static readonly string[] CandidateDirs = {
            "MaxIm DL 7", "MaxIm DL 6", "MaxIm DL 5",
        };

        private static readonly object _lock = new object();
        private static Dictionary<int, string> _names = new Dictionary<int, string>();
        private static string _pathSeen;
        private static DateTime _lastWriteSeen = DateTime.MinValue;

        /// <summary>
        /// Where the names last came from, meant to be shown beside them.
        /// </summary>
        public static string Provenance { get; private set; } = "not read yet";

        /// <summary>
        /// The file MaxIm is actually keeping its filter names in, or null.
        /// </summary>
        /// <summary>
        /// Set this to pin the file explicitly, when the guesses below are wrong.
        /// </summary>
        public static string OverridePath { get; set; }

        /// <summary>
        /// Where a "Documents" folder might be.
        ///
        /// Environment.GetFolderPath(MyDocuments) alone is NOT enough here.  The
        ///  Wise40 watcher starts Dash with CreateProcessAsUser() and neither
        ///  LoadUserProfile() nor CreateEnvironmentBlock(), so the child has the
        ///  user's token but not their loaded profile - and MyDocuments comes back
        ///  empty or pointing at the service profile.  An empty root then makes
        ///  Path.Combine() produce a RELATIVE path, File.Exists() resolves it
        ///  against the working directory, and the answer is a confident "no such
        ///  file" for a file that is plainly there.
        /// </summary>
        private static IEnumerable<string> DocumentRoots()
        {
            string byShell = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(byShell) && System.IO.Path.IsPathRooted(byShell))
                yield return byShell;

            string profile = Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrWhiteSpace(profile) && System.IO.Path.IsPathRooted(profile))
                yield return System.IO.Path.Combine(profile, "Documents");

            //  Last resort: the conventional location for whoever we are running as.
            string user = Environment.UserName;
            if (!string.IsNullOrWhiteSpace(user))
                yield return System.IO.Path.Combine(@"C:\Users", user, "Documents");
        }

        /// <summary>
        /// Everywhere we would look, in order, whether or not it exists.  Kept
        ///  separate so that a failure can say what it tried.
        /// </summary>
        private static IEnumerable<string> CandidatePaths()
        {
            if (!string.IsNullOrWhiteSpace(OverridePath))
            {
                yield return OverridePath;
                yield break;
            }

            foreach (string root in DocumentRoots())
                foreach (string dir in CandidateDirs)
                    yield return System.IO.Path.Combine(root, dir, "Settings", "MaxIm CCD", "SetupFilterWheel.txt");
        }

        public static string Path
        {
            get
            {
                foreach (string candidate in CandidatePaths())
                    if (File.Exists(candidate))
                        return candidate;

                return null;
            }
        }

        /// <summary>
        /// The name MaxIm has for a wheel position, or null when it has none.
        /// </summary>
        public static string ForPosition(int position)
        {
            lock (_lock)
            {
                Refresh();

                //  MaxIm counts slots from 1, we count positions from 0.
                if (_names.TryGetValue(position + 1, out string name) && !string.IsNullOrWhiteSpace(name))
                    return name;

                return null;
            }
        }

        private static void Refresh()
        {
            try
            {
                string path = Path;

                if (path == null)
                {
                    _names = new Dictionary<int, string>();
                    _lastWriteSeen = DateTime.MinValue;

                    //
                    // Say WHERE we looked.  "not found" on its own sent us hunting
                    //  for a file that was sitting there the whole time, because the
                    //  place we were looking was not the place we thought.
                    //
                    Provenance = "MaxIm DL: SetupFilterWheel.txt not found in " +
                        string.Join("; ", CandidatePaths().Select(p => System.IO.Path.GetDirectoryName(p)).Distinct());
                    return;
                }

                DateTime written = File.GetLastWriteTime(path);
                if (path == _pathSeen && written == _lastWriteSeen)
                    return;                                     // unchanged since we last read it

                _names = Parse(File.ReadAllLines(path));
                _pathSeen = path;
                _lastWriteSeen = written;

                string version = new DirectoryInfo(path).Parent?.Parent?.Parent?.Name ?? "MaxIm DL";
                Provenance = $"{version} ({_names.Count} named filters, saved {written:yyyy-MM-dd HH:mm})";
            }
            catch (Exception ex)
            {
                _names = new Dictionary<int, string>();
                _lastWriteSeen = DateTime.MinValue;
                Provenance = $"MaxIm DL: {ex.Message}";
            }
        }

        private static Dictionary<int, string> Parse(string[] lines)
        {
            Dictionary<int, string> names = new Dictionary<int, string>();
            Regex re = new Regex($@"^Camera{CameraIndex}_ASCOMFilterF(?<slot>\d+)\s+""(?<name>[^""]*)""\s*$");

            foreach (string line in lines)
            {
                Match m = re.Match(line);
                if (!m.Success)
                    continue;

                string name = m.Groups["name"].Value.Trim();
                if (name.Length == 0)                           // an unused slot
                    continue;

                names[int.Parse(m.Groups["slot"].Value)] = name;
            }

            return names;
        }
    }
}
