namespace ErabliereApi.Services.AccuWeatherModels;

/// <summary>
/// Hourly weather forecast response
/// </summary>
public class HourlyWeatherForecastResponse
{
    /// <summary>
    /// Date and time of the forecast
    /// </summary>
    public DateTime DateTime { get; set; }
    /// <summary>
    /// Epoch date time
    /// </summary>
    public int EpochDateTime { get; set; }
    /// <summary>
    /// Weather icon
    /// </summary>
    public int WeatherIcon { get; set; }
    /// <summary>
    /// Icon phrase
    /// </summary>
    public string? IconPhrase { get; set; }
    /// <summary>
    /// Has precipitation
    /// </summary>
    public bool HasPrecipitation { get; set; }
    /// <summary>
    /// Type of precipitation
    /// </summary>
    public string? PrecipitationType { get; set; }
    /// <summary>
    /// Intensity of precipitation
    /// </summary>
    public string? PrecipitationIntensity { get; set; }
    /// <summary>
    /// Temperature informations
    /// </summary>
    public HourlyForecastTemperature? Temperature { get; set; }
    /// <summary>
    /// Link for mobile devices
    /// </summary>
    public string? MobileLink { get; set; }
    /// <summary>
    /// Link for web devices
    /// </summary>
    public string? Link { get; set; }
}

/// <summary>
/// Hourly forecast temperature
/// </summary>
public class HourlyForecastTemperature
{
    /// <summary>
    /// Value of the temperature
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Unit of the temperature
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Unit type of the temperature
    /// </summary>
    public int UnitType { get; set; }
}