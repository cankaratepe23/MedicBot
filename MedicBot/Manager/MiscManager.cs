using HtmlAgilityPack;

namespace MedicBot;

public class MiscManager
{
    public static async Task<string> GetSelcukSportsUrlAsync()
    {
        var selcukUrl = "https://selcuksportshd78.biz";
        await using var stream = await Program.Client.GetStreamAsync(selcukUrl);
        using var streamReader = new StreamReader(stream);
        var responseString = await streamReader.ReadToEndAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(responseString);
        var root = doc.DocumentNode ?? throw new FormatException("Could not find root node, probably error in HTML parsing.");
        var selcukStreamUrl = root.SelectSingleNode("//div/div/a[1]").Attributes["href"].Value;
        return selcukStreamUrl;
    }
}
