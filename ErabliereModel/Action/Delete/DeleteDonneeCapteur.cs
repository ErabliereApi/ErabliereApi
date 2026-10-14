using System;

namespace ErabliereApi.Donnees.Action.Delete;

/// <summary>
/// Modèle de suppression d'une donnée de capteur. Seuls les ids sont lus : la donnée est chargée par le serveur
/// et doit appartenir au capteur de la route.
/// </summary>
public class DeleteDonneeCapteur
{
    /// <summary>
    /// L'id de la donnée à supprimer
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id du capteur de la donnée, qui doit concorder avec celui de la route
    /// </summary>
    public Guid? IdCapteur { get; set; }
}
