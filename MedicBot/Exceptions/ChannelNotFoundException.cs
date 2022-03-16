namespace MedicBot.Exceptions;

public class ChannelNotFoundException : Exception
{
    public ChannelNotFoundException()
    {
    }

    public ChannelNotFoundException(string? message) : base(message)
    {
    }

    public ChannelNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}