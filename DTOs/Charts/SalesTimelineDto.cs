namespace APIlog.Server.DTOs.Charts;

public record SalesTimelinePointDto(string Date, int Count);
public record SalesTimelineSeriesDto(string Label, IEnumerable<SalesTimelinePointDto> Points);
