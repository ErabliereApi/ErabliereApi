using ErabliereApi.Services.Weather.Helper;

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
        public string? ErrorMessage { get; set; }
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
                if (b[0] == 4) // Battery info packet
                {
                    var bib = b[0..10];
                    b = b[10..];

                    values.Add(new Mesurement
                    {
                        Channel = bib[0],
                        Mesure = 7,
                        Value = bib[1]
                    });

                    values.Add(new Mesurement
                    {
                        Channel = bib[0],
                        Mesure = 9, // Hardware version
                        Value = (bib[2] << 24) + (bib[3] << 16) + (bib[4] << 8) + bib[5],
                    });

                    values.Add(new Mesurement
                    {
                        Channel = bib[0],
                        Mesure = 8, // interval uplink
                        Value = (bib[6] << 8) + bib[7]
                    });

                    values.Add(new Mesurement
                    {
                        Channel = bib[0],
                        Mesure = 10, // GPS Ulink interval
                        Value = (bib[8] << 8) + bib[9]
                    });
                }

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

                decimal? dewPoint = null;
                string? dewPointError = null;
                try
                {
                    dewPoint = (decimal)DewPointCalculator.CalculateDewPoint
                    (
                        (double)airTemp,
                        airHumidity
                    );
                } catch (Exception e)
                {
                    dewPointError = e.Message;
                }

                values.Add(new Mesurement
                {
                    Mesure = 4202,
                    Value = dewPoint,
                    ErrorMessage = dewPointError
                });

                if (b.Length == 22)
                {
                    values.Add(new Mesurement
                    {
                        Channel = b[20],
                        Mesure = 7,
                        Value = b[21]
                    });
                }
            }
            else
            {
                while (i < (length - 2))
                {
                    var channel = b[i++];
                    var mesurment = GetMesurement(b[i++], b[i++]);
                    decimal? value = null;
                    string? message = null;

                    switch (mesurment)
                    {
                        case 07: // Batterie
                            var batteryLevel = b[i++] + (b[i++] << 8);
                            var frequency = b[i++] + (b[i++] << 8);
                            values.Add(new Mesurement
                            {
                                Channel = channel,
                                Mesure = 07,
                                Value = batteryLevel
                            });
                            values.Add(new Mesurement
                            {
                                Channel = channel,
                                Mesure = 08,
                                Value = frequency
                            });
                            break;
                        case 4097: // Air Temperature
                        case 4098: // Air Humidity
                        case 4099: // Light Intensity
                        case 4100: // CO2
                        case 4101: // Barometric Pressure
                        case 4102: // Soil Temperature
                        case 4103: // Soil Moisture
                            value = (b[i++] + (b[i++] << 8) + (b[i++] << 16) + (b[i++] << 24));
                            value = value / 1000.0m;
                            values.Add(new Mesurement
                            {
                                Channel = channel,
                                Mesure = mesurment,
                                Value = value,
                                ErrorMessage = message
                            });
                            break;
                        default:
                            message = $"Mesurement {mesurment} it unknow in {Convert.ToBase64String(b)}";
                            value = (b[i++] + (b[i++] << 8) + (b[i++] << 16) + (b[i++] << 24));
                            logger?.LogWarning(message);
                            values.Add(new Mesurement
                            {
                                Channel = channel,
                                Mesure = mesurment,
                                Value = value,
                                ErrorMessage = message
                            });
                            break;
                    }
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
        var validLength = b.Length == 20 || b.Length == 22 || b.Length == 30;

        if (b.Length == 20)
        {
            var b1 = b[0] == 1;
            var b2 = b[11] == 2;

            return b1 && b2;
        }
        else if (b.Length == 22)
        {
            var b1 = b[0] == 1;
            var b2 = b[11] == 2;
            var b3 = b[20] == 3;

            return b1 && b2 && b3;
        }
        else if (b.Length == 30)
        {
            var b1 = b[0] == 4;
            var b2 = b[10] == 1;
            var b3 = b[21] == 2;

            return b1 && b2 && b3;
        }

        return false;
    }

    private static int GetMesurement(byte v1, byte v2)
    {
        int m = v1;
        m = m + (v2 << 8);

        return m;
    }
}
