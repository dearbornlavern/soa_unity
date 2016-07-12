using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace soa
{
    public abstract class ThreadWorker
    {
        private readonly Thread thread;
        protected bool alive;

        public ThreadWorker()
        {
            this.thread = new Thread(doWork);
            this.alive = false;
        }

        protected abstract void doWork();

        public bool isAlive()
        {
            return alive;
        }

        public virtual void Start()
        {
            alive = true;
            thread.Start();
        }

        public virtual void Stop()
        {
            alive = false;
            thread.Join();
        }
    }
}
