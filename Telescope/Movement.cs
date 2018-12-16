using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.DeviceInterface;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;


namespace ASCOM.Wise40
{
    public class MovementSpecifier: IEquatable<Tuple<TelescopeAxes, double, Const.AxisDirection>>
    {
        public readonly TelescopeAxes axis;
        public readonly Const.AxisDirection direction;

        public bool Equals(Tuple<TelescopeAxes, double, Const.AxisDirection> other)
        {
            if (other == null)
                return false;

            return other.Item1 == axis && other.Item3 == direction;
        }

        public override bool Equals(object other)
        {
            bool ret = false;

            if (other == null)
                return false;

            if (other.GetType() != this.GetType())
                return false;

            var o = (MovementSpecifier) other;
            ret = o.axis == axis && o.direction == direction;
            return ret;
        }

        public static implicit operator Tuple<TelescopeAxes, Const.AxisDirection> (MovementSpecifier m)
        {
            return new Tuple<TelescopeAxes, Const.AxisDirection>(m.axis, m.direction);
        }

        public static implicit operator MovementSpecifier(Tuple<TelescopeAxes, Const.AxisDirection> t)
        {
            return new MovementSpecifier(t.Item1, t.Item2);
        }

        public TelescopeAxes Axis {  get { return axis; } }
        public Const.AxisDirection Direction { get { return direction; } }

        public MovementSpecifier(TelescopeAxes axis, Const.AxisDirection direction)
        {
            this.axis = axis;
            this.direction = direction;
        }

        public override int GetHashCode()
        {
            return axis.GetHashCode() ^ direction.GetHashCode();
        }

        public override string ToString()
        {
            return "( " + axis.ToString() + ", " + ", " + direction.ToString() + " )";
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }

    public class MovementWorker : IEquatable<Tuple<WiseVirtualMotor, bool>>
    {
        public readonly WiseVirtualMotor[] motors;
        public readonly bool slew;

        public MovementWorker(WiseVirtualMotor[] motors)
        {
            this.motors = motors;
        }

        public bool Equals(Tuple<WiseVirtualMotor, bool> other)
        {
            return other.Item1.Equals(motors) && other.Item2.Equals(slew);
        }

        public override int GetHashCode()
        {
            return motors.GetHashCode() ^ (slew.GetHashCode() << 1);
        }

        public override string ToString()
        {
            string s = "( ";
            foreach (WiseVirtualMotor m in motors)
                s += m.WiseName + " ";
            s += " )";
            return s;
        }
    }

    public class MovementDictionary : IDictionary<MovementSpecifier, MovementWorker>
    {
        private Dictionary<MovementSpecifier, MovementWorker> dict;

        public MovementDictionary()
        {
            dict = new Dictionary<MovementSpecifier, MovementWorker>();
        }

        public MovementWorker this[MovementSpecifier key]
        {
            get
            {
                return ((IDictionary<MovementSpecifier, MovementWorker>)dict)[key];
            }

            set
            {
                ((IDictionary<MovementSpecifier, MovementWorker>)dict)[key] = value;
            }
        }

        public int Count
        {
            get
            {
                return ((IDictionary<MovementSpecifier, MovementWorker>)dict).Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((IDictionary<MovementSpecifier, MovementWorker>)dict).IsReadOnly;
            }
        }

        public ICollection<MovementSpecifier> Keys
        {
            get
            {
                return ((IDictionary<MovementSpecifier, MovementWorker>)dict).Keys;
            }
        }

        public ICollection<MovementWorker> Values
        {
            get
            {
                return ((IDictionary<MovementSpecifier, MovementWorker>)dict).Values;
            }
        }

        public void Add(KeyValuePair<MovementSpecifier, MovementWorker> item)
        {
            ((IDictionary<MovementSpecifier, MovementWorker>)dict).Add(item);
        }

        public void Add(MovementSpecifier key, MovementWorker value)
        {
            ((IDictionary<MovementSpecifier, MovementWorker>)dict).Add(key, value);
        }

        public void Clear()
        {
            ((IDictionary<MovementSpecifier, MovementWorker>)dict).Clear();
        }

        public bool Contains(KeyValuePair<MovementSpecifier, MovementWorker> item)
        {
            return ((IDictionary<MovementSpecifier, MovementWorker>)dict).Contains(item);
        }

        public bool ContainsKey(MovementSpecifier key)
        {
            return ((IDictionary<MovementSpecifier, MovementWorker>)dict).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<MovementSpecifier, MovementWorker>[] array, int arrayIndex)
        {
            ((IDictionary<MovementSpecifier, MovementWorker>)dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<MovementSpecifier, MovementWorker>> GetEnumerator()
        {
            return ((IDictionary<MovementSpecifier, MovementWorker>)dict).GetEnumerator();
        }

        public bool Remove(KeyValuePair<MovementSpecifier, MovementWorker> item)
        {
            return ((IDictionary<MovementSpecifier, MovementWorker>)dict).Remove(item);
        }

        public bool Remove(MovementSpecifier key)
        {
            return ((IDictionary<MovementSpecifier, MovementWorker>)dict).Remove(key);
        }

        public bool TryGetValue(MovementSpecifier key, out MovementWorker value)
        {
            return ((IDictionary<MovementSpecifier, MovementWorker>)dict).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<MovementSpecifier, MovementWorker>)dict).GetEnumerator();
        }

        public override string ToString()
        {
            string s = null;

            foreach (MovementSpecifier spec in dict.Keys)
            {
                MovementWorker worker = dict[spec];

                s += "  dict[" + spec.axis.ToString() + ", " + ", " + spec.direction.ToString() + " ] = { [ ";
                foreach (WiseVirtualMotor m in worker.motors)
                    s += m.ToString() + " ";
                s += "], " + worker.slew.ToString() + "} " + spec.GetHashCode() + "\n";
            }
            return s;
        }
    }
}
