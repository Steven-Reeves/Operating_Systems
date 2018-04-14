// Steven Reeves
// Assignment 1 
// CST_352

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Assign1
{
    class Program
    {
        private const int BUFFER_SIZE = 5;
        private const int PRODUCE_COUNT = 10;
        private const int TIMEOUT = 10;
        private const int N_Producers = 2;
        private const int N_Consumers = 2;

        static void Main(string[] args)
        {

            // get a random number
            Random rand = new Random();
            SafeRing ring = new SafeRing(BUFFER_SIZE);
            List<Producer> producers = new List<Producer>();
            List<WaitHandle> producersComplete = new List<WaitHandle>();
            List<Consumer> consumers = new List<Consumer>();


            for (int i = 0; i < N_Producers; i++)
            {
                Producer p = new Producer(ring, PRODUCE_COUNT, rand, TIMEOUT);
                producers.Add(p);
                producersComplete.Add(p.Complete);
                p.Start();
            }

            for (int i = 0; i < N_Consumers; i++)
            {
                Consumer c = new Consumer(ring, rand, TIMEOUT);
                consumers.Add(c);
                c.Start();
            }

            // wait for all producers to complete and stop consumers
            WaitHandle.WaitAll(producersComplete.ToArray());
            foreach(Consumer c in consumers)
            {
                c.Stop();
            }
        }
    }
}
