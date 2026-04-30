using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
using Newtonsoft.Json;
using Serilog;

namespace MedicBot.Manager;

public class ImportExportManager : IImportExportManager
{
    private readonly IAudioRepository _audioRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    public ImportExportManager(IAudioRepository audioRepository, IHttpClientFactory httpClientFactory)
    {
        _audioRepository = audioRepository;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<int> Import(string url)
    {
        var numberOfEntriesAdded = 0;
        var client = _httpClientFactory.CreateClient();
        string jsonString;
        {
            await using var stream = await client.GetStreamAsync(url);
            using var streamReader = new StreamReader(stream);
            jsonString = await streamReader.ReadToEndAsync();
        }
        var deserializedObject = JsonConvert.DeserializeObject<Dictionary<string, LegacyAudioEntry>>(jsonString);
        if (deserializedObject == null)
        {
            Log.Warning("Could not convert JSON file when importing");
            throw new InvalidOperationException("Could not convert the JSON file");
        }

        foreach (var audioEntry in deserializedObject.Values)
        {
            Log.Verbose("Reading legacy audio entry: {@AudioEntry}", audioEntry);
            var audioTrack = new AudioTrack(
                audioEntry.Name,
                audioEntry.Aliases,
                audioEntry.Collections,
                string.Join('/', Constants.AudioTracksPath, audioEntry.FileName),
                audioEntry.OwnerId
            );
            Log.Verbose("Converted to audio track: {@AudioTrack}", audioTrack);
            _audioRepository.Add(audioTrack);
            Log.Verbose("Added the previous audio track");
            numberOfEntriesAdded++;
        }

        return numberOfEntriesAdded;
    }
}