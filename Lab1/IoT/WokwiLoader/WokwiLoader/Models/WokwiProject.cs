namespace WokwiLoader.Models;

public class WokwiProject
{
    public List<WokwiFile> Files { get; set; } = new();
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Unlisted { get; set; } = false;
}
