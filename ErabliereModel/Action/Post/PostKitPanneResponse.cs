using System;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// La configuration d'un kit de détection de panne de courant, retournée une seule fois à sa
/// création. La clé d'API n'est conservée que sous forme hachée et ne peut plus être relue.
/// </summary>
public class PostKitPanneResponse
{
    /// <summary>
    /// L'id du kit créé
    /// </summary>
    public Guid IdKit { get; set; }

    /// <summary>
    /// Le nom du fichier de configuration du script, dans ~/.erabliereapi/ sur le kit
    /// </summary>
    public string NomFichierConfiguration { get; set; } = "";

    /// <summary>
    /// Le contenu du fichier de configuration du script monitorUpsV2.py
    /// </summary>
    public string FichierConfiguration { get; set; } = "";

    /// <summary>
    /// Le nom du fichier contenant la clé d'API, dans ~/.erabliereapi/ sur le kit
    /// </summary>
    public string NomFichierAuthentification { get; set; } = "";

    /// <summary>
    /// Le contenu du fichier contenant la clé d'API
    /// </summary>
    public string FichierAuthentification { get; set; } = "";
}
