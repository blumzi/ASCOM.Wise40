using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;

using ASCOM.Wise40.Common;
using Newtonsoft.Json;
using System.Threading;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace RemoteSafetyDashboard
{
//    public class ASCOMTransaction
//    {
//        private long busy;
//        public string verb;
//        public string argument;
//        public string parameters;
//        public string response;
//        public string error;

//        public bool Busy
//        {
//            get
//            {
//                return Interlocked.Read(ref busy) == 1;
//            }

//            set
//            {
//                Interlocked.Exchange(ref busy, value == true ? 1 : 0);
//            }
//        }

//        public string Status { get; set; }

//        public Statuser.Severity Severity { get; set; }
//    }

    public class FakeSafeToOperateAccess
    {
        private static string _rootUri;
        private readonly HttpClient _client;
        private readonly Communicator _communicator;
        private static readonly Debugger debugger = Debugger.Instance;

        public FakeSafeToOperateAccess(Communicator communicator, string serverAddress)
        {
            _communicator = communicator;
            _rootUri = $"http://{serverAddress}:11111/api/v1/safetymonitor/0/";

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/html"));
            _client.DefaultRequestHeaders.ConnectionClose = false;
            _client.Timeout = TimeSpan.FromSeconds(10);
        }

        public bool Connected
        {
            get
            {
                Task.Run(() => Communicate()).ContinueWith((_) => _communicator.Busy = false);

                while (_communicator.Busy)
                    Thread.Sleep(100);

                string response = _communicator.transaction.response;
                return string.Equals(response, "true", StringComparison.OrdinalIgnoreCase);
            }

            set
            {
                Task.Run(() => Communicate()).ContinueWith((t) => _communicator.Busy = false);

                while (_communicator.Busy)
                    Thread.Sleep(100);
            }
        }

        public string Action(/* string action, string parameters */)
        {
            Task.Run(() => Communicate()).ContinueWith((t) => _communicator.Busy = false);

            while (_communicator.Busy)
                Thread.Sleep(500);

            return _communicator.transaction.response;
        }

        private async void Communicate()
        {
            string uri = _rootUri + _communicator.transaction.location;
            #region debug
            string here = 
                $"Communicate(verb: {_communicator.transaction.verb}, " +
                $"uri: {uri}";
            #endregion

            try
            {
                string httpResponseBody;
                HttpResponseMessage httpResponse;

                if (_communicator.transaction.verb == "GET")
                {
                    here += ") ";
                    httpResponse = await _client.GetAsync(uri).ConfigureAwait(false);
                }
                else
                {
                    here += _communicator.transaction.parameters == null ?
                        ") " :
                        $", parameters: {_communicator.transaction.parameters}) ";
                    httpResponse = await _client.PutAsync(uri,
                        new StringContent(_communicator.transaction.parameters, Encoding.UTF8, "application/x-www-form-urlencoded")).ConfigureAwait(false);
                }

                if (! httpResponse.IsSuccessStatusCode)
                {
                    _communicator.Status = $"Communicate: HTTP code {httpResponse.StatusCode}";
                    _communicator.Severity = Statuser.Severity.Error;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{here}: Bad HTTP status code: {httpResponse.StatusCode}");
                    #endregion
                    return;
                }
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                //#region debug
                //debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{here}: httpResponseBody: \"{httpResponseBody}\"");
                //#endregion
                ASCOMResponse ascomResponse = JsonConvert.DeserializeObject<ASCOMResponse>(httpResponseBody);

                if (ascomResponse.ErrorNumber != 0)
                    Exceptor.Throw<Exception>($"{here}", $"{ascomResponse.ErrorMessage}");
                else if (ascomResponse.DriverException != null)
                    Exceptor.Throw<Exception>($"{here}", $"{ascomResponse.DriverException}");

                _communicator.transaction.response = ascomResponse.Value;
                _communicator.Status = "";
                #region debug
                if (_communicator.transaction.response == null)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{here}: null Value");
                }
                #endregion
            }
            catch (HttpRequestException ex)
            {
                _communicator.Status = $"Communicate: {ex.Message}";
                _communicator.Severity = Statuser.Severity.Error;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{here}: Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
            catch (TaskCanceledException ex)
            {
                _communicator.Status = "Communicate: Timedout";
                _communicator.Severity = Statuser.Severity.Error;
                #region debug
                Debugger.Instance.WriteLine(Debugger.DebugLevel.DebugLogic, $"{here}: Caught {ex.Message} (timeout) at\n{ex.StackTrace}");
                #endregion
            }
            catch (Exception ex)
            {
                _communicator.Status = $"Communicate: {ex.Message}";
                _communicator.Severity = Statuser.Severity.Error;
                #region debug
                Debugger.Instance.WriteLine(Debugger.DebugLevel.DebugLogic, $"{here}: Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
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
