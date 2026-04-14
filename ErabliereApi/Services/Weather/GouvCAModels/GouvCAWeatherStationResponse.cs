namespace ErabliereApi.Services.GouvCAModels;

#pragma warning disable

public class Alert
{
}

public class Aqhi
{
    public string? url { get; set; }
}

public class AvgMaxTemp
{
    public object? metric { get; set; }
    public object? imperial { get; set; }
}

public class AvgMeanTemp
{
    public object? metric { get; set; }
    public object? imperial { get; set; }
}

public class AvgMinTemp
{
    public object? metric { get; set; }
    public object? imperial { get; set; }
}

public class Daily
{
    public string? date { get; set; }
    public string? summary { get; set; }
    public int periodID { get; set; }
    public string? periodLabel { get; set; }
    public WindChill? windChill { get; set; }
    public object? sun { get; set; }
    public string? temperatureText { get; set; }
    public object? humidex { get; set; }
    public string? precip { get; set; }
    public Frost? frost { get; set; }
    public string? titleText { get; set; }
    public Temperature? temperature { get; set; }
    public string? iconCode { get; set; }
    public string? text { get; set; }
}

public class DailyFcst
{
    public string? dailyIssuedTimeShrt { get; set; }
    public RegionalNormals? regionalNormals { get; set; }
    public List<Daily>? daily { get; set; }
    public string? dailyIssuedTime { get; set; }
    public string? dailyIssuedTimeEpoch { get; set; }
}

public class Datestamp
{
    public DateTime timeStamp { get; set; }
    public string? timeStampText { get; set; }
}

public class Dewpoint
{
    public string? imperial { get; set; }
    public string? imperialUnrounded { get; set; }
    public string? metric { get; set; }
    public string? metricUnrounded { get; set; }
}

public class ExtremeMaxTemp
{
    public double metric { get; set; }
    public double imperial { get; set; }
    public string? year { get; set; }
    public string? interval { get; set; }
}

public class ExtremeMinTemp
{
    public double metric { get; set; }
    public double imperial { get; set; }
    public string? year { get; set; }
    public string? interval { get; set; }
}

public class ExtremePrecip
{
    public double metric { get; set; }
    public double imperial { get; set; }
    public string? year { get; set; }
    public string? interval { get; set; }
}

public class ExtremeRainfall
{
    public object? metric { get; set; }
    public object? imperial { get; set; }
    public object? year { get; set; }
    public object? interval { get; set; }
}

public class ExtremeSnowfall
{
    public object? metric { get; set; }
    public object? imperial { get; set; }
    public object? year { get; set; }
    public object? interval { get; set; }
}

public class ExtremeSnowOnGround
{
    public object? metric { get; set; }
    public object? imperial { get; set; }
    public object? year { get; set; }
    public object? interval { get; set; }
}

public class FeelsLike
{
    public string? imperial { get; set; }
    public string? metric { get; set; }
}

public class Frost
{
    public string? textSummary { get; set; }
}

public class HighTemperature
{
    public double imperial { get; set; }
    public double metric { get; set; }
}

public class Hour
{
    public DateTime timeStamp { get; set; }
    public double temperature { get; set; }
    public double dewpoint { get; set; }
    public double pressure { get; set; }
    public int humidity { get; set; }
    public int windSpeed { get; set; }
    public string? windDirection { get; set; }
    public int? windGust { get; set; }
}

public class Hourly
{
    public string? date { get; set; }
    public int periodID { get; set; }
    public WindGust? windGust { get; set; }
    public string? windDir { get; set; }
    public FeelsLike? feelsLike { get; set; }
    public string? condition { get; set; }
    public string? precip { get; set; }
    public Temperature? temperature { get; set; }
    public string? iconCode { get; set; }
    public string? time { get; set; }
    public WindSpeed? windSpeed { get; set; }
    public int epochTime { get; set; }
    public string? dateShrt { get; set; }
}

public class HourlyFcst
{
    public string? hourlyIssuedTimeShrt { get; set; }
    public List<Hourly>? hourly { get; set; }
}

public class Imperial
{
    public int highTemp { get; set; }
    public int lowTemp { get; set; }
    public string? text { get; set; }
}

public class LowTemperature
{
    public double imperial { get; set; }
    public double metric { get; set; }
}

public class Metric
{
    public int highTemp { get; set; }
    public int lowTemp { get; set; }
    public string? text { get; set; }
}

public class Mtimes
{
    public int AQHI { get; set; }
    public int DAILY_FORECAST { get; set; }
    public int HOURLY_OBSERVATION { get; set; }
    public int HOURLY_FORECAST { get; set; }
    public int RISESET { get; set; }
    public int RISESET_NEXT_DAY { get; set; }
    public int PAST_HOURLY { get; set; }
    public int YESTERDAY_OBS { get; set; }
}

public class Observation
{
    public string? observedAt { get; set; }
    public string? provinceCode { get; set; }
    public string? climateId { get; set; }
    public string? tcid { get; set; }
    public DateTime timeStamp { get; set; }
    public string? timeStampText { get; set; }
    public string? iconCode { get; set; }
    public string? condition { get; set; }
    public Temperature? temperature { get; set; }
    public Dewpoint? dewpoint { get; set; }
    public FeelsLike? feelsLike { get; set; }
    public Pressure? pressure { get; set; }
    public string? tendency { get; set; }
    public Visibility? visibility { get; set; }
    public string? humidity { get; set; }
    public WindSpeed? windSpeed { get; set; }
    public WindGust? windGust { get; set; }
    public string? windDirection { get; set; }
    public string? windBearing { get; set; }
}

public class PastHourly
{
    public string? observedAt { get; set; }
    public string? provinceCode { get; set; }
    public List<Hour>? hours { get; set; }
}

public class PeakWind
{
    public int? imperial { get; set; }
    public int? metric { get; set; }
    public int? nautical { get; set; }
    public string? directionCardinal { get; set; }
}

public class Precip
{
    public double imperial { get; set; }
    public double metric { get; set; }
}

public class Pressure
{
    public string? imperial { get; set; }
    public string? metric { get; set; }
    public string? changeImperial { get; set; }
    public string? changeMetric { get; set; }
}

public class Rainfall
{
    public object? imperial { get; set; }
    public object? metric { get; set; }
}

public class Record
{
    public Station? station { get; set; }
    public RecordValue? recordValue { get; set; }
    public bool? defaultDisplay { get; set; }
}

public class RecordValue
{
    public object? pop { get; set; }
    public string? date { get; set; }
    public AvgMaxTemp? avgMaxTemp { get; set; }
    public AvgMinTemp? avgMinTemp { get; set; }
    public AvgMeanTemp? avgMeanTemp { get; set; }
    public ExtremeMaxTemp? extremeMaxTemp { get; set; }
    public ExtremeMinTemp? extremeMinTemp { get; set; }
    public ExtremePrecip? extremePrecip { get; set; }
    public ExtremeRainfall? extremeRainfall { get; set; }
    public ExtremeSnowfall? extremeSnowfall { get; set; }
    public ExtremeSnowOnGround? extremeSnowOnGround { get; set; }
}

public class RegionalNormals
{
    public Metric? metric { get; set; }
    public Imperial? imperial { get; set; }
}

public class Rise
{
    public string? time12h { get; set; }
    public string? epochTimeRounded { get; set; }
    public string? time { get; set; }
}

public class RiseSet
{
    public Set? set { get; set; }
    public string? timeZone { get; set; }
    public Rise? rise { get; set; }
}

public class RiseSetNextDay
{
    public Set? set { get; set; }
    public string? timeZone { get; set; }
    public Rise? rise { get; set; }
}

public class GouvCAWeatherStationResponse
{
    public string? cgndb { get; set; }
    public string? displayName { get; set; }
    public string? zonePoly { get; set; }
    public int lastUpdated { get; set; }
    public Alert? alert { get; set; }
    public Observation? observation { get; set; }
    public HourlyFcst? hourlyFcst { get; set; }
    public DailyFcst? dailyFcst { get; set; }
    public Aqhi? aqhi { get; set; }
    public RiseSet? riseSet { get; set; }
    public RiseSetNextDay? riseSetNextDay { get; set; }
    public List<object>? metNotes { get; set; }
    public PastHourly? pastHourly { get; set; }
    public string? cityCode { get; set; }
    public string? province { get; set; }
    public object? lat { get; set; }
    public object? lon { get; set; }
    public string? tcId { get; set; }
    public string? climateId { get; set; }
    public string? tc2Id { get; set; }
    public string? climate2Id { get; set; }
    public string? wxoZoneCode { get; set; }
    public string? timezone { get; set; }
    public string? climatIDDaily { get; set; }
    public string? climatIDHrly { get; set; }
    public string? radarView { get; set; }
    public YesterdayObs? yesterdayObs { get; set; }
    public List<Record>? records { get; set; }
    public int alert_nodata_epoch { get; set; }
    public Mtimes? mtimes { get; set; }
}

public class Set
{
    public string? time12h { get; set; }
    public string? epochTimeRounded { get; set; }
    public string? time { get; set; }
}

public class Snowfall
{
    public object? imperial { get; set; }
    public object? metric { get; set; }
}

public class Station
{
    public string? observedAt { get; set; }
    public string? climateId { get; set; }
    public string? tcId { get; set; }
}

public class Temperature
{
    public string? imperial { get; set; }
    public string? imperialUnrounded { get; set; }
    public string? metric { get; set; }
    public string? metricUnrounded { get; set; }
    public int? periodHigh { get; set; }
    public int? periodLow { get; set; }
}

public class Visibility
{
    public string? imperial { get; set; }
    public string? metric { get; set; }
}

public class WindChill
{
    public List<object>? calculated { get; set; }
    public string? textSummary { get; set; }
}

public class WindGust
{
    public string? imperial { get; set; }
    public string? metric { get; set; }
}

public class WindSpeed
{
    public string? imperial { get; set; }
    public string? metric { get; set; }
}

public class YesterdayObs
{
    public Station? station { get; set; }
    public Datestamp? datestamp { get; set; }
    public HighTemperature? highTemperature { get; set; }
    public LowTemperature? lowTemperature { get; set; }
    public Precip? precip { get; set; }
    public Rainfall? rainfall { get; set; }
    public Snowfall? snowfall { get; set; }
    public PeakWind? peakWind { get; set; }
}

#pragma warning enable