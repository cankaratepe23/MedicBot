namespace MedicBot.Exceptions;

public class LavalinkLoadFailedException : Exception
{
    public LavalinkLoadFailedException()
    {
    }

    public LavalinkLoadFailedException(string? message) : base(message)
    {
    }

    public LavalinkLoadFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}