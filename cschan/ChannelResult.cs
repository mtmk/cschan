namespace cschan
{
    public class ChannelResult<T>
    {
        public ChannelResult(T item, bool failed, bool timedout, bool exited, string message)
        {
            Item = item;
            Failed = failed;
            Timedout = timedout;
            Exited = exited;
            Message = message;
        }

        public T Item { get; private set; }
        public bool Failed { get; private set; }
        public bool Timedout { get; private set; }
        public bool Exited { get; private set; }
        public string Message { get; private set; }
    }
}