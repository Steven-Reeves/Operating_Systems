﻿// Steven Reeves
// Assignment 1 
// CST_352

using System;
using System.Collections.Generic;
using System.Threading;

namespace Assign1
{
    class Consumer
    {
        private SafeRing queue;
        private Random rand;
        private Thread thread;
        private bool stop = false;
        private int timeout;

        public Consumer(SafeRing queue, Random rand, int timeout = -1)
        {
            this.queue = queue;
            this.rand = rand;
            thread = new Thread(ThreadCon);
            this.timeout = timeout;
        }

        private static void ThreadCon(object param)
        {
            Consumer c = param as Consumer;
            c.Consume();
        }

        public void Start()
        {
            // Start Thread!
            thread.Start(this);

        }

        private void Consume()
        {
            while (!stop)
            {
                try
                {

                int num = queue.Remove(timeout);
                int nap = rand.Next(1, 1000);

                Thread.Sleep(nap);
                }
                catch(ThreadInterruptedException)
                {
                    //expected, done.
                }
                catch (TimeoutException te)
                {
                    Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " Error: " + te.Message);
                }
            }
        }

        public void Stop()
        {
            stop = true;
            thread.Interrupt();
        }
    }
}
