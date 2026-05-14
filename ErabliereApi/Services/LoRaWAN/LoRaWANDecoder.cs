using System.Reflection;

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
    public static (Mesurement[], int) TryDecodeData(string data, ILogger? logger = null)
    {
        // Décoder les données
        var bytes = Convert.FromBase64String(data);

        var (decodedData, crc) = DecodeData(bytes, logger);

        return (decodedData, crc);
    }

    public class Mesurement
    {
        public int Channel { get; set; }
        public int Mesure { get; set; }
        public decimal? Value { get; set; }
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
            while (i < (length - 2))
            {
                var channel = b[i++];
                var mesurment = GetMesurement(b[i++], b[i++]);
                decimal? value = (decimal)(b[i++] + (b[i++] << 8) + (b[i++] << 16) + (b[i++] << 24));
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
                        message = $"Mesurement {mesurment} it unknow";
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
        catch (Exception e)
        {
            logger?.LogError(e, "Erreur lors du parsing des mesures du paquet LoRaWAN");
        }

        return (values.ToArray(), crc);
    }

    private static int GetMesurement(byte v1, byte v2)
    {
        int m = v1;
        m = m + (v2 << 8);

        return m;
    }
}
