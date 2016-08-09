using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40.Common
{
    public class FixedSizedQueue<T>
    {
        private object _lock = new object();
        ConcurrentQueue<T> queue;

        public int MaxSize { get; set; }

        public FixedSizedQueue(int maxSize, IEnumerable<T> items = null)
        {
            this.MaxSize = maxSize;
            if (items == null)
            {
                queue = new ConcurrentQueue<T>();
            }
            else
            {
                queue = new ConcurrentQueue<T>(items);
                EnsureLimitConstraint();
            }
        }

        public void Enqueue(T obj)
        {
            queue.Enqueue(obj);
            EnsureLimitConstraint();
        }

        private void EnsureLimitConstraint()
        {
            if (queue.Count > MaxSize)
            {
                lock (_lock)
                {
                    T overflow;
                    while (queue.Count > MaxSize)
                    {
                        queue.TryDequeue(out overflow);
                    }
                }
            }
        }


        /// <summary>
        /// returns the current snapshot of the queue
        /// </summary>
        /// <returns>Array of T</returns>
        public T[] ToArray()
        {
            return queue.ToArray();
        }
    }
}
