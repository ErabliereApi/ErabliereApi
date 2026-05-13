using System.Reflection;

namespace ErabliereApi.Services.LoRaWAN;

public static class LoRaWANPacketDecoder
{
    /// <summary>
    /// Decode LoRaWAN packet
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Mesurement[] DecodeData(string data)
    {
        // Décoder les données
        var bytes = Convert.FromBase64String(data);

        var decodedData = DecodeData(bytes);

        return decodedData;
    }

    public class Mesurement
    {
        public int Channel { get; set; }
        public int Mesure { get; set; }
        public decimal Value { get; set; }
    }

    private static Mesurement[] DecodeData(byte[] b)
    {
        int i = 0;
        int length = b.Length;
        var values = new List<Mesurement>();

        while (i < (length - 2))
        {
            var channel = b[i++];
            var mesurment = GetMesurement(b[i++], b[i++]);
            var value = (decimal)(b[i++] + (b[i++] << 8) + (b[i++] << 16) + (b[i++] << 24));

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
                    throw new InvalidOperationException($"Mesurement {mesurment} it unknow");
            }

            values.Add(new Mesurement
            {
                Channel = channel,
                Mesure = mesurment,
                Value = value
            });
        }

        var crc = b[i++] + (b[i++] << 8);

        return values.ToArray();
    }

    private static int GetMesurement(byte v1, byte v2)
    {
        int m = v1;
        m = m + (v2 << 8);

        return m;
    }
}
