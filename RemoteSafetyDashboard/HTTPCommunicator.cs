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
    public class HTTPCommunicator
    {
        private readonly HttpClient _client;
        private readonly AscomClient _ascomClient;
        private static readonly Debugger debugger = Debugger.Instance;

        public HTTPCommunicator(AscomClient ascomClient)
        {
            _ascomClient = ascomClient;

            _client = new HttpClient();
            _client.DefaultRequestHeaders
                .Accept
                .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/html"));
            _client.DefaultRequestHeaders
                .Accept
                .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.ConnectionClose = false;
            _client.Timeout = TimeSpan.FromSeconds(30);
        }

        public bool Connected
        {
            get
            {
                _ascomClient.Busy = true;
                Task.Run(() => Communicate()).ContinueWith((_) => _ascomClient.Busy = false);

                while (_ascomClient.Busy)
                    Thread.Sleep(100);

                string response = _ascomClient.transaction.response;
                return string.Equals(response, "true", StringComparison.OrdinalIgnoreCase);
            }

            set
            {
                _ascomClient.Busy = true;
                Task.Run(() => Communicate()).ContinueWith((_) => _ascomClient.Busy = false);

                while (_ascomClient.Busy)
                    Thread.Sleep(100);
            }
        }

        public string Action(string action, string parameters)
        {
            _ascomClient.Busy = true;
            Task.Run(() => Communicate()).ContinueWith((_) => _ascomClient.Busy = false);

            while (_ascomClient.Busy)
                Thread.Sleep(500);

            return _ascomClient.transaction.response;
        }

        private async void Communicate()
        {
            string uri = _ascomClient.Uri + _ascomClient.transaction.location;
            #region debug
            string here = $"Communicate(verb: {_ascomClient.transaction.verb}, uri: {uri}";
            #endregion

            HttpResponseMessage httpResponse;

            here += (_ascomClient.transaction.verb == "GET") ?
                ") " :
                _ascomClient.transaction.parameters == null ?
                    ") " :
                    $", parameters: {_ascomClient.transaction.parameters}) ";

            try
            {
                if (_ascomClient.transaction.verb == "GET")
                {
                    httpResponse = await _client.GetAsync(uri).ConfigureAwait(false);
                }
                else
                {
                    httpResponse = await _client.PutAsync(uri,
                        new StringContent(_ascomClient.transaction.parameters, Encoding.UTF8, "application/x-www-form-urlencoded"))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _ascomClient.Status = $"Communicate: {ex.Message}";
                _ascomClient.Severity = Statuser.Severity.Error;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugWise, $"Communicate: Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
                return;
            }

            _ascomClient.Severity = Statuser.Severity.Error;
            try {
                if (httpResponse?.IsSuccessStatusCode == true) {
                    //httpResponseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    //if (httpResponseBody == null)
                    //{
                    //    #region debug
                    //    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "httpResponseBody: NULL");
                    //    #endregion
                    //    return;
                    //}        
                    var contentStream = await httpResponse.Content.ReadAsStreamAsync();

                    var streamReader = new System.IO.StreamReader(contentStream);
                    var jsonReader = new JsonTextReader(streamReader);

                    JsonSerializer serializer = new JsonSerializer();

                    ASCOMResponse ascomResponse = null;
                    try
                    {
                        ascomResponse = serializer.Deserialize<ASCOMResponse>(jsonReader);
                    }
                    catch (JsonReaderException ex)
                    {
                        Exceptor.Throw<Exception>($"{here}", $"{ex.Message}");
                    }

                    if (ascomResponse.ErrorNumber != 0)
                        Exceptor.Throw<Exception>($"{here}", $"{ascomResponse.ErrorMessage}");
                    else if (! String.IsNullOrEmpty(ascomResponse.DriverException))
                        Exceptor.Throw<Exception>($"{here}", $"{ascomResponse.DriverException}");

                    _ascomClient.transaction.response = ascomResponse.Value;
                    _ascomClient.Status = "";
                    _ascomClient.Severity = Statuser.Severity.Good;
                }
            }
            catch (HttpRequestException ex)
            {
                _ascomClient.Status = $"Communicate: {ex.Message}";
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{here}: Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
            catch (TaskCanceledException ex)
            {
                _ascomClient.Status = "Communicate: Timedout";
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{here}: Caught {ex.Message} (timeout) at\n{ex.StackTrace}");
                #endregion
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    ex = ex.InnerException;

                _ascomClient.Status = $"Communicate: {ex.Message}";
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{here}: Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
            finally
            {
                httpResponse?.Dispose();
            }
            _ascomClient.Busy = false;
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
