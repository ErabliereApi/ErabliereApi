namespace ErabliereApi.Donnees.Action.Get;

/// <summary>
/// Modèle reçu lors de l'obtention des alertes
/// </summary>
public class GetAlerte : Alerte
{
    /// <summary>
    /// Constructeur par défaut
    /// </summary>
    public GetAlerte()
    {
        
    }

    /// <summary>
    /// Crée une instance de GetAlerte à partir d'une Alerte
    /// </summary>
    /// <param name="alerte"></param>
    public GetAlerte(Alerte alerte)
    {
        Emails = alerte.EnvoyerA?.Split(';', System.StringSplitOptions.RemoveEmptyEntries) ?? System.Array.Empty<string>();
        Numeros = alerte.TexterA?.Split(';', System.StringSplitOptions.RemoveEmptyEntries) ?? System.Array.Empty<string>();

        var props = typeof(Alerte).GetProperties();
        foreach (var prop in props)
        {
            if (prop.CanWrite)
            {
                prop.SetValue(this, prop.GetValue(alerte));
            }
        }
    }

    /// <summary>
    /// Adresse couriels dans un tableau de chaîne de caractère
    /// </summary>
    public string[] Emails { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Numéros de téléphone dans un tableau de chaîne de caractère
    /// </summary>
    public string[] Numeros { get; set; } = System.Array.Empty<string>();
}
