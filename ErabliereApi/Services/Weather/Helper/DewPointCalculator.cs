namespace ErabliereApi.Services.Weather.Helper;

using System;

/// <summary>
/// Helper pour calculer de point de rosé (Dew point)
/// </summary>
public static class DewPointCalculator
{
    // Coefficients de Magnus-Tetens constants pour la précision
    private const double A = 17.625;
    private const double B = 243.04;

    /// <summary>
    /// Calcule le point de rosée en degrés Celsius.
    /// </summary>
    /// <param name="temperature">Température de l'air en °C</param>
    /// <param name="relativeHumidity">Humidité relative en % (Ex: 65 pour 65%)</param>
    /// <returns>Le point de rosée en °C</returns>
    public static double CalculateDewPoint(double temperature, double relativeHumidity)
    {
        // Validation des entrées
        if (relativeHumidity <= 0 || relativeHumidity > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeHumidity), "L'humidité doit être entre 0% et 100%.");
        }

        // Étape 1 : Calcul de la variable intermédiaire alpha
        double alpha = Math.Log(relativeHumidity / 100.0) + ((A * temperature) / (B + temperature));

        // Étape 2 : Application de la formule finale
        double dewPoint = (B * alpha) / (A - alpha);

        return Math.Round(dewPoint, 2);
    }

    /// <summary>
    /// Calcule l'humidité relative à partir de la température et du point de rosée.
    /// </summary>
    /// <param name="temperature">Température de l'air en °C</param>
    /// <param name="dewPoint">Point de rosée en °C</param>
    /// <returns>L'humidité relative en %</returns>
    public static double CalculateRelativeHumidity(double temperature, double dewPoint)
    {
        if (dewPoint > temperature)
        {
            throw new ArgumentException("Le point de rosée ne peut pas être supérieur à la température de l'air.");
        }

        double numerator = (A * dewPoint) / (B + dewPoint);
        double denominator = (A * temperature) / (B + temperature);

        double rh = 100.0 * Math.Exp(numerator - denominator);

        return Math.Round(rh, 2);
    }
}