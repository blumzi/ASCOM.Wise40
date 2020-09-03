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
        private readonly object _lock = new object();
        private ConcurrentQueue<T> queue;

        public int MaxSize { get; set; }

        public bool IsEmpty
        {
            get
            {
                return queue == null || queue.IsEmpty;
            }
        }

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
                    while (queue.Count > MaxSize)
                    {
                        queue.TryDequeue(out T _);
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

        public T TryPeek()
        {
            T value;

            while (!queue.TryPeek(out value))
                ;
            return value;
        }
    }
}
