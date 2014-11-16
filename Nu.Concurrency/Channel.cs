using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nu.Concurrency
{
    // Select
    public class Channel<T> : IDisposable
    {
        private readonly AutoResetEvent sendEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent retreiveEvent = new AutoResetEvent(false);
        private int waiting;
        private readonly object sendLock = new object();
        private readonly object retreiveLock = new object();
        private bool closed;
        private bool disposed;
        private T nextItem;

        public bool Send(T item)
        {
            if (disposed)
            {
                throw new ChannelDisposedException();
            }
            if (closed)
            {
                return false;
            }
            waiting++;
            lock (sendLock)
            {
                nextItem = item;
                sendEvent.Set();
                retreiveEvent.WaitOne();
                if(disposed) throw new ChannelDisposedException();
                return true;
            }
        }

        public bool Retreive(out T item, int timeout = -1)
        {
            return Retrevie(out item, TimeSpan.FromMilliseconds(timeout));
        }

        public bool Retrevie(out T item, TimeSpan timeout)
        {
            if(disposed) throw new ChannelDisposedException();
            if (closed && waiting == 0)
            {
                item = default(T);
                return false;
            }
            lock (retreiveLock)
            {
                if (sendEvent.WaitOne(timeout))
                {
                    if (disposed) throw new ChannelDisposedException();
                    item = nextItem;
                    waiting--;

                    retreiveEvent.Set();
                    return true;
                }
                item = default (T);
                return false;
            } 
        }

        public void Close()
        {
            closed = true;
        }

        public void Dispose()
        {
            disposed = true;
            sendEvent.Dispose();
            retreiveEvent.Dispose();
        }
        
        #region Static Methods

        public static Channel<T> Merge(params Channel<T>[] channels)
        {
            var channel = new Channel<T>();
            var tasks = channels.ToList().Select(x => Task.Factory.StartNew(() =>
            {
                T item;
                while (x.Retreive(out item))
                {
                    channel.Send(item);
                }
                
            })).ToList();

            Task.Factory.StartNew(() =>
            {
                tasks.ForEach(x => x.Wait());
                channel.Close();
            });
            return channel;
        }

        #endregion

    }
}
