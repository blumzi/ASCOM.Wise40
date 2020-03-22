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
        private HttpClient _client;
        Communicator _communicator;

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
                Task.Run(() => Communicate()).ContinueWith((t) => _communicator.Busy = false);

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

        public string Action(string action, string parameters)
        {
            Task.Run(() => Communicate()).ContinueWith((t) => _communicator.Busy = false);

            while (_communicator.Busy)
                Thread.Sleep(500);

            return _communicator.transaction.response;
        }

        private async void Communicate()
        {
            try
            {
                string httpResponseBody;
                string uri = _rootUri + _communicator.transaction.location;
                HttpResponseMessage httpResponse;

                if (_communicator.transaction.verb == "GET")
                {
                    httpResponse = await _client.GetAsync(uri);
                }
                else
                {
                    httpResponse = await _client.PutAsync(uri, new StringContent(_communicator.transaction.parameters, Encoding.UTF8, "application/x-www-form-urlencoded"));
                }
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                ASCOMResponse ascomResponse = JsonConvert.DeserializeObject<ASCOMResponse>(httpResponseBody);

                if (ascomResponse.ErrorNumber != 0)
                    throw new Exception($"{ascomResponse.ErrorMessage}");
                else if (ascomResponse.DriverException != null)
                    throw new Exception($"{ascomResponse.DriverException}");

                _communicator.transaction.response = ascomResponse.Value;
                _communicator.Status = "";
            }
            catch (HttpRequestException ex)
            {
                _communicator.Status = $"{ex.Message}";
                _communicator.Severity = Statuser.Severity.Error;
            }
            catch (TaskCanceledException ex)
            {
                _communicator.Status = "Timedout";
                _communicator.Severity = Statuser.Severity.Error;
            }
            catch (Exception ex)
            {
                _communicator.Status = $"{ex.Message}";
                _communicator.Severity = Statuser.Severity.Error;
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
