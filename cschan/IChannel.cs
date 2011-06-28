namespace cschan
{
    public interface IChannel<T>
    {
        ChannelResult<T> Put(T item);
        ChannelResult<T> Get();
    }
}