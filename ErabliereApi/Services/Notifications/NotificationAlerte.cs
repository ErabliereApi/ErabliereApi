namespace ErabliereApi.Services.Notifications;

/// <summary>
/// Message de début ou de fin d'alerte en attente d'envoi.
/// </summary>
/// <param name="IdAlerte">L'id de l'alerte, pour la journalisation</param>
/// <param name="Sujet">Le sujet du courriel</param>
/// <param name="Message">Le corps du courriel et le texte du texto</param>
/// <param name="EnvoyerA">Adresses courriel séparées par des ';'</param>
/// <param name="TexterA">Numéros de téléphone séparés par des ';'</param>
public record NotificationAlerte(Guid? IdAlerte, string Sujet, string Message, string? EnvoyerA, string? TexterA);
