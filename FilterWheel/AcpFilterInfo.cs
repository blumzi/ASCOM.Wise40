using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ASCOM.Wise40
{
    /// <summary>
    /// Reads ACP's FilterInfo.txt, which is where the observer maintains focus
    ///  offsets and autofocus parameters.  Wise40 no longer keeps its own copy of
    ///  any of this.
    ///
    /// The file is POSITIONAL - one line per filter wheel slot, in slot order -
    ///  and carries no filter names at all.  Whatever names appear after the ';'
    ///  are comments for the human reading the file, and in ours they are
    ///  placeholders (c1..c8), so they are deliberately not used here.
    ///
    /// Format, per ACP's own SampleFilterInfo.txt, with the last two optional:
    ///
    ///     offset, ref-filt, ptg-filt, af-minmag, af-maxmag
    ///
    /// Reading is best effort.  The file may be absent, shorter than the wheel
    ///  mounted, or longer.  Callers take the entries that exist and treat the
    ///  rest as unknown - see Entries and Entry.Valid.
    /// </summary>
    public static class AcpFilterInfo
    {
        //
        // ACP's own config directory.  "Public Documents" is what Explorer shows
        //  for this; the path on disk has no such folder.
        //
        public static string Path { get; set; } =
            @"C:\Users\Public\Documents\ACP Config\FilterInfo.txt";

        public class Entry
        {
            public int Offset;
            public int ReferenceFilter;
            public int PointingFilter;
            public double? AutofocusMinMag;
            public double? AutofocusMaxMag;

            /// <summary>
            /// False when the line could not be parsed.  Such a line still takes
            ///  up its place in the list: the file is positional, so dropping it
            ///  would silently shift every slot after it.
            /// </summary>
            public bool Valid;
        }

        private static readonly object _lock = new object();
        private static List<Entry> _entries = new List<Entry>();
        private static DateTime _lastWriteSeen = DateTime.MinValue;

        /// <summary>
        /// Where the last answer came from, and how it went.  Meant to be shown
        ///  next to the values, so nobody has to guess whether a zero offset is
        ///  ACP's opinion or our fallback.
        /// </summary>
        public static string Provenance { get; private set; } = "not read yet";

        /// <summary>
        /// The entries, one per slot, re-read only when the file changes.
        /// </summary>
        public static List<Entry> Entries
        {
            get
            {
                lock (_lock)
                {
                    try
                    {
                        if (!File.Exists(Path))
                        {
                            _entries = new List<Entry>();
                            _lastWriteSeen = DateTime.MinValue;
                            Provenance = $"{Path}: not found";
                            return _entries;
                        }

                        DateTime lastWrite = File.GetLastWriteTime(Path);
                        if (lastWrite != _lastWriteSeen)
                        {
                            _entries = Parse(File.ReadAllLines(Path));
                            _lastWriteSeen = lastWrite;
                            Provenance = $"ACP FilterInfo.txt ({_entries.Count} slots, saved {lastWrite:yyyy-MM-dd HH:mm})";
                        }
                    }
                    catch (Exception ex)
                    {
                        _entries = new List<Entry>();
                        _lastWriteSeen = DateTime.MinValue;
                        Provenance = $"{Path}: {ex.Message}";
                    }

                    return _entries;
                }
            }
        }

        /// <summary>
        /// The entry for a slot, or null when the file says nothing about it -
        ///  four lines against an eight position wheel, or none at all.
        /// </summary>
        public static Entry ForPosition(int position)
        {
            List<Entry> entries = Entries;

            if (position < 0 || position >= entries.Count)
                return null;

            Entry entry = entries[position];
            return entry.Valid ? entry : null;
        }

        private static List<Entry> Parse(string[] lines)
        {
            List<Entry> entries = new List<Entry>();

            foreach (string line in lines)
            {
                //
                // ';' starts a comment.  A line that is only a comment, or blank,
                //  is not a slot and must not take up an index.
                //
                int semi = line.IndexOf(';');
                string data = (semi == -1 ? line : line.Substring(0, semi)).Trim();

                if (data.Length == 0)
                    continue;

                string[] fields = data.Split(',');
                Entry entry = new Entry();

                if (fields.Length >= 1 && int.TryParse(fields[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int offset))
                {
                    entry.Offset = offset;
                    entry.Valid = true;
                }

                if (fields.Length >= 2 && int.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int refFilt))
                    entry.ReferenceFilter = refFilt;

                if (fields.Length >= 3 && int.TryParse(fields[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int ptgFilt))
                    entry.PointingFilter = ptgFilt;

                if (fields.Length >= 4 && double.TryParse(fields[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double minMag))
                    entry.AutofocusMinMag = minMag;

                if (fields.Length >= 5 && double.TryParse(fields[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double maxMag))
                    entry.AutofocusMaxMag = maxMag;

                entries.Add(entry);
            }

            return entries;
        }
    }
}
