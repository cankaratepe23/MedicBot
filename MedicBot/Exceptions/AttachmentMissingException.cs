namespace MedicBot.Exceptions;

public class AttachmentMissingException : Exception
{
    public AttachmentMissingException()
    {
    }

    public AttachmentMissingException(string? message) : base(message)
    {
    }

    public AttachmentMissingException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}