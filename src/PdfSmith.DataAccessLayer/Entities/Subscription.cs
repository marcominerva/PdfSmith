namespace PdfSmith.DataAccessLayer.Entities;

public class Subscription
{
    public Guid Id { get; set; }

    public string ApiKey { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset ValidTo { get; set; }

    public int RequestPerWindow { get; set; }

    public int WindowMinutes { get; set; }
}
