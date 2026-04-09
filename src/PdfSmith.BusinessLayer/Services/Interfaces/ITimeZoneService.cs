namespace PdfSmith.BusinessLayer.Services.Interfaces;

public interface ITimeZoneService
{
    string? GetTimeZoneHeaderValue();

    TimeZoneInfo? GetTimeZone();
}