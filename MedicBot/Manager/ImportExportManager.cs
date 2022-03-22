using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
using Newtonsoft.Json;
using Serilog;

namespace MedicBot.Manager;

public static class ImportExportManager
{
    public static async Task<int> Import(string url)
    {
        var numberOfEntriesAdded = 0;
        var jsonString = "";
        {
            using var client = new HttpClient();
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
                Path.Combine(Constants.AudioTracksPath, audioEntry.FileName),
                audioEntry.OwnerId
            );
            Log.Verbose("Converted to audio track: {@AudioTrack}", audioTrack);
            AudioRepository.Add(audioTrack);
            Log.Verbose("Added the previous audio track");
            numberOfEntriesAdded++;
        }

        return numberOfEntriesAdded;
    }
}