using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerLibrary.MultiThreading
{
    public class WaitReleaseSemaphore //: IDisposable
    {
        private readonly AsyncMutex asyncMutex;
        private int timeout;
        private bool caughtByThis = false;
        public WaitReleaseSemaphore(AsyncMutex sema, int timeout = 600000)
        {
            asyncMutex = sema;
            this.timeout = timeout;
        }

        public async Task<WaitReleaseSemaphore> Wait(Func<bool, Task> action)
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.CancelAfter(timeout);

            START:
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await action(true);
                    }
                    finally
                    {
                        Dispose();
                    }
                    return this;
                }

                int prevKey = Interlocked.CompareExchange(ref asyncMutex.key, 1, 0);
                if (prevKey == 0) //AsyncMutex was free, and we can take it and execute the function
                {
                    asyncMutex.keyLocal.Value++;
                    caughtByThis = true;
                    try
                    {
                        await action(false);
                    }
                    finally
                    {
                        Dispose();
                    }
                }
                else //we should wait for other tasks                    
                {
                    prevKey = Interlocked.CompareExchange(ref asyncMutex.key, asyncMutex.key + 1, asyncMutex.keyLocal.Value);
                    if (prevKey == asyncMutex.keyLocal.Value) //the AsyncMutex is caught by current task, so we can continue
                    {
                        asyncMutex.keyLocal.Value++;
                        caughtByThis = true;
                        try
                        {
                            await action(false);
                        }
                        finally { Dispose(); }
                    }
                    else if (asyncMutex.key < asyncMutex.keyLocal.Value)
                        throw new Exception("key is smaller than keyLocal. So, something is wrong");
                    else
                    {
                        await Task.Delay(50);
                        goto START;
                    }
                }

                return this;
            }
        }

        public void Dispose()
        {
            if (caughtByThis)
                Interlocked.Decrement(ref asyncMutex.key);
        }
    }
}
