using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace ErabliereApi.Services.Notifications;

/// <summary>
/// File d'attente en mémoire des messages d'alerte. La requête HTTP y dépose le message et
/// <see cref="EnvoiNotificationsAlerteService"/> l'envoie, pour que la réponse n'attende pas
/// le courriel et le texto.
/// </summary>
public class FileNotificationsAlerte
{
    private readonly Channel<NotificationAlerte> _canal = Channel.CreateUnbounded<NotificationAlerte>(
        new UnboundedChannelOptions { SingleReader = true });

    private int _enAttente;

    /// <summary>
    /// Nombre de messages déposés dont l'envoi n'est pas terminé.
    /// </summary>
    public int EnAttente => Volatile.Read(ref _enAttente);

    /// <summary>
    /// Dépose un message à envoyer.
    /// </summary>
    public void Ajouter(NotificationAlerte notification)
    {
        Interlocked.Increment(ref _enAttente);

        _canal.Writer.TryWrite(notification);
    }

    /// <summary>
    /// Retire le prochain message s'il y en a un, sans attendre.
    /// </summary>
    public bool TryLire([MaybeNullWhen(false)] out NotificationAlerte notification)
    {
        return _canal.Reader.TryRead(out notification);
    }

    /// <summary>
    /// Lit les messages au fur et à mesure qu'ils sont déposés.
    /// </summary>
    public IAsyncEnumerable<NotificationAlerte> LireAsync(CancellationToken token)
    {
        return _canal.Reader.ReadAllAsync(token);
    }

    /// <summary>
    /// Indique que l'envoi d'un message lu est terminé, réussi ou non.
    /// </summary>
    public void MarquerTraitee()
    {
        Interlocked.Decrement(ref _enAttente);
    }
}
