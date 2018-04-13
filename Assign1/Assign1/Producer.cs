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
        private ManualResetEvent complete;

        public Producer(SafeRing queue, int itemNum)
        {
            //  TODO: Produce()
        }

        public void Start()
        {
            //  TODO: Start()
        }

        public WaitHandle Complete { get { return complete; } }
    }
}
