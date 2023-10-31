using DSharpPlus;
using ImageMagick;
using MedicBot.Utils;
using Serilog;

namespace MedicBot;
public static class ImageManager
{
    // TODO Remove if unused
    private static DiscordClient Client { get; set; } = null!;
    private static string ImagesPath { get; set; } = null!;
    private static string TempFilesPath { get; set; } = null!;

    public static void Init(DiscordClient client, string imagesPath, string tempFilesPath)
    {
        Client = client;
        ImagesPath = imagesPath;
        TempFilesPath = tempFilesPath;
    }

    public static async Task AddAsync(string imageName, ulong userId, string url)
    {
        // TODO: De-duplicate this code
        // Middleware/pre-processor to handle incoming files?
        // File downloader?
        // Some kind of repository interface to call .NameExists() without knowing whether incoming file is AudioTrack or Image.
        // OR : Generic class/methods to do this

        // Maybe don't do any of the above idk

        if (!imageName.IsValidFileName())
        {
            Log.Warning("{Filename} has invalid characters", imageName);
            throw new ArgumentException($"Filename: {imageName} has invalid characters.");
        }

        if (url.IndexOf('?') != -1)
        {
            url = url[..url.IndexOf('?')];
        }

        if (url.LastIndexOf('.') == -1 || string.IsNullOrWhiteSpace(url[url.LastIndexOf('.')..]))
        {
            Log.Warning("Discord attachment doesn't have a file extension");
            throw new ArgumentException(
                "The file you sent has no extension. Please add a valid extension to the file before sending it.");
        }

        if (ImageRepository.NameExists(imageName))
        {
            Log.Warning("An Image with the name {imageName} already exists", imageName);
            throw new ImageExistsException($"An Image with the name {imageName} already exists.");
        }

        var fileExtension = url[url.LastIndexOf('.')..];
        Log.Information("Detected file extension: {FileExtension}", fileExtension);
        var fileName = imageName + fileExtension;
        var filePath = string.Join('/', ImagesPath, fileName);

        {
            Log.Information("Downloading file to {FilePath}", filePath);
            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync(url);
            await using var fileStream = File.OpenWrite(filePath);
            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
        }

        if (new FileInfo(filePath).Length > 500 * 1024)
        {
            Log.Information("File size larger than 500KB, converting to WebP");
            var originalFilePath = filePath;
            filePath = ConvertImage(originalFilePath);
            Log.Information("Deleting original file");
            File.Delete(originalFilePath);
        }

        ImageRepository.Add(new ReactionImage() {Name = imageName, Path = filePath, OwnerId = userId});
    }

    private static string ConvertImage(string imagePath)
    {
        var imageName = imagePath[..imagePath.IndexOf('.')];
        var newImagePath = imageName + ".webp";
        using var magick = new MagickImage(imagePath);
        magick.Quality = 95;
        magick.WriteAsync(newImagePath);
        return newImagePath;
    }

    public static FileStream GetAsync(string imageName)
    {
        // TODO Switch to fuzzy search for regular strings, keep exact search for quoted (" ") strings
        var image = ImageRepository.FindByNameExact(imageName);
        if (image == null)
        {
            Log.Warning("No image was found with name: {Name}", imageName);
            throw new ImageNotFoundException(
                $"No image was found with name: {imageName}");
        }
        return File.OpenRead(image.Path);
    }
}
