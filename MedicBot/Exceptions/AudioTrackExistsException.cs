namespace MedicBot.Exceptions;

public class AudioTrackExistsException : Exception
{
    public AudioTrackExistsException()
    {
    }

    public AudioTrackExistsException(string? message) : base(message)
    {
    }

    public AudioTrackExistsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}