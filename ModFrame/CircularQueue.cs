using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseToKill
{
    class CircularQueue<T> : Queue<T>
    {
        private int capacity;

        public CircularQueue (int capacity): base(capacity)
        {
            this.capacity = capacity;
        }

        public new void Enqueue(T item)
        {
            if (this.Count() >= capacity)
            {
                this.Dequeue();
            }
            base.Enqueue(item);
        }
        
    }
}
