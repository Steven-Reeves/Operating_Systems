// Steven Reeves
// Assignment 1 
// CST_352

using System;
using System.Collections.Generic;
using System.Threading;

namespace Assign1
{
    class Producer
    {
        private SafeRing queue;
        private ManualResetEvent complete;
        private Random rand;
        private int numItems;
        private Thread thread;

        public Producer(SafeRing queue, int itemNum, Random rand)
        {
            this.queue = queue;
            this.rand = rand;
            this.numItems = itemNum;
            complete = new ManualResetEvent(false);
            thread = new Thread(ThreadProc);
        }

        private static void ThreadProc(object param)
        {
            Producer p = param as Producer;
            p.Produce();
        }

        public void Start()
        {
            //start your thread!
            thread.Start(this);
        }

        private void Produce()
        {
            for (int i = 0; i < numItems; i++)
            {
                int num = rand.Next(1, 1000);
                queue.Insert(num);

                Thread.Sleep(num);
            }
            // Signal you're complete
            complete.Set();
        }

        public WaitHandle Complete { get { return complete; } }
    }
}
