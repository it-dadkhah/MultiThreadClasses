using System.Collections.Generic;
using System.Threading;

namespace ServerLibrary.MultiThreading
{
    public class AsyncMutex
    {
        public int key;
        public AsyncLocal<int> keyLocal = new AsyncLocal<int>();
    }
}
