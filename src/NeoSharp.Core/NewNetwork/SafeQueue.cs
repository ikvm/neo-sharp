using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NeoSharp.Core.NewNetwork
{
    public class SafeQueue<T> 
    {
        #region Private Fields 
        private readonly ConcurrentQueue<T> _safeList = new ConcurrentQueue<T>();

        private ReaderWriterLockSlim _readerWriteLockSlim = new ReaderWriterLockSlim();
        private AutoResetEvent _waitForQueueToChangeEvent = new AutoResetEvent(false);
        #endregion

        #region Public Methods
        public void Enqueue(T item)
        {
            this._readerWriteLockSlim.EnterWriteLock();
            this._safeList.Enqueue(item);
            this._readerWriteLockSlim.ExitWriteLock();

            this._waitForQueueToChangeEvent.Set();
        }

        public T Dequeue()
        {
            if (!this._safeList.Any()) return default(T);

            var dequeueItem = default(T);

            this._readerWriteLockSlim.EnterWriteLock();
            this._safeList.TryDequeue(out dequeueItem);
            this._readerWriteLockSlim.ExitWriteLock();

            return dequeueItem;
        }

        public void WaitForQueueToChange()
        {
            this._waitForQueueToChangeEvent.WaitOne();
        }
        #endregion
    }
}