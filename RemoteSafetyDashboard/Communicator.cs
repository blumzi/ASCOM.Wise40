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

    public class Communicator
    {
        public enum Type { ASCOM, Fake };
        private readonly Type _type;
        private readonly FakeSafeToOperateAccess fakeSafeToOperate;
        private readonly ASCOM.DriverAccess.SafetyMonitor safeToOperate = null;
        public Transaction transaction;
        private readonly string remoteAddress;
        private static readonly Debugger debugger = Debugger.Instance;

        public Communicator(Type type = Type.ASCOM, string address = "132.66.65.9")
        {
            _type = type;
            remoteAddress = address;
            if (type == Type.ASCOM)
                safeToOperate = new ASCOM.DriverAccess.SafetyMonitor("ASCOM.Remote1.SafetyMonitor");
            else
                fakeSafeToOperate = new FakeSafeToOperateAccess(this, address);
        }

        private string Communicating
        {
            get
            {
                return $"Communicating with {remoteAddress} ...";
            }
        }

        public bool Connected
        {
            get
            {
                transaction = new Transaction
                {
                    verb = "GET",
                    location = "connected",
                    Status = Communicating,
                    Severity = Statuser.Severity.Normal,
                    Busy = true,
                };

                return (_type == Type.ASCOM) ? safeToOperate.Connected : fakeSafeToOperate.Connected;
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

                if (_type == Type.ASCOM)
                    safeToOperate.Connected = value;
                else
                    fakeSafeToOperate.Connected = value;
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

            string response = _type == Type.ASCOM
                ? safeToOperate.Action(transaction.location, transaction.parameters)
                : fakeSafeToOperate.Action(/* transaction.location, transaction.parameters */);
            //#region debug
            //debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
            //    $"Action(action: {action}, parameters: {parameters}) => response: {response}");
            //#endregion
            return response;
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
