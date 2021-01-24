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
        private static readonly Debugger debugger = Debugger.Instance;

        public HTTPCommunicator()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders
                .Accept
                .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/html"));
            _client.DefaultRequestHeaders
                .Accept
                .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.ConnectionClose = false;
            _client.Timeout = TimeSpan.FromSeconds(10);
        }

        public void SendSync(AscomTransaction t)
        {
            string uri = t.uri + t.location;
            #region debug
            string op = $"Send(verb: {t.verb}, uri: {uri}";

            if (t.verb == "GET")
                op += ") ";
            else
            {
                if (t.parameters == null)
                    op += ") ";
                else
                    op += $", parameters: {t.parameters}) ";
            }
            #endregion

            HttpRequestMessage httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
            };

            if (t.verb == "GET")
                httpRequest.Method = HttpMethod.Get;
            else
            {
                httpRequest.Method = HttpMethod.Put;
                httpRequest.Content = new StringContent(t.parameters);
            }

            t.Error = null;
            try
            {
                t.state = AscomTransaction.State.Pending;
                using (HttpResponseMessage response = _client.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead).Result)
                {
                    using (HttpContent content = response.Content)
                    {
                        string json = content.ReadAsStringAsync().Result;
                        t.response = JsonConvert.DeserializeObject<ASCOMResponse>(json);
                        t.state = AscomTransaction.State.Succeeded;
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                t.state = AscomTransaction.State.Failed;
                t.Error = $"Timedout";
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugWise, $"{op}: Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
            catch (Exception ex)
            {
                t.state = AscomTransaction.State.Failed;
                t.Error = $"Exception: {ex.Message}";
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugWise, $"{op}: Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
        }
    }
}
