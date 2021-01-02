using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using System.Threading;

namespace RemoteSafetyDashboard
{
    public class Transaction
    {
        private long busy;
        public string verb;
        public string location;
        public string parameters;
        public string response;

        public bool Busy
        {
            get
            {
                return Interlocked.Read(ref busy) == 1;
            }

            set
            {
                Interlocked.Exchange(ref busy, value ? 1 : 0);
            }
        }

        public string Status { get; set; }

        public Statuser.Severity Severity { get; set; }
    }

    public class AscomClient
    {
        private readonly HTTPCommunicator httpCommunicator;
        public Transaction transaction;
        private bool connected = false;

        public AscomClient(string uri, string shortName)
        {
            Uri = uri;
            Name = shortName;
            httpCommunicator = new HTTPCommunicator(this);
        }

        public string Uri { get; }

        public string Name { get; }

        private string Communicating
        {
            get
            {
                return $"Communicating with {Name} ...";
            }
        }

        public bool Connected
        {
            get
            {
                if (Busy)
                    return connected;

                transaction = new Transaction
                {
                    verb = "GET",
                    location = "connected",
                    Status = Communicating,
                    Severity = Statuser.Severity.Normal,
                    Busy = true,
                };

                connected = httpCommunicator.Connected;
                return connected;
            }

            set
            {
                transaction = new Transaction
                {
                    verb = "PUT",
                    location = "connected",
                    parameters = $"Connected={value}",
                    Status = Communicating,
                    Severity = Statuser.Severity.Normal,
                    Busy = true,
                };

                connected = value;
                httpCommunicator.Connected = connected;
            }
        }

        public string Action(string action, string parameters)
        {
            transaction = new Transaction
            {
                verb = "PUT",
                location = "action",
                parameters = $"Action={action}&Parameters={parameters}",
                Status = Communicating,
                Severity = Statuser.Severity.Normal,
                Busy = true,
            };

            return httpCommunicator.Action(action, parameters);
        }

        public string Status
        {
            get
            {
                return transaction?.Status ?? "";
            }

            set
            {
                if (transaction != null)
                    transaction.Status = value;
            }
        }

        public Statuser.Severity Severity {
            get
            {
                return transaction?.Severity ?? Statuser.Severity.Normal;
            }

            set
            {
                if (transaction != null)
                    transaction.Severity = value;
            }
        }

        public bool Busy
        {
            get
            {
                return transaction?.Busy ?? false;
            }

            set
            {
                if (transaction != null)
                    transaction.Busy = value;
            }
        }
    }
}
