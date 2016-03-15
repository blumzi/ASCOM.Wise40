using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.DeviceInterface;
using ASCOM.Wise40.Hardware;


namespace ASCOM.Wise40
{
    public class MovementSpecifier: IEquatable<Tuple<TelescopeAxes, double, int>>
    {
        public readonly TelescopeAxes axis;
        public readonly double rate;
        public readonly int sign;

        public bool Equals(Tuple<TelescopeAxes, double, int> other)
        {
            if (other == null)
                return false;

            //Console.WriteLine("Equals: this# {0} other# {1}", this.GetHashCode(), other.GetHashCode());
            return other.Item1 == axis && other.Item2 == rate && other.Item3 == sign;
        }

        public override bool Equals(object other)
        {
            bool ret = false;

            if (other == null)
                return false;

            if (other.GetType() != this.GetType())
                return false;

            var o = (MovementSpecifier) other;
            ret = o.axis == axis && o.rate == rate && o.sign == sign;
            //Console.WriteLine("Equals: this({0}, {1}, {2}) other: ({3}, {4}, {5}) => {6}", axis, rate, sign, o.axis, o.rate, o.sign, ret);
            return ret;
        }

        public static implicit operator Tuple<TelescopeAxes, double, int> (MovementSpecifier m)
        {
            return new Tuple<TelescopeAxes, double, int>(m.axis, m.rate, m.sign);
        }

        public static implicit operator MovementSpecifier(Tuple<TelescopeAxes, double, int> t)
        {
            return new MovementSpecifier(t.Item1, t.Item2, t.Item3);
        }

        public TelescopeAxes Axis {  get { return axis; } }
        public double Rate { get { return rate; } }
        public int Sign { get { return sign; } }

        public MovementSpecifier(TelescopeAxes axis, double rate, int sign)
        {
            this.axis = axis;
            this.rate = rate;
            this.sign = sign;
        }

        public override int GetHashCode()
        {
            return axis.GetHashCode() ^ rate.GetHashCode() ^ sign.GetHashCode();
        }

        public override string ToString()
        {
            return "( " + axis.ToString() + ", " + rate.ToString() + ", " + sign.ToString() + " )";
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }

    public class MovementWorker : IEquatable<Tuple<WiseMotor, bool>>
    {
        public readonly WiseMotor[] motors;
        public readonly bool slew;

        public MovementWorker(WiseMotor[] motors, bool slew)
        {
            this.motors = motors;
            this.slew = slew;
        }

        public bool Equals(Tuple<WiseMotor, bool> other)
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
            foreach (WiseMotor m in motors)
                s += m.name + " ";
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
                //Console.WriteLine("get: key# {0}", key.GetHashCode());
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

                s += "  dict[" + spec.axis.ToString() + ", " + spec.rate.ToString() + ", " + spec.sign.ToString() + " ] = { [ ";
                foreach (WiseMotor m in worker.motors)
                    s += m.ToString() + " ";
                s += "], " + worker.slew.ToString() + "} " + spec.GetHashCode() + "\n";
            }
            return s;
        }
    }
}
