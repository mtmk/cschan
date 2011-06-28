using System.Collections.Generic;
using System.Threading;

namespace cschan
{
    public class Channel<T> : IChannel<T>
    {
        private readonly WaitHandle _exitHandle;
        private readonly int _timeout;

        private readonly Semaphore _sPut;
        private readonly Semaphore _sGet;

        private readonly object _guard = new object();
        private readonly Queue<T> _q = new Queue<T>();

        public Channel(int capacity) : this(capacity, new ManualResetEvent(false)) { }

        public Channel(int capacity, WaitHandle exitHandle) : this(capacity, exitHandle, Timeout.Infinite) { }

        public Channel(int capacity, WaitHandle exitHandle, int timeout)
        {
            _exitHandle = exitHandle;
            _timeout = timeout;
            _sPut = new Semaphore(capacity, capacity);
            _sGet = new Semaphore(0, capacity);
        }

        public ChannelResult<T> Put(T item)
        {
            var waitAny = WaitHandle.WaitAny(new[] { _sPut, _exitHandle }, _timeout);
            if (waitAny == 1)
                return new ChannelResult<T>(default(T), true, false, true, "Exited");
            if (waitAny == WaitHandle.WaitTimeout)
                return new ChannelResult<T>(default(T), true, true, false, "Timedout");

            lock (_guard)
            {
                _sGet.Release();
                _q.Enqueue(item);
                return new ChannelResult<T>(item, false, false, false, "Enqueued");
            }
        }

        public ChannelResult<T> Get()
        {
            var waitAny = WaitHandle.WaitAny(new[] { _sGet, _exitHandle }, _timeout);
            if (waitAny == 1)
                return new ChannelResult<T>(default(T), true, false, true, "Exited");
            if (waitAny == WaitHandle.WaitTimeout)
                return new ChannelResult<T>(default(T), true, true, false, "Timedout");

            lock (_guard)
            {
                _sPut.Release();
                return new ChannelResult<T>(_q.Dequeue(), false, false, false, "Dequeued");
            }
        }
    }
}
