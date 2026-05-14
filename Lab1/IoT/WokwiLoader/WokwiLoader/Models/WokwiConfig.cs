namespace WokwiLoader.Models;

public class WokwiConfig
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public bool Unlisted { get; set; } = false;
    public string? Cookie { get; set; }
    public string? FolderPath { get; set; }
}
