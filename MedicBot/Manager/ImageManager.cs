using DSharpPlus;
using ImageMagick;
using MedicBot.Exceptions;
using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Manager;

public class ImageManager : IImageManager
{
    private readonly DiscordClient _client;
    private readonly IImageRepository _imageRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _imagesPath;
    private readonly string _tempFilesPath;

    public ImageManager(DiscordClient client, IImageRepository imageRepository, IHttpClientFactory httpClientFactory)
    {
        _client = client;
        _imageRepository = imageRepository;
        _httpClientFactory = httpClientFactory;
        _imagesPath = Constants.ImagesPath;
        _tempFilesPath = Constants.TempFilesPath;
    }

    public async Task AddAsync(string imageName, ulong userId, string url)
    {
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

        if (_imageRepository.NameExists(imageName))
        {
            Log.Warning("An Image with the name {imageName} already exists", imageName);
            throw new ImageExistsException($"An Image with the name {imageName} already exists.");
        }

        var fileExtension = url[url.LastIndexOf('.')..];
        Log.Information("Detected file extension: {FileExtension}", fileExtension);
        var fileName = imageName + fileExtension;
        var filePath = string.Join('/', _imagesPath, fileName);

        {
            var httpClient = _httpClientFactory.CreateClient();
            Log.Information("Downloading file to {FilePath}", filePath);
            await using var stream = await httpClient.GetStreamAsync(url);
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

        _imageRepository.Add(new ReactionImage() { Name = imageName, Path = filePath, OwnerId = userId });
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

    public async Task<IEnumerable<ReactionImage>> FindAsync(string searchQuery, long limit = 10)
    {
        string? tag = null;
        searchQuery = searchQuery.Trim();
        if (searchQuery.Contains(':'))
        {
            var splitQuery = searchQuery.Split(':');
            tag = splitQuery[0];
            searchQuery = splitQuery[1].Trim();
        }

        if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery == "?")
        {
            if (limit == 1)
            {
                var randomImage = await _imageRepository.Random(tag);
                return new List<ReactionImage> {randomImage};
            }

            return _imageRepository.All(tag);
        }

        if (searchQuery.StartsWith('\"') && searchQuery.EndsWith('"'))
        {
            return _imageRepository.FindAllByName(searchQuery.Trim('"'), tag);
        }

        return await _imageRepository.FindMany(searchQuery, limit, tag);
    }

    public async Task<FileStream> FindAndOpenAsync(string imageName)
    {
        var image = (await FindAsync(imageName)).FirstOrDefault();
        if (image == null)
        {
            Log.Warning("No image was found with name: {Name}", imageName);
            throw new ImageNotFoundException(
                $"No image was found with name: {imageName}");
        }
        return OpenImage(image);
    }

    public FileStream OpenImage(ReactionImage image)
    {
        return File.OpenRead(image.Path);
    }

    public ReactionImage FindExact(string imageName)
    {
        var image = _imageRepository.FindByNameExact(imageName);
        if (image == null)
        {
            Log.Warning("No image was found with name: {Name}", imageName);
            throw new ImageNotFoundException(
                $"No image was found with name: {imageName}");
        }

        return image;
    }

    public async Task<string> DeleteAsync(ReactionImage image, ulong userId)
    {
        if (image.OwnerId != userId && _client.CurrentUser.Id != userId)
        {
            Log.Warning("A non-owner or non-admin user {UserId} attempted deleting the following image: {@Image}",
                userId, image);
            var user = await _client.GetUserAsync(userId);
            if (user != null)
            {
                Log.Warning("Offending user of the unauthorized delete operation: {User}", user);
            }

            throw new UnauthorizedException("You need to be the owner of this reaction image to delete it.");
        }

        _imageRepository.Delete(image.Id);
        File.Delete(image.Path);

        return GetRandomDeletionResponse();
    }

    public async Task<string> DeleteAsync(string imageName, ulong userId)
    {
        var image = FindExact(imageName);
        return await DeleteAsync(image, userId);
    }

    private static string GetRandomDeletionResponse()
    {
        var responses = new string[]
        {
            "Poof! Deleted this masterpiece.",
            "Oopsie-doodle! That picture's now in the digital Bermuda Triangle.",
            "And just like that, the vanishing act of the century! Ta-da, it's gone!",
            "Say goodbye to the pixelated phantom - it has officially vanished!",
            "Deleted! If only Houdini could make files disappear as swiftly as I can.",
            "Out of sight, out of bytes - Abracadabra, it's history!",
            "Who needs a magic wand when you've got a delete button? Poof, gone!",
            "The delete button's my secret weapon - Shazam! Empty space!",
            "Picture, picture on the screen, who's the deleted one? Oh, it's you!",
            "That image just joined the ranks of 'Gone but not forgotten... because I have no backup!'",
            "Deleted in 3... 2... 1! Don't worry; I didn't call NASA for this mission."
        };
        return responses[new Random().Next(responses.Length)];
    }
}
