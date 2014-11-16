using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nu.Concurrency
{
    public class BufferedChannel<T> : IDisposable
    {
        private readonly Queue<T> queue = new Queue<T>();
        private readonly ManualResetEvent queueEvent = new ManualResetEvent(false);
        private bool closed;
        private bool disposed;

        public bool Send(T item)
        {
            if (disposed) throw new ChannelDisposedException();
            if (closed)
            {
                return false;
            }
            lock (queue)
            {
                queue.Enqueue(item);
                queueEvent.Set();
                return true;
            }
        }

        public bool Receive(out T item)
        {
            if(disposed) throw new ChannelDisposedException();
            lock (queue)
            {
                if (closed && queue.Count == 0)
                {
                    item = default(T);
                    return false;
                }
                queueEvent.WaitOne();
                if(disposed) throw new ChannelDisposedException();
                item = queue.Dequeue();
                if (queue.Count == 0)
                {
                    queueEvent.Reset();
                }
                return true;
            }
        }

        public void Close()
        {
            closed = true;
        }

        public void Dispose()
        {
            disposed = true;
            queueEvent.Set();
            queueEvent.Dispose();
        }

        public static BufferedChannel<T> Merge(params Channel<T>[] channels)
        {
            var bufferedChannel = new BufferedChannel<T>();

            var tasks = channels.ToList().Select(x => Task.Factory.StartNew(() =>
            {
                T item;
                while (x.Receive(out item))
                {
                    bufferedChannel.Send(item);
                }
                
            })).ToList();

            Task.Factory.StartNew(() =>
            {
                tasks.ForEach(x => x.Wait());
                bufferedChannel.Close();
            });
            return bufferedChannel;
        }

    }
}