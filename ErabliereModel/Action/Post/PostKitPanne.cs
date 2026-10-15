using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Modèle d'ajout d'un kit de détection de panne de courant à une érablière. L'API crée les
/// capteurs, les alertes et la clé d'API du kit. Ne contient volontairement aucune propriété
/// de navigation afin d'empêcher l'over-posting vers des entités liées.
/// </summary>
public class PostKitPanne
{
    /// <summary>
    /// Le nom du kit. Préfixe aussi le nom de ses capteurs et de ses alertes.
    /// </summary>
    [MaxLength(30)]
    public string? Nom { get; set; }

    /// <summary>
    /// Une liste d'adresses courriel séparées par des ';'. Par défaut, le courriel de l'utilisateur.
    /// </summary>
    [MaxLength(200)]
    public string? EnvoyerA { get; set; }

    /// <summary>
    /// Une liste de numéros de téléphone séparés par des ';'
    /// </summary>
    [MaxLength(200)]
    public string? TexterA { get; set; }

    /// <summary>
    /// Le délai, en minutes, après lequel un kit muet déclenche l'alerte de silence. Pendant
    /// une panne, le routeur tombe souvent avec le courant : le silence est alors le seul signal.
    /// </summary>
    [Range(1, 10080)]
    public int DelaiSilenceMinutes { get; set; } = 5;

    /// <summary>
    /// L'adresse de l'API que le kit doit appeler, http ou https. Par défaut, l'adresse de la
    /// requête, qui peut être fausse derrière un proxy inverse sans les en-têtes transférés.
    /// </summary>
    /// <example>https://erabliereapi.freddycoder.com</example>
    [MaxLength(200)]
    public string? Url { get; set; }
}
