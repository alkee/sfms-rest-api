using System.Text.Json;

namespace sfms_rest_api;

public class FileMeta
{
    public string OriginalFileName { get; set; } = "";

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
    public static FileMeta FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new FileMeta();
        return JsonSerializer.Deserialize<FileMeta>(json)!;
    }
}
