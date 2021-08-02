using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;

namespace ASCOM.Wise40.Common
{
    public class PeriodicHttpFetcher
    {
        private HttpClient _client = null;
        private static readonly Debugger debugger = Debugger.Instance;
        private readonly string _url;
        private readonly HttpMethod _method;
        private readonly HttpContent _content;
        private readonly Timer _timer;
        private string _result;
        private readonly int _tries;
        private TimeSpan _period;
        private bool _clientPropertiesHaveChanged = false;
        private bool _enabled;
        private readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(30);
        public static readonly Exceptor HTTPExceptor = new Exceptor(Debugger.DebugLevel.DebugHTTP);

        public PeriodicHttpFetcher(string name,
            string url,
            TimeSpan? period = null,
            int tries = 1,
            int maxAgeMillis = 0,
            int dueMillis = 0,
            string method = "GET",
            string content = null)
        {
            _tries = tries;
            Period = (period == null) ? TimeSpan.Zero : (TimeSpan) period;
            MaxAge = (maxAgeMillis == 0) ? defaultTimeout : TimeSpan.FromMilliseconds(maxAgeMillis);
            MakeHttpClient();

            switch (method)
            {
                case "GET":
                    _method = HttpMethod.Get;
                    break;
                case "PUT":
                    _method = HttpMethod.Put;
                    break;
            }
            if (_method == HttpMethod.Put)
                _content = new StringContent(content);
            _url = url;
            _enabled = true;
            _timer = new Timer(OnTimer, this, dueMillis, Timeout.Infinite);
            Name = $"PeriodicHttpFetcher(\"{name}\")";
        }

        private void MakeHttpClient()
        {
            _client?.Dispose();

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/html"));
            _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.ConnectionClose = false;
            _client.Timeout = TimeSpan.FromMilliseconds(Period == TimeSpan.Zero ? defaultTimeout.TotalMilliseconds : Period.TotalMilliseconds * .8);

            _clientPropertiesHaveChanged = false;
        }

        private static void OnTimer(object state)
        {
            (state as PeriodicHttpFetcher)?.Fetch();
        }

        public void Trigger()
        {
            _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        public string Name { get; set; }

        // Did the last fetch succeed?
        public bool Alive { get; set; } = false;

        public string Result
        {
            get
            {
                string op = Name + ".Response.get";

                if (LastSuccess == DateTime.MinValue)
                    HTTPExceptor.Throw<InvalidValueException>(op, "Value never fetched!");
                else if (Stale)
                    HTTPExceptor.Throw<InvalidValueException>(op, $"Value is stale: age: {Age.ToMinimalString()} > {MaxAge.ToMinimalString()}");

                return _result;
            }

            set
            {
                _result = value;
            }
        }

        public DateTime LastSuccess { get; set; } = DateTime.MinValue;

        public DateTime LastAttempt { get; set; } = DateTime.MinValue;
        public DateTime LastFailure { get; set; } = DateTime.MinValue;

        public TimeSpan Age
        {
            get
            {
                return DateTime.Now.Subtract(LastSuccess);
            }
        }

        public bool Stale {
            get {
                return Age > MaxAge;
            }
        }

        public TimeSpan MaxAge { get; set; } = TimeSpan.Zero;

        public void Fetch()
        {
            #region debug
            string op = $"{Name}.Fetch() ";
            #endregion

            if (_clientPropertiesHaveChanged)
                MakeHttpClient();

            for (Tries = 0; Tries < _tries; Tries++)
            {
                DateTime start = DateTime.Now;
                LastAttempt = start;
                Duration = TimeSpan.Zero;

                try
                {
                    using (HttpRequestMessage httpRequest = new HttpRequestMessage
                    {
                        RequestUri = new Uri(_url),
                        Method = _method,
                    })
                    {
                        if (_method == HttpMethod.Put || _method == HttpMethod.Post)
                            httpRequest.Content = _content;

                        using (HttpResponseMessage response = _client.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead).Result)
                        {
                            using (HttpContent content = response.Content)
                            {
                                Result = content.ReadAsStringAsync().Result;
                                LastSuccess = DateTime.Now;
                                Alive = true;
                                CauseOfDeath = null;
                                Successes++;
                                Duration = DateTime.Now.Subtract(start);
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: Succeeded after {Tries+1} tries, duration: {Duration.ToMinimalString()}.");
                                #endregion
                                break;      // tries loop
                            }
                        }
                    }
                }
                catch (AggregateException ae)
                {
                    foreach (Exception ex in ae.InnerExceptions)
                    {
                        if (ex is TaskCanceledException)
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugWise, $"{op}: Timedout: {ex.Message} at\n{ex.StackTrace}");
                            #endregion
                            Alive = false;
                            CauseOfDeath = "Timeout";
                        }
                        else if (ex is HttpRequestException)
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugWise, $"{op}: Network error: {ex.Message} at\n{ex.StackTrace}");
                            #endregion
                            Alive = false;
                            CauseOfDeath = $"HTTP error ({ex.Message})";
                            Failures++;
                            LastFailure = DateTime.Now;
                        }
                        else
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugWise, $"{op}: Caught: {ex.Message} at\n{ex.StackTrace}");
                            #endregion
                            Alive = false;
                            CauseOfDeath = $"Exception ({ex.Message})";
                            Failures++;
                            LastFailure = DateTime.Now;
                        }
                    }
                }
                finally
                {
                    DateTime now = DateTime.Now;

                    LastAttempt = now;
                    if (Duration == TimeSpan.Zero)      // last transaction threw an exception
                        Duration = now.Subtract(start);
                    try
                    {
                        _timer.Change(
                            Period == TimeSpan.Zero ?
                                Timeout.Infinite :
                                (int)Period.TotalMilliseconds,
                            Timeout.Infinite);
                    }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        public TimeSpan Period
        {
            get
            {
                return _period;
            }

            set
            {
                _period = value;
                _clientPropertiesHaveChanged = true;
            }
        }

        /// <summary>
        /// Time span of the latest transaction (either successful or failed)
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.MaxValue;
        /// <summary>
        /// Number of successful transactions
        /// </summary>
        public int Successes { get; set; } = 0;
        /// <summary>
        /// Number of failed transactions
        /// </summary>
        public int Failures { get; set; } = 0;

        /// <summary>
        /// How many tries did it take to succeed (last transaction)
        /// </summary>
        public int Tries { get; set; } = 0;

        public string CauseOfDeath { get; set; } = null;

        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                bool wasEnabled = _enabled;

                if (wasEnabled && !value)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                else if (!wasEnabled && value)
                {
                    _timer.Change((int) Period.TotalMilliseconds, Timeout.Infinite);
                }

                _enabled = value;
            }
        }
    }

    public class AscomServerFetcher : PeriodicHttpFetcher
    {
        public AscomServerFetcher() : base(
            name: "AscomServerFetcher",
            url: Const.RESTServer.top + "concurrency"
            ) { }

        public int Concurrency
        {
            get
            {
                try
                {
                    Trigger();
                    ASCOMResponse ascomResponse = JsonConvert.DeserializeObject<ASCOMResponse>(this.Result);
                    return Convert.ToInt32(ascomResponse.Value);
                }
                catch (Exception ex)
                {
                    HTTPExceptor.Throw<InvalidOperationException>("AscomServerFetcher.Concurrency", $"Caught: {ex.Message}");
                    return -1;
                }
            }
        }
    }

    public class ASCOMResponse
    {
        public string Value;
        public int ClientTransactionID;
        public int ServerTransactionID;
        public int ErrorNumber;
        public string ErrorMessage;
        public string DriverException;
    }
}
