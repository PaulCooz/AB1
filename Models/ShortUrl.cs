namespace AB1.Models;

public class ShortUrl
{
    public int Id { get; set; }

    public required string OriginalUrl { get; set; }

    public required string Hash { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int ClickCount { get; set; }
}
