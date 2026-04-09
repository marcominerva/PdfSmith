using PdfSmith.BusinessLayer.Services.Interfaces;

namespace PdfSmith.BusinessLayer.Services;

public class ClientTimeProvider(ITimeZoneService timeZoneService) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => timeZoneService.GetTimeZone() ?? TimeZoneInfo.Utc;
}
