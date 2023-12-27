namespace MedicBot.Exceptions;

public class AudioTrackNotFoundException : Exception
{
    public AudioTrackNotFoundException()
    {
    }

    public AudioTrackNotFoundException(string? message) : base(message)
    {
    }

    public AudioTrackNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public AudioTrackNotFoundException(string audioName, bool useId) : base($"No track was found with {(useId ? "ID" : "name")}: {audioName} ")
    {
    }
}