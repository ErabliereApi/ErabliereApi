using ErabliereApi.Donnees.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Modèle représentant l'abonnement d'un utilisateur à un forfait de l'application.
/// L'abonnement est lié au <see cref="Customer" /> et peut être synchronisé avec un
/// abonnement Stripe via <see cref="StripeSubscriptionId" />.
/// </summary>
public class Abonnement : IIdentifiable<Guid?, Abonnement>, IDatesInfo
{
    /// <summary>
    /// La clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id du <see cref="Customer"/> possédant l'abonnement
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Le <see cref="Customer"/> possédant l'abonnement
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Le forfait de l'abonnement. Voir <see cref="ForfaitsAbonnement" /> pour les valeurs permises.
    /// </summary>
    [MaxLength(50)]
    public string Plan { get; set; } = ForfaitsAbonnement.Gratuit;

    /// <summary>
    /// La date de début de l'abonnement
    /// </summary>
    public DateTimeOffset? DateDebut { get; set; }

    /// <summary>
    /// La date de fin de l'abonnement, ou null si l'abonnement n'a pas d'échéance
    /// </summary>
    public DateTimeOffset? DateFin { get; set; }

    /// <summary>
    /// La fréquence de facturation d'un forfait payant. Voir <see cref="FrequencesFacturation" />
    /// pour les valeurs permises. Null pour un forfait gratuit.
    /// </summary>
    [MaxLength(50)]
    public string? FrequenceFacturation { get; set; }

    /// <summary>
    /// Le statut de l'abonnement
    /// </summary>
    public StatutAbonnement Statut { get; set; } = StatutAbonnement.EnAttente;

    /// <summary>
    /// L'id de l'abonnement Stripe relié (sub_...), ou null pour un abonnement sans paiement
    /// </summary>
    [MaxLength(100)]
    public string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Date de création
    /// </summary>
    public DateTimeOffset? DC { get; set; }

    /// <summary>
    /// Date de modification
    /// </summary>
    public DateTimeOffset? DM { get; set; }

    /// <summary>
    /// Les transitions de statut permises à partir de chaque statut.
    /// </summary>
    private static readonly Dictionary<StatutAbonnement, StatutAbonnement[]> TransitionsPermises = new()
    {
        { StatutAbonnement.EnAttente, [StatutAbonnement.Actif, StatutAbonnement.Annule] },
        { StatutAbonnement.Actif, [StatutAbonnement.Annule, StatutAbonnement.Expire] },
        { StatutAbonnement.Annule, [] },
        { StatutAbonnement.Expire, [] },
    };

    /// <summary>
    /// Valide que la plage de dates est cohérente. Des dates null sont permises,
    /// mais si les deux dates sont présentes, la date de début doit précéder la date de fin.
    /// </summary>
    public static bool DatesValides(DateTimeOffset? dateDebut, DateTimeOffset? dateFin)
    {
        return dateDebut == null || dateFin == null || dateDebut < dateFin;
    }

    /// <summary>
    /// Indique si l'abonnement est actif au moment reçu en paramètre :
    /// le statut doit être <see cref="StatutAbonnement.Actif" /> et le moment
    /// doit être dans la plage [DateDebut, DateFin).
    /// </summary>
    public bool EstActif(DateTimeOffset maintenant)
    {
        return Statut == StatutAbonnement.Actif &&
               (DateDebut == null || DateDebut <= maintenant) &&
               (DateFin == null || maintenant < DateFin);
    }

    /// <summary>
    /// Indique si l'abonnement peut passer de son statut courant au statut cible.
    /// </summary>
    public bool PeutTransitionnerVers(StatutAbonnement cible)
    {
        return TransitionsPermises.TryGetValue(Statut, out var cibles) &&
               Array.Exists(cibles, c => c == cible);
    }

    /// <summary>
    /// Change le statut de l'abonnement en validant la transition.
    /// </summary>
    /// <exception cref="InvalidOperationException">La transition n'est pas permise</exception>
    public void ChangerStatut(StatutAbonnement cible)
    {
        if (!PeutTransitionnerVers(cible))
        {
            throw new InvalidOperationException(
                $"La transition de statut {Statut} vers {cible} n'est pas permise.");
        }

        Statut = cible;
        DM = DateTimeOffset.Now;
    }

    /// <inheritdoc />
    public int CompareTo(Abonnement? other)
    {
        return 0;
    }
}
