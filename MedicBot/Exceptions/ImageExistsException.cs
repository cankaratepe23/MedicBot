namespace MedicBot;
public class ImageExistsException : Exception
{
    public ImageExistsException()
    {
    }

    public ImageExistsException(string? message) : base(message)
    {
    }

    public ImageExistsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
