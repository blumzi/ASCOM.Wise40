using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using System.Threading;

namespace RemoteSafetyDashboard
{
    public class AscomTransaction
    {
        public enum State { Init, Pending, Failed, Succeeded };

        public State state;
        public string verb;
        public string uri;
        public string location;
        public string parameters;
        public ASCOMResponse response;

        public string Error { get; set; }
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

    public class AscomClient
    {
        private readonly HTTPCommunicator httpCommunicator;
        private bool connected = false;
        private AscomTransaction t;

        public AscomClient(string uri)
        {
            Uri = uri;
            Name = uri.Remove(uri.Length - 3);
            Name = Name.Remove(0, Name.LastIndexOf('/'));
            httpCommunicator = new HTTPCommunicator();
        }

        public string Uri { get; }

        public string Name { get; }

        public bool Connected
        {
            get
            {
                if (Busy)
                    return connected;
                else
                {
                    t = new AscomTransaction {
                        state = AscomTransaction.State.Init,
                        verb = "GET",
                        uri = Uri,
                        location = "connected",
                    };

                    httpCommunicator.SendSync(t);
                    connected = Convert.ToBoolean(PostProcess(t));
                    t = null;

                    return connected;
                }
            }

            set
            {
                if (Busy)
                    return;
                else
                {
                    t = new AscomTransaction {
                        verb = "PUT",
                        uri = Uri,
                        location = "connected",
                        parameters = $"{value}",
                    };

                    httpCommunicator.SendSync(t);
                    PostProcess(t);
                    t = null;
                }
            }
        }

        public string Action(string action, string parameters)
        {
            string ret;

            if (Busy)
                return null;
            else
            {
                t = new AscomTransaction
                {
                    verb = "PUT",
                    uri = Uri,
                    location = "action",
                    parameters = $"Action={action}&Parameters={parameters}",
                };

                httpCommunicator.SendSync(t);
                ret = PostProcess(t);
                t = null;
            }
            return ret;
        }

        public string PostProcess(AscomTransaction t)
        {
            string op = "PostProcess";

            if (t == null)
            {
                Exceptor.Throw<Exception>(op, "NULL transaction");
                return null;
            }

            if (t.state == AscomTransaction.State.Succeeded)
            {
                return t.response.Value;
            }

            op += $" verb: {t.verb}, url: {t.uri + t.location}, parameters: {t.parameters}";

            if (t.Error != null)
                Exceptor.Throw<Exception>(op, $"Error: {t.Error}");

            if (t.response == null)
                Exceptor.Throw<Exception>(op, "Empty response");

            if (t.response.ErrorNumber != 0)
                Exceptor.Throw<Exception>(op, $"ErrorMessage: {t.response.ErrorMessage}");

            if (!String.IsNullOrEmpty(t.response.DriverException))
                Exceptor.Throw<Exception>(op, $"DriverException: {t.response.DriverException}");

            return null;
        }

        public bool Busy {
            get {
                return t != null && (t.state == AscomTransaction.State.Init || t.state == AscomTransaction.State.Pending);
            }
        }
    }
}
