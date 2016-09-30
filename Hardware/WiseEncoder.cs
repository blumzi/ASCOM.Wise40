using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    //[StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// The masks must be contiguous bits, between 0x1 and 0xff (or 0xf)
    /// </summary>
    public struct WiseEncSpec
    {
        public WiseBoard brd;
        public MccDaq.DigitalPortType port;
        public byte mask;
    };

    /// <summary>
    /// Wraps a hardware encoder with:
    ///  - atomic read of the MccDaqs
    ///  - simulated values
    ///  - Gray code handling
    /// </summary>
    //public class WiseEncoder : IWiseObject, IConnectable, IDisposable, ISimulated
    public class WiseEncoder : WiseObject, IConnectable, IDisposable
    {
        //private string _name;
        private List<WiseDaq> _daqs;
        private List<byte> _masks;
        private int _nbits;
        private bool _isGray;
        private int _hwTicks;
        private bool _connected = false;
        //protected bool _simulated = Environment.MachineName.ToLower() != "dome-ctlr";
        private AtomicReader _atomicReader;
        protected Common.Debugger debugger = Common.Debugger.Instance;

        public WiseEncoder() { }

        public WiseEncoder(string name,
            int hwTicks,
            List<WiseEncSpec> specs,
            bool isGray = false,
            double timeoutMillis = Const.defaultReadTimeoutMillis,
            int retries = Const.defaultReadRetries)
        {
            init(name, hwTicks, specs, isGray, timeoutMillis, retries);
        }

        /// <summary>
        /// Initializes a WiseEncoder
        /// </summary>
        /// <param name="name"></param>
        /// <param name="hwTicks"></param>
        /// <param name="specs"></param>
        /// <param name="isGray"></param>
        /// <param name="timeoutMillis"></param>
        /// <param name="retries"></param>
        public void init(string name,
            int hwTicks,
            List<WiseEncSpec> specs,
            bool isGray = false,
            double timeoutMillis = Const.defaultReadTimeoutMillis,
            int retries = Const.defaultReadRetries)
        {
            int nSpecs = specs.Count();
            _daqs = new List<WiseDaq>(nSpecs);
            _masks = new List<byte>(nSpecs);
            _isGray = isGray;
            _hwTicks = hwTicks;
            Name = name;
            
            foreach (WiseEncSpec spec in specs)
            {
                WiseDaq daq;
                byte mask;

                if ((daq = spec.brd.daqs.Find(x => x.porttype == spec.port)) == null)
                    throw new WiseException(name + ": Cannot find Daq for " + spec.port + " on " + spec.brd.Name);

                mask = (byte)((spec.mask == 0) ? ~(1 << daq.nbits) : spec.mask);
                daq.setDir(MccDaq.DigitalPortDirection.DigitalIn);
                for (int bit = 0; bit < daq.nbits; bit++)
                {
                    if ((mask & (1 << bit)) != 0)
                    {
                        _nbits++;
                    }
                }
                _daqs.Add(daq);
                _masks.Add(mask);
            }
            _atomicReader = new AtomicReader(Name, _daqs, timeoutMillis, retries);
        }

        public uint Value
        {
            get
            {
                uint ret = 0;
                List<uint> values = _atomicReader.Values;

                foreach (uint v in values)
                    ret = (ret << 8) | (v & _masks[values.IndexOf(v)]);
                if (_isGray)
                    ret = GrayCode[ret];
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugEncoders, "{0}: value: {1}", Name, ret);
                #endregion

                return ret;
            }
        }

        public List<uint> RawValues
        {
            get
            {
                return _atomicReader.Values;
            }
        }

        #region GrayCode
        protected static ushort[] GrayCode = new ushort[1024] {
            0,    1,    3,    2,    7,    6,    4,    5,   15,   14,
            12,   13,    8,    9,   11,   10,   31,   30,   28,   29,
            24,   25,   27,   26,   16,   17,   19,   18,   23,   22,
            20,   21,   63,   62,   60,   61,   56,   57,   59,   58,
            48,   49,   51,   50,   55,   54,   52,   53,   32,   33,
            35,   34,   39,   38,   36,   37,   47,   46,   44,   45,
            40,   41,   43,   42,  127,  126,  124,  125,  120,  121,
           123,  122,  112,  113,  115,  114,  119,  118,  116,  117,
            96,   97,   99,   98,  103,  102,  100,  101,  111,  110,
           108,  109,  104,  105,  107,  106,   64,   65,   67,   66,
            71,   70,   68,   69,   79,   78,   76,   77,   72,   73,
            75,   74,   95,   94,   92,   93,   88,   89,   91,   90,
            80,   81,   83,   82,   87,   86,   84,   85,  255,  254,
           252,  253,  248,  249,  251,  250,  240,  241,  243,  242,
           247,  246,  244,  245,  224,  225,  227,  226,  231,  230,
           228,  229,  239,  238,  236,  237,  232,  233,  235,  234,
           192,  193,  195,  194,  199,  198,  196,  197,  207,  206,
           204,  205,  200,  201,  203,  202,  223,  222,  220,  221,
           216,  217,  219,  218,  208,  209,  211,  210,  215,  214,
           212,  213,  128,  129,  131,  130,  135,  134,  132,  133,
           143,  142,  140,  141,  136,  137,  139,  138,  159,  158,
           156,  157,  152,  153,  155,  154,  144,  145,  147,  146,
           151,  150,  148,  149,  191,  190,  188,  189,  184,  185,
           187,  186,  176,  177,  179,  178,  183,  182,  180,  181,
           160,  161,  163,  162,  167,  166,  164,  165,  175,  174,
           172,  173,  168,  169,  171,  170,  511,  510,  508,  509,
           504,  505,  507,  506,  496,  497,  499,  498,  503,  502,
           500,  501,  480,  481,  483,  482,  487,  486,  484,  485,
           495,  494,  492,  493,  488,  489,  491,  490,  448,  449,
           451,  450,  455,  454,  452,  453,  463,  462,  460,  461,
           456,  457,  459,  458,  479,  478,  476,  477,  472,  473,
           475,  474,  464,  465,  467,  466,  471,  470,  468,  469,
           384,  385,  387,  386,  391,  390,  388,  389,  399,  398,
           396,  397,  392,  393,  395,  394,  415,  414,  412,  413,
           408,  409,  411,  410,  400,  401,  403,  402,  407,  406,
           404,  405,  447,  446,  444,  445,  440,  441,  443,  442,
           432,  433,  435,  434,  439,  438,  436,  437,  416,  417,
           419,  418,  423,  422,  420,  421,  431,  430,  428,  429,
           424,  425,  427,  426,  256,  257,  259,  258,  263,  262,
           260,  261,  271,  270,  268,  269,  264,  265,  267,  266,
           287,  286,  284,  285,  280,  281,  283,  282,  272,  273,
           275,  274,  279,  278,  276,  277,  319,  318,  316,  317,
           312,  313,  315,  314,  304,  305,  307,  306,  311,  310,
           308,  309,  288,  289,  291,  290,  295,  294,  292,  293,
           303,  302,  300,  301,  296,  297,  299,  298,  383,  382,
           380,  381,  376,  377,  379,  378,  368,  369,  371,  370,
           375,  374,  372,  373,  352,  353,  355,  354,  359,  358,
           356,  357,  367,  366,  364,  365,  360,  361,  363,  362,
           320,  321,  323,  322,  327,  326,  324,  325,  335,  334,
           332,  333,  328,  329,  331,  330,  351,  350,  348,  349,
           344,  345,  347,  346,  336,  337,  339,  338,  343,  342,
           340,  341, 1023, 1022, 1020, 1021, 1016, 1017, 1019, 1018,
          1008, 1009, 1011, 1010, 1015, 1014, 1012, 1013,  992,  993,
           995,  994,  999,  998,  996,  997, 1007, 1006, 1004, 1005,
          1000, 1001, 1003, 1002,  960,  961,  963,  962,  967,  966,
           964,  965,  975,  974,  972,  973,  968,  969,  971,  970,
           991,  990,  988,  989,  984,  985,  987,  986,  976,  977,
           979,  978,  983,  982,  980,  981,  896,  897,  899,  898,
           903,  902,  900,  901,  911,  910,  908,  909,  904,  905,
           907,  906,  927,  926,  924,  925,  920,  921,  923,  922,
           912,  913,  915,  914,  919,  918,  916,  917,  959,  958,
           956,  957,  952,  953,  955,  954,  944,  945,  947,  946,
           951,  950,  948,  949,  928,  929,  931,  930,  935,  934,
           932,  933,  943,  942,  940,  941,  936,  937,  939,  938,
           768,  769,  771,  770,  775,  774,  772,  773,  783,  782,
           780,  781,  776,  777,  779,  778,  799,  798,  796,  797,
           792,  793,  795,  794,  784,  785,  787,  786,  791,  790,
           788,  789,  831,  830,  828,  829,  824,  825,  827,  826,
           816,  817,  819,  818,  823,  822,  820,  821,  800,  801,
           803,  802,  807,  806,  804,  805,  815,  814,  812,  813,
           808,  809,  811,  810,  895,  894,  892,  893,  888,  889,
           891,  890,  880,  881,  883,  882,  887,  886,  884,  885,
           864,  865,  867,  866,  871,  870,  868,  869,  879,  878,
           876,  877,  872,  873,  875,  874,  832,  833,  835,  834,
           839,  838,  836,  837,  847,  846,  844,  845,  840,  841,
           843,  842,  863,  862,  860,  861,  856,  857,  859,  858,
           848,  849,  851,  850,  855,  854,  852,  853,  512,  513,
           515,  514,  519,  518,  516,  517,  527,  526,  524,  525,
           520,  521,  523,  522,  543,  542,  540,  541,  536,  537,
           539,  538,  528,  529,  531,  530,  535,  534,  532,  533,
           575,  574,  572,  573,  568,  569,  571,  570,  560,  561,
           563,  562,  567,  566,  564,  565,  544,  545,  547,  546,
           551,  550,  548,  549,  559,  558,  556,  557,  552,  553,
           555,  554,  639,  638,  636,  637,  632,  633,  635,  634,
           624,  625,  627,  626,  631,  630,  628,  629,  608,  609,
           611,  610,  615,  614,  612,  613,  623,  622,  620,  621,
           616,  617,  619,  618,  576,  577,  579,  578,  583,  582,
           580,  581,  591,  590,  588,  589,  584,  585,  587,  586,
           607,  606,  604,  605,  600,  601,  603,  602,  592,  593,
           595,  594,  599,  598,  596,  597,  767,  766,  764,  765,
           760,  761,  763,  762,  752,  753,  755,  754,  759,  758,
           756,  757,  736,  737,  739,  738,  743,  742,  740,  741,
           751,  750,  748,  749,  744,  745,  747,  746,  704,  705,
           707,  706,  711,  710,  708,  709,  719,  718,  716,  717,
           712,  713,  715,  714,  735,  734,  732,  733,  728,  729,
           731,  730,  720,  721,  723,  722,  727,  726,  724,  725,
           640,  641,  643,  642,  647,  646,  644,  645,  655,  654,
           652,  653,  648,  649,  651,  650,  671,  670,  668,  669,
           664,  665,  667,  666,  656,  657,  659,  658,  663,  662,
           660,  661,  703,  702,  700,  701,  696,  697,  699,  698,
           688,  689,  691,  690,  695,  694,  692,  693,  672,  673,
           675,  674,  679,  678,  676,  677,  687,  686,  684,  685,
           680,  681,  683,  682
                 };
        #endregion

        public void Connect(bool connected)
        {
            int encBit = _nbits - 1;
            
            foreach (var daq in _daqs)
                for (int daqBit = daq.nbits - 1; daqBit >= 0; daqBit--)
                    if ((_masks[_daqs.IndexOf(daq)] & (1 << daqBit)) != 0)
                        if (connected)
                            daq.setOwner(Name + "[" + encBit-- + "]", daqBit);
                        else
                            daq.unsetOwner(daqBit);

            _connected = connected;
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public void Dispose()
        {
            for (int daqno = 0; daqno < _daqs.Count(); daqno++)
                for (int bit = 0; bit < _daqs[daqno].nbits; bit++)
                    if ((_masks[daqno] & (1 << bit)) != 0)
                        _daqs[daqno].unsetOwner(bit);
        }
    }
}
