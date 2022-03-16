namespace MedicBot.Exceptions;

public class LavalinkNotConnectedException : Exception
{
    public LavalinkNotConnectedException()
    {
    }

    public LavalinkNotConnectedException(string? message) : base(message)
    {
    }

    public LavalinkNotConnectedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}