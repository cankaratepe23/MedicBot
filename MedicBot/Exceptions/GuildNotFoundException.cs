namespace MedicBot.Exceptions;

public class GuildNotFoundException : Exception
{
    public GuildNotFoundException()
    {
    }

    public GuildNotFoundException(string? message) : base(message)
    {
    }

    public GuildNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}