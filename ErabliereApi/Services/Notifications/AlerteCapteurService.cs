using ErabliereApi.Depot.Sql;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Services.Notifications;

/// <summary>
/// Tient à jour l'état des alertes d'un capteur et dépose un message seulement quand l'état change.
/// Utilisé par tous les points d'entrée des données de capteur.
/// </summary>
public class AlerteCapteurService
{
    private readonly ErabliereDbContext _depot;
    private readonly FileNotificationsAlerte _file;
    private readonly ILogger<AlerteCapteurService> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public AlerteCapteurService(
        ErabliereDbContext depot,
        FileNotificationsAlerte file,
        ILogger<AlerteCapteurService> logger)
    {
        _depot = depot;
        _file = file;
        _logger = logger;
    }

    /// <summary>
    /// Évalue une donnée contre les alertes activées de son capteur. Enregistre le changement
    /// d'état s'il y en a un et dépose le message de début ou de fin dans la file d'envoi.
    /// </summary>
    /// <param name="idCapteur">L'id du capteur de la donnée</param>
    /// <param name="valeur">La valeur de la donnée</param>
    /// <param name="date">La date de la donnée</param>
    /// <param name="token">Jeton d'annulation</param>
    public async Task EvaluerAsync(Guid idCapteur, decimal valeur, DateTimeOffset date, CancellationToken token)
    {
        var alertes = await _depot.AlerteCapteurs
            .Include(a => a.Capteur)
            .ThenInclude(c => c!.Erabliere)
            .Where(a => a.IdCapteur == idCapteur && a.IsEnable)
            .ToArrayAsync(token);

        foreach (var alerte in alertes)
        {
            var transition = AlerteCapteurTransition.Evaluer(alerte, valeur, date);

            if (transition == TypeTransitionAlerte.Aucune)
            {
                continue;
            }

            var debut = alerte.LastOccurence;

            alerte.EstEnAlerte = transition == TypeTransitionAlerte.Debut;
            alerte.DateChangementEtat = date;

            if (transition == TypeTransitionAlerte.Debut)
            {
                alerte.LastOccurence = date;
            }

            try
            {
                await _depot.SaveChangesAsync(token);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Une autre requête a changé l'état entre-temps et a déposé son message.
                _logger.LogInformation("Alerte {AlerteId} : changement d'état déjà appliqué par une autre requête", alerte.Id);

                await _depot.Entry(alerte).ReloadAsync(token);

                continue;
            }

            _logger.LogInformation("Alerte {AlerteId} {AlerteNom} : {Transition} à {Date}", alerte.Id, alerte.Nom, transition, date);

            _file.Ajouter(AlerteCapteurMessage.Creer(alerte, transition, valeur, date, debut));
        }
    }
}
