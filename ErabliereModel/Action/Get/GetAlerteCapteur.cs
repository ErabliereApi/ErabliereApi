using System;

namespace ErabliereApi.Donnees.Action.Get;

/// <summary>
/// Modèle des alertes capteur utiliser pour l'obtention des alertes capteur.
/// </summary>
public class GetAlerteCapteur : AlerteCapteur
{
    /// <summary>
    /// Constructeur par défaut
    /// </summary>
    public GetAlerteCapteur()
    {
        
    }

    /// <summary>
    /// Créer une instance de GetAlerteCapteur à partir d'une instance d'AlerteCapteur
    /// </summary>
    /// <param name="other"></param>
    public GetAlerteCapteur(AlerteCapteur other)
    {
        Emails = other.EnvoyerA != null ?
                 other.EnvoyerA.Split(';', StringSplitOptions.RemoveEmptyEntries) :
                 Array.Empty<string>();

        Numeros = other.TexterA != null ?
                  other.TexterA.Split(';', StringSplitOptions.RemoveEmptyEntries) :
                  Array.Empty<string>();

        var properties = typeof(AlerteCapteur).GetProperties();
        foreach (var property in properties)
        {
            if (property.CanRead && property.CanWrite)
            {
                var value = property.GetValue(other);
                property.SetValue(this, value);
            }
        }
    }

    /// <summary>
    /// La listes des courriels dans une liste
    /// </summary>
    public string[] Emails { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Numéros de téléphone dans un tableau de chaîne de caractère
    /// </summary>
    public string[] Numeros { get; set; } = System.Array.Empty<string>();
}
