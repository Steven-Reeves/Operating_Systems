// Steven Reeves
// Assignment 1 
// CST_352

using System;
using System.Collections.Generic;
using System.Threading;

namespace Assign1
{
    class SafeRing
    {
        private int capacity;
        private int[] buffer;
        private int count;
        private Mutex mutex;
        private ManualResetEvent hasCapacity;
        private ManualResetEvent hasItems;

        public SafeRing(int capacity)
        {
            this.capacity = capacity;
            // Fail early, throw exceptions.
            if (capacity <= 0)
                throw new Exception("Positive capacity is needed!");

            buffer = new int[capacity];
            mutex = new Mutex();
            hasCapacity = new ManualResetEvent(true);
            hasItems = new ManualResetEvent(false);
        }

        public int Remove()
        {
            // Mutex and ManualResetEvent are derived from WaitHandle
            WaitHandle.WaitAll(new WaitHandle[] { mutex, hasItems });

            // TODO update this from lecture 1
            int i = buffer[head];
            head = (head + 1) % capacity;
            count--;

            hasCapacity.Set();

            if (count == 0)
                hasItems.Reset();

            Console.WriteLine("Removed: " + i + " !");

            // Release the mutex, so other threads can use it
            mutex.ReleaseMutex();

            return i;
        }

        public void Insert(int number)
        {
            WaitHandle.WaitAll(new WaitHandle[] { mutex, hasCapacity });

            // TODO update this from lecture 1
            buffer[tail] = number;
            tail = (tail + 1) % capacity;
            count++;

            Console.WriteLine("Inserted: " + number + " !");

            hasItems.Set();

            if (count == capacity)
                hasCapacity.Reset();

            // Release the mutex, so other threads can use it
            mutex.ReleaseMutex();

        }

        public int Count()
        {
            // Make sure that nothing is being inserted or removed
            mutex.WaitOne();

            // Use temp variable to store count
            int n = count;

            // Make sure to release the mutex before you return!
            mutex.ReleaseMutex();

            return n;
        }

    }
}
