// Steven Reeves
// Assignment 1 
// CST_352

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assign1
{
    class Program
    {
        private const int BUFFER_SIZE = 5;
        private const int N_Producers = 2;
        private const int N_Consumers = 2;

        static void Main(string[] args)
        {
            /*
            SafeRing ring = new SafeRing(10);
            ring.Insert(586);
            Console.WriteLine("Inserted, count = " + ring.Count());
            ring.Insert(42);
            Console.WriteLine("Inserted, count = " + ring.Count());
            int i = ring.Remove();
            Console.WriteLine("Removed " + i + ", count:" + ring.Count());
            int hat = ring.Count();
            Console.WriteLine("Removed " + hat + ", count:" + ring.Count());
            */
            SafeRing ring = new SafeRing(BUFFER_SIZE);
            List<Producer> producers = new List<Producer>();
            List<Consumer> consumers = new List<Consumer>();


            for (int i = 0; i < N_Producers; i++)
            {
                Producer p = new Producer(ring, 10);
                producers.Add(p);
                p.Start();
            }

            for (int i = 0; i < N_Consumers; i++)
            {
                Consumer c = new Consumer(ring);
                consumers.Add(c);
                c.Start();
            }

            // TODO : wait for all producers to complete!
        }
    }
}
