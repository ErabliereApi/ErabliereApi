namespace ErabliereApi.Services.LoRaWAN;

/// <summary>
/// Decodeur de paquet LoRaWAN
/// </summary>
public static class LoRaWANPacketDecoder
{
    /// <summary>
    /// Decode LoRaWAN packet
    /// </summary>
    /// <param name="data"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static MesurementResponse TryDecodeData(string data, ILogger? logger = null)
    {
        // Décoder les données
        var bytes = Convert.FromBase64String(data);

        var (decodedData, crc) = DecodeData(bytes, logger);

        return new MesurementResponse
        {
            Mesurements = decodedData,
            Crc = crc
        };
    }

    /// <summary>
    /// Modèle de réponse de la fonction de décodage
    /// </summary>
    public class MesurementResponse
    {
        /// <summary>
        /// Liste des mesures
        /// </summary>
        public Mesurement[]? Mesurements { get; set; }

        /// <summary>
        /// CRC
        /// </summary>
        public int Crc { get; set; }
    }

    /// <summary>
    /// Format du décodage d'une mesure
    /// </summary>
    public class Mesurement
    {
        /// <summary>
        /// Canal
        /// </summary>
        public int Channel { get; set; }

        /// <summary>
        /// L'ID de la mesure, voir les code de Senscap pour référence
        /// </summary>
        public int Mesure { get; set; }

        /// <summary>
        /// La valeur
        /// </summary>
        public decimal? Value { get; set; }

        /// <summary>
        /// Message d'erreur s'il y a lieu
        /// </summary>
        public string? errorMessage { get; set; }
    }

    private static (Mesurement[], int) DecodeData(byte[] b, ILogger? logger)
    {
        int i = 0;
        int length = b.Length;
        var values = new List<Mesurement>();
        var crc = 0;

        try
        {
            if (isWeatherStation(b))
            {
                decimal airTemp = ((b[1] << 8) + b[2]) / 10m;
                int airHumidity = b[3];
                uint lightIntensity = (uint)((b[4] << 24) + (b[5] << 16) + (b[6] << 8) + b[7]);
                decimal uvIndex = b[8] / 10m;
                decimal windSpeed = ((ushort)(b[9] << 8) + b[10]) / 10m;
                if (b[11] != 2)
                {
                    logger?.LogError("Byte at 11 should be 2");
                }
                int windDirection = (b[12] << 8) + b[13];
                decimal rainfallIntensity = ((b[14] << 24) + (b[15] << 16) + (b[16] << 8) + b[17]) / 1000m;
                decimal barometricPressur = ((b[18] << 8) + b[19]) * 10m;

                values.Add(new Mesurement
                {
                    Mesure = 4097,
                    Value = airTemp
                });
                values.Add(new Mesurement
                {
                    Mesure = 4098,
                    Value = airHumidity
                });
                values.Add(new Mesurement
                {
                    Mesure = 4099,
                    Value = lightIntensity
                });
                values.Add(new Mesurement
                {
                    Mesure = 4190,
                    Value = uvIndex
                });
                values.Add(new Mesurement
                {
                    Mesure = 4105,
                    Value = windSpeed
                });
                values.Add(new Mesurement
                {
                    Mesure = 4104,
                    Value = windDirection
                });
                values.Add(new Mesurement
                {
                    Mesure = 4113,
                    Value = rainfallIntensity
                });
                values.Add(new Mesurement
                {
                    Mesure = 4101,
                    Value = barometricPressur
                });
            }
            else
            {
                while (i < (length - 2))
                {
                    var channel = b[i++];
                    var mesurment = GetMesurement(b[i++], b[i++]);
                    decimal? value = (b[i++] + (b[i++] << 8) + (b[i++] << 16) + (b[i++] << 24));
                    string? message = null;

                    switch (mesurment)
                    {
                        case 4097: // Air Temperature
                        case 4098: // Air Humidity
                        case 4099: // Light Intensity
                        case 4100: // CO2
                        case 4101: // Barometric Pressure
                        case 4102: // Soil Temperature
                        case 4103: // Soil Moisture
                            value = value / 1000.0m;
                            break;
                        default:
                            message = $"Mesurement {mesurment} it unknow in {Convert.ToBase64String(b)}";
                            value = null;
                            logger?.LogWarning(message);
                            break;
                    }

                    values.Add(new Mesurement
                    {
                        Channel = channel,
                        Mesure = mesurment,
                        Value = value,
                        errorMessage = message
                    });
                }

                crc = b[i++] + (b[i] << 8);
            }
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Erreur lors du parsing des mesures du paquet LoRaWAN {Data}", Convert.ToBase64String(b));
        }

        return (values.ToArray(), crc);
    }

    private static bool isWeatherStation(byte[] b)
    {
        var validLength = b.Length == 20;
        var b1 = b[0] == 1;
        var b2 = b[11] == 2;

        return validLength && b1 && b2;
    }

    private static int GetMesurement(byte v1, byte v2)
    {
        int m = v1;
        m = m + (v2 << 8);

        return m;
    }
}
