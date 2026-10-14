using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Services.Notifications;

/// <summary>
/// Surveille les capteurs muets : un kit qui détecte une panne de courant ne peut plus rien
/// envoyer si le routeur tombe avec le courant, le silence est alors le seul signal.
/// Le silence se mesure sur l'heure de réception, jamais sur la date des données : un appareil
/// qui renvoie en différé des données datées du passé n'est plus muet.
/// </summary>
public class SilenceCapteurService
{
    private readonly ErabliereDbContext _depot;
    private readonly FileNotificationsAlerte _file;
    private readonly TimeProvider _temps;
    private readonly ILogger<SilenceCapteurService> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public SilenceCapteurService(
        ErabliereDbContext depot,
        FileNotificationsAlerte file,
        TimeProvider temps,
        ILogger<SilenceCapteurService> logger)
    {
        _depot = depot;
        _file = file;
        _temps = temps;
        _logger = logger;
    }

    /// <summary>
    /// Enregistre l'heure de réception des données de capteurs qui viennent d'être enregistrées,
    /// et termine le silence de leurs alertes s'il y en a un. N'échoue jamais : les données sont
    /// déjà enregistrées.
    /// </summary>
    /// <param name="idCapteurs">Les capteurs dont une donnée vient d'être reçue</param>
    /// <param name="token">Jeton d'annulation</param>
    public async Task SignalerReceptionAsync(IEnumerable<Guid> idCapteurs, CancellationToken token)
    {
        var ids = idCapteurs.Distinct().Select(id => (Guid?)id).ToArray();

        if (ids.Length == 0)
        {
            return;
        }

        try
        {
            var maintenant = _temps.GetUtcNow();

            await EnregistrerReceptionAsync(ids, maintenant, token);

            await TerminerSilencesAsync(ids, maintenant, token);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "La réception des données des capteurs {IdCapteurs} n'a pas pu être enregistrée", string.Join(", ", ids));
        }
    }

    /// <summary>
    /// Repère les alertes dont le capteur n'a rien envoyé depuis plus longtemps que leur délai de
    /// silence, et dépose un message par alerte, une seule fois par silence. Appelé périodiquement
    /// par <see cref="SurveillanceSilenceCapteursService"/>, par chaque instance de l'API :
    /// le jeton de concurrence <see cref="AlerteCapteur.EnSilenceDepuis"/> garantit qu'une seule
    /// instance envoie le message.
    /// </summary>
    /// <param name="token">Jeton d'annulation</param>
    /// <returns>Le nombre de silences commencés par cet appel</returns>
    public async Task<int> DetecterSilencesAsync(CancellationToken token)
    {
        var maintenant = _temps.GetUtcNow();

        var alertes = await _depot.AlerteCapteurs
            .Include(a => a.Capteur)
            .ThenInclude(c => c!.Erabliere)
            .Where(a => a.IsEnable && a.DelaiSilenceMinutes != null && a.EnSilenceDepuis == null)
            .ToArrayAsync(token);

        var commences = 0;

        foreach (var alerte in alertes)
        {
            var depuis = DebutSilence(alerte);

            if (depuis == null || maintenant - depuis.Value < TimeSpan.FromMinutes(alerte.DelaiSilenceMinutes!.Value))
            {
                continue;
            }

            alerte.EnSilenceDepuis = depuis;

            try
            {
                await _depot.SaveChangesAsync(token);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Une autre instance a commencé ce silence et a déposé le message, ou l'alerte a changé.
                _logger.LogInformation("Alerte {AlerteId} : silence déjà traité par une autre instance", alerte.Id);

                await _depot.Entry(alerte).ReloadAsync(token);

                continue;
            }

            commences++;

            await EnregistrerEpisodeAsync(alerte, () =>
            {
                _depot.EpisodesAlerteCapteur.Add(NouvelEpisode(alerte, depuis.Value));

                return Task.CompletedTask;
            }, token);

            _logger.LogInformation("Alerte {AlerteId} {AlerteNom} : capteur muet depuis {Depuis}", alerte.Id, alerte.Nom, depuis);

            _file.Ajouter(AlerteCapteurMessage.CreerSilence(alerte, depuis.Value, alerte.Capteur?.DateDerniereReception != null));
        }

        return commences;
    }

    /// <summary>
    /// La dernière réception du capteur. Un capteur qui n'a rien envoyé depuis la mise en place de
    /// cette surveillance compte à partir de la dernière modification de l'alerte, pour qu'un
    /// appareil déjà muet finisse par être signalé.
    /// </summary>
    private static DateTimeOffset? DebutSilence(AlerteCapteur alerte)
    {
        if (alerte.Capteur?.DateDerniereReception is DateTimeOffset reception)
        {
            return reception;
        }

        if (alerte.DM.HasValue)
        {
            return alerte.DM;
        }

        return alerte.DC.HasValue ? new DateTimeOffset(alerte.DC.Value) : null;
    }

    private async Task EnregistrerReceptionAsync(Guid?[] ids, DateTimeOffset maintenant, CancellationToken token)
    {
        var capteurs = await _depot.Capteurs
            .Where(c => ids.Contains(c.Id))
            .ToArrayAsync(token);

        foreach (var capteur in capteurs)
        {
            // Deux requêtes simultanées : l'heure ne recule pas.
            if (capteur.DateDerniereReception == null || capteur.DateDerniereReception < maintenant)
            {
                capteur.DateDerniereReception = maintenant;
            }
        }

        await _depot.SaveChangesAsync(token);
    }

    private async Task TerminerSilencesAsync(Guid?[] ids, DateTimeOffset maintenant, CancellationToken token)
    {
        var alertes = await _depot.AlerteCapteurs
            .Include(a => a.Capteur)
            .ThenInclude(c => c!.Erabliere)
            .Where(a => ids.Contains(a.IdCapteur) && a.EnSilenceDepuis != null)
            .ToArrayAsync(token);

        foreach (var alerte in alertes)
        {
            var debut = alerte.EnSilenceDepuis!.Value;

            alerte.EnSilenceDepuis = null;

            try
            {
                await _depot.SaveChangesAsync(token);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Une autre requête a terminé ce silence et a déposé le message.
                _logger.LogInformation("Alerte {AlerteId} : fin du silence déjà appliquée par une autre requête", alerte.Id);

                await _depot.Entry(alerte).ReloadAsync(token);

                continue;
            }

            await EnregistrerEpisodeAsync(alerte, () => FermerEpisodesAsync(alerte, debut, maintenant, token), token);

            _logger.LogInformation("Alerte {AlerteId} {AlerteNom} : données reprises à {Reprise} après un silence depuis {Debut}", alerte.Id, alerte.Nom, maintenant, debut);

            _file.Ajouter(AlerteCapteurMessage.CreerReprise(alerte, debut, maintenant));
        }
    }

    private async Task EnregistrerEpisodeAsync(AlerteCapteur alerte, Func<Task> modifier, CancellationToken token)
    {
        try
        {
            await modifier();

            await _depot.SaveChangesAsync(token);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // L'état du silence est enregistré et le message doit partir quand même.
            _logger.LogError(e, "Alerte {AlerteId} : l'épisode de silence n'a pas pu être enregistré", alerte.Id);

            foreach (var entry in _depot.ChangeTracker.Entries<EpisodeAlerteCapteur>().ToArray())
            {
                entry.State = EntityState.Detached;
            }
        }
    }

    private async Task FermerEpisodesAsync(AlerteCapteur alerte, DateTimeOffset debut, DateTimeOffset fin, CancellationToken token)
    {
        var episodes = await _depot.EpisodesAlerteCapteur
            .Where(e => e.IdAlerteCapteur == alerte.Id && e.Type == TypeEpisodeAlerte.Silence && e.DateFin == null)
            .ToArrayAsync(token);

        if (episodes.Length == 0)
        {
            // Silence en cours sans épisode ouvert : on garde quand même la trace.
            _logger.LogWarning("Alerte {AlerteId} : fin de silence sans épisode ouvert, l'épisode est recréé", alerte.Id);

            var episode = NouvelEpisode(alerte, debut);
            _depot.EpisodesAlerteCapteur.Add(episode);
            episodes = [episode];
        }

        foreach (var episode in episodes)
        {
            episode.DateFin = fin;
            episode.RaisonFin = RaisonFinEpisode.DonneesReprises;
        }
    }

    private static EpisodeAlerteCapteur NouvelEpisode(AlerteCapteur alerte, DateTimeOffset debut)
    {
        return new EpisodeAlerteCapteur
        {
            Id = Guid.NewGuid(),
            IdAlerteCapteur = alerte.Id,
            IdCapteur = alerte.IdCapteur,
            NomAlerte = alerte.Nom,
            Type = TypeEpisodeAlerte.Silence,
            DateDebut = debut
        };
    }
}
