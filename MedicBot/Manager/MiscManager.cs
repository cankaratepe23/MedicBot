using HtmlAgilityPack;

namespace MedicBot.Manager;

public class MiscManager : IMiscManager
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MiscManager(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetSelcukSportsUrlAsync()
    {
        var selcukUrl = "https://selcuksportshd78.biz";
        var client = _httpClientFactory.CreateClient();
        await using var stream = await client.GetStreamAsync(selcukUrl);
        using var streamReader = new StreamReader(stream);
        var responseString = await streamReader.ReadToEndAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(responseString);
        var root = doc.DocumentNode ?? throw new FormatException("Could not find root node, probably error in HTML parsing.");
        var selcukStreamUrl = root.SelectSingleNode("//div/div/a[1]").Attributes["href"].Value;
        return selcukStreamUrl;
    }
}
