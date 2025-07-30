using AutoMapper;
using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Action.Put;
using ErabliereApi.Extensions;
using ErabliereApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les données des notes
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly ErabliereDbContext _depot;
    private readonly IMapper _mapper;
    private readonly ILogger<NotesController> _logger;

    /// <summary>
    /// Constructeur par défaut
    /// </summary>
    /// <param name="depot"></param>
    /// <param name="mapper"></param>
    /// <param name="logger"></param>
    public NotesController(
        ErabliereDbContext depot,
        IMapper mapper,
        ILogger<NotesController> logger)
    {
        _depot = depot;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Lister les notes avec les fonctionnalité de OData
    /// </summary>
    /// <param name="id">Id de l'érablière</param>
    /// <returns></returns>
    [HttpGet]
    [EnableQuery]
    [ValiderOwnership("id")]
    public IQueryable<Note> Lister(Guid id)
    {
        return _depot.Notes.AsNoTracking().Where(n => n.IdErabliere == id);
    }

    /// <summary>
    /// Récupère la quantité de notes
    /// </summary>
    /// <returns></returns>
    [HttpGet("Quantite")]
    [ProducesResponseType(200, Type = typeof(int))]
    public async Task<IActionResult> Compter(Guid id, [FromQuery] string? search, CancellationToken token)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
#nullable disable
            return Ok(await _depot.Notes
                .CountAsync(n => n.IdErabliere == id && (n.Title.Contains(search) || n.Text.Contains(search)), token));
#nullable enable
        }

        return Ok(await _depot.Notes.CountAsync(n => n.IdErabliere == id, token));
    }

    /// <summary>
    /// Get toutes les notes avec un rappel actif et la périodicité est due
    /// </summary>
    /// <param name="id">Id de l'erabliere</param>
    /// <param name="token"></param>
    /// <returns>Retournes les notes avec un rappel actif et la périodicité est due</returns>
    [HttpGet("ActiveRappelsNotes")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<Note>))]
    [ValiderOwnership("id")]
    public async Task<IActionResult> GetNotesWithActiveRappels(Guid id, CancellationToken token)
    {
        var today = DateTimeOffset.Now;

        var notesWithRappel = await _depot.Notes
            .Include(n => n.Rappel)
            .Where(n => n.IdErabliere == id
                     && n.Rappel != null
                     && n.Rappel.IsActive
                     && n.Rappel.DateRappel <= today
                     && (n.Rappel.DateRappelFin == null || n.Rappel.DateRappelFin >= today))
            .Select(n => new Note
            {
                Id = n.Id,
                IdErabliere = n.IdErabliere,
                Title = n.Title,
                Text = n.Text,
                NoteDate = n.NoteDate,
                FileExtension = n.FileExtension,
                FileName = n.FileName,
                ExternalStorageType = n.ExternalStorageType,
                ExternalStorageUrl = n.ExternalStorageUrl,
                Rappel = new Rappel
                {
#nullable disable
                    Id = n.Rappel.Id,
                    NoteId = n.Rappel.NoteId,
#nullable enable
                    DateRappel = n.Rappel.DateRappel,
                    DateRappelFin = n.Rappel.DateRappelFin,
                    Periodicite = n.Rappel.Periodicite,
                    IsActive = n.Rappel.IsActive
                }
            })
#nullable disable
            .OrderByDescending(n => n.Rappel.DateRappel)
#nullable enable
            .ToListAsync(token);


        return Ok(notesWithRappel);
    }

    /// <summary>
    /// Obetenir l'image d'une note
    /// </summary>
    /// <param name="id">Id de l'érablière</param>
    /// <param name="noteId">Id de la note</param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <response code="200">Retourne l'image de la note</response>
    /// <response code="204">La note n'a pas d'image</response>
    /// <response code="404">La note n'existe pas</response>
    /// <response code="500">Erreur interne</response>
    /// <response code="400">L'érablière ne possède pas la note</response>
    /// <response code="401">Non autorisé</response>
    /// <response code="403">Interdit</response>
    [HttpGet("{noteId}/Image")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [ProducesResponseType(204)]
    [ValiderOwnership("id")]
    public async Task<IActionResult> ObtenirImage(Guid id, Guid noteId, CancellationToken token)
    {
        var note = await _depot.Notes.FindAsync([noteId], token);

        if (note == null)
        {
            return NotFound();
        }

        if (note.IdErabliere != id)
        {
            return BadRequest("L'érablière ne possède pas la note");
        }

        if (note.File == null)
        {
            return NoContent();
        }

        Response.Headers.Append("Cache-Control", "private, max-age=2592000");

        return File(note.File, $"image/{note.FileExtension ?? "jpg"}");
    }

    /// <summary>
    /// Action permettant d'ajouter une note
    /// </summary>
    /// <param name="id">Id de l'érablière</param>
    /// <param name="postNote"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(200, Type = typeof(Note))]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Ajouter(Guid id, PostNote postNote, CancellationToken token)
    {
        if (id != postNote.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'érablière possédant la note");
        }

        if (postNote.File != null && !postNote.IsValidBase64())
        {
            return BadRequest("Le fichier n'est pas en base64 valide");
        }

        if (postNote.Created == null)
        {
            postNote.Created = DateTimeOffset.Now;
        }

        if (postNote.NoteDate == null)
        {
            postNote.NoteDate = DateTimeOffset.Now;
        }

        var note = _mapper.Map<Note>(postNote);

        // Creer un rappel si le rappel est présent
        if (postNote.Rappel != null)
        {
            var allowedPeriodiciteValues = new[] { "annuel", "mensuel", "hebdo", "bihebdo", "quotidien", null };
            if (!allowedPeriodiciteValues.Contains(postNote.Rappel.Periodicite, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("rappel.periodicite", $"Periodicité invalide. Doit être : Annuel, Mensuel, Hebdo, Bihebdo, Quotidien. Était: {postNote.Rappel.Periodicite}");

                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            note.Rappel = new Rappel
            {
                IdErabliere = postNote.IdErabliere,
                IsActive = true,
                DateRappel = postNote.Rappel.DateRappel,
                DateRappelFin = postNote.Rappel.DateRappelFin,
                Periodicite = postNote.Rappel.Periodicite,
                NoteId = note.Id
            };
        }

        var entite = await _depot.Notes.AddAsync(note, token);

        await _depot.SaveChangesAsync(token);

        entite.Entity.File = null;

        return Ok(entite.Entity);
    }

    /// <summary>
    /// Action permettant de créer une note en utilisant Content-Type: multipart/form-data
    /// </summary>
    /// <param name="id">Id de l'érablière</param>
    /// <param name="token"></param>
    /// <param name="postNoteMultipart"></param>
    /// <returns></returns>
    [HttpPost("multipart")]
    [ProducesResponseType(200, Type = typeof(PostNoteMultipartResponse))]
    [ValiderOwnership("id")]
    public async Task<IActionResult> AjouterMultipart(Guid id, CancellationToken token,
        [FromForm] PostNoteMultipart postNoteMultipart)
    {
        if (postNoteMultipart.File == null)
        {
            return BadRequest("Le fichier est manquant");
        }

        var note = _mapper.Map<Note>(postNoteMultipart);

        note.File = await postNoteMultipart.File.ToByteArray(token);

        var entite = await _depot.Notes.AddAsync(note, token);

        await _depot.SaveChangesAsync(token);

        return Ok(_mapper.Map<PostNoteMultipartResponse>(entite.Entity));
    }

    /// <summary>
    /// Action permettant de modifier note
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="noteId">L'id de la note</param>
    /// <param name="putNote"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPut("{noteId}")]
    [ProducesResponseType(200, Type = typeof(Note))]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Modifier(Guid id, Guid noteId, PutNote putNote, CancellationToken token)
    {
        if (id != putNote.IdErabliere)
        {
            ModelState.AddModelError("id", "L'id de la route ne concorde pas avec l'érablière possédant la note");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        if (noteId != putNote.Id)
        {
            ModelState.AddModelError("id", "L'id de la note dans la route ne concorde pas avec l'id de la note dans le corps du message");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        if (putNote.Rappel != null)
        {
            var allowedPeriodiciteValues = new[] { "annuel", "mensuel", "hebdo", "bihebdo", "quotidien", null };
            if (!allowedPeriodiciteValues.Contains(putNote.Rappel.Periodicite))
            {
                ModelState.AddModelError("rappel.periodicite", $"Periodicité invalide. Valeurs acceptées: Annuel, Mensuel, Hebdo, Bihebdo, Quotidien. Était: {putNote.Rappel.Periodicite}");

                return BadRequest(new ValidationProblemDetails(ModelState));
            }
        }

        var entity = await _depot.Notes.FindAsync([noteId], token);

        if (entity == null || entity.IdErabliere != id)
        {
            return NotFound();
        }

        if (putNote.FileExtension != null)
        {
            entity.FileExtension = putNote.FileExtension;
        }

        if (putNote.NoteDate != null)
        {
            entity.NoteDate = putNote.NoteDate;
        }

        if (putNote.Text != null)
        {
            entity.Text = putNote.Text;
        }

        if (putNote.Title != null)
        {
            entity.Title = putNote.Title;
        }

        // Update rappel si le rappel est présent
        if (putNote.Rappel != null)
        {
            var rappel = await _depot.Rappels.FirstOrDefaultAsync(r => r.NoteId == entity.Id, token);
            if (rappel != null)
            {
                rappel.DateRappel = putNote.Rappel.DateRappel;
                rappel.DateRappelFin = putNote.Rappel.DateRappelFin;
                rappel.Periodicite = putNote.Rappel.Periodicite;
                rappel.IsActive = putNote.Rappel.IsActive;
            }
            else
            {
                entity.Rappel = new Rappel
                {
                    NoteId = entity.Id,
                    IdErabliere = id,
                    IsActive = putNote.Rappel.IsActive,
                    DateRappel = putNote.Rappel.DateRappel,
                    DateRappelFin = putNote.Rappel.DateRappelFin,
                    Periodicite = putNote.Rappel.Periodicite
                };
            }
        }

        await _depot.SaveChangesAsync(token);

        return Ok(entity);
    }

    /// <summary>
    /// Action permettant de mettre à jour l'image d'une note
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="noteId">L'id de la note</param>
    /// <param name="postNoteMultipart">Le modèle de la note avec le fichier</param>
    /// <param name="token">Le jeton d'annulation</param>
    [HttpPut("{noteId}/Image")]
    [ProducesResponseType(204)]
    [ValiderOwnership("id")]
    public async Task<IActionResult> ModifierImage(Guid id, Guid noteId, PutNoteMultipart postNoteMultipart, CancellationToken token)
    {
        if (postNoteMultipart.File == null)
        {
            return BadRequest("Le fichier est manquant");
        }

        var entity = await _depot.Notes.FindAsync([noteId], token);

        if (entity != null && entity.IdErabliere == id)
        {
            entity.File = await postNoteMultipart.File.ToByteArray(token);
            entity.FileExtension = postNoteMultipart.FileExtension;

            await _depot.SaveChangesAsync(token);

            return NoContent();
        }
        else
        {
            return NotFound();
        }
    }

    /// <summary>
    ///  Action permettant de mettre à jour les rappels des notes avec une périodicité due
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPut("PeriodiciteNotes")]
    [ProducesResponseType(200, Type = typeof(Note[]))]
    [ValiderOwnership("id")]
    public async Task<IActionResult> UpdateRappels(Guid id, CancellationToken token)
    {
        var today = DateTimeOffset.Now;

        var notesWithRappel = await _depot.Notes
            .Include(n => n.Rappel)
            .Where(n => n.IdErabliere == id && n.Rappel != null && n.Rappel.IsActive && n.Rappel.DateRappelFin < today && n.Rappel.Periodicite != null)
            .Select(n => new Note
            {
                Id = n.Id,
                Rappel = new Rappel
                {
#nullable disable
                    Id = n.Rappel.Id,
#nullable enable
                    DateRappel = n.Rappel.DateRappel,
                    DateRappelFin = n.Rappel.DateRappelFin,
                    Periodicite = n.Rappel.Periodicite
                }
            })
            .ToArrayAsync(token);

        if (notesWithRappel.Length == 0)
        {
            return Ok(notesWithRappel);
        }

        UpdateNoteWithRappel(notesWithRappel);

        await _depot.SaveChangesAsync(token);

        return Ok(notesWithRappel);
    }

    /// <summary>
    /// Action permettant de rappeler la prochaine période pour les notes avec un rappel actif
    /// </summary>
    [ProducesResponseType(200, Type = typeof(Rappel))]
    [HttpPut("{IdNote}/RappelProchainePeriode")]
    public async Task<IActionResult> RappelProchainePeriode(Guid id, Guid idNote, CancellationToken token)
    {
        var rappel = await _depot.Rappels
#nullable disable
            .Where(r => r.NoteId == idNote && r.IsActive && r.Note.IdErabliere == id)
#nullable enable
            .FirstOrDefaultAsync(token);

        if (rappel == null)
        {
            return NotFound("La note ou le rappel n'existe pas");
        }

        UpdateRappelDates(rappel);

        await _depot.SaveChangesAsync(token);

        return Ok(rappel);
    }

    private void UpdateNoteWithRappel(Note[] notesWithRappel)
    {
        foreach (var note in notesWithRappel)
        {
            if (!IsNoteRappelValid(note))
            {
                _logger.LogWarning("Note with id {NoteId} has a null rappel or rappel properties", note?.Id);
                continue;
            }

            if (note.Rappel == null)
            {
                _logger.LogWarning("Note with id {NoteId} has a null rappel", note.Id);
                continue;
            }

            UpdateRappelDates(note.Rappel);
        }
    }

    private static bool IsNoteRappelValid(Note? note)
    {
        return note != null &&
               note.Rappel != null &&
               note.Rappel.Periodicite != null &&
               note.Rappel.DateRappel != null;
    }

    private void UpdateRappelDates(Rappel rappel)
    {
        if (rappel.DateRappel == null)
        {
            _logger.LogWarning("Rappel with id {RappelId} has a null DateRappel", rappel.Id);
            return;
        }

        switch (rappel.Periodicite)
        {
            case "annuel":
                rappel.DateRappel = rappel.DateRappel.Value.AddYears(1);
                if (rappel.DateRappelFin != null)
                    rappel.DateRappelFin = rappel.DateRappelFin.Value.AddYears(1);
                break;
            case "mensuel":
                rappel.DateRappel = rappel.DateRappel.Value.AddMonths(1);
                if (rappel.DateRappelFin != null)
                    rappel.DateRappelFin = rappel.DateRappelFin.Value.AddMonths(1);
                break;
            case "bihebdo":
                rappel.DateRappel = rappel.DateRappel.Value.AddDays(14);
                if (rappel.DateRappelFin != null)
                    rappel.DateRappelFin = rappel.DateRappelFin.Value.AddDays(14);
                break;
            case "hebdo":
                rappel.DateRappel = rappel.DateRappel.Value.AddDays(7);
                if (rappel.DateRappelFin != null)
                    rappel.DateRappelFin = rappel.DateRappelFin.Value.AddDays(7);
                break;
            case "quotidien":
                rappel.DateRappel = rappel.DateRappel.Value.AddDays(1);
                if (rappel.DateRappelFin != null)
                    rappel.DateRappelFin = rappel.DateRappelFin.Value.AddDays(1);
                break;
        }
    }


    /// <summary>
    /// Action permettant de supprimer une note
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="noteId">L'id de la note</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpDelete("{noteId}")]
    [ProducesResponseType(200, Type = typeof(Note))]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Supprimer(Guid id, Guid noteId, CancellationToken token)
    {
        var entity = await _depot.Notes.Include(n => n.Rappel).FirstOrDefaultAsync(n => n.Id == noteId, token);

        if (entity != null && entity.IdErabliere == id)
        {
            if (entity.Rappel != null)
            {
                _depot.Rappels.Remove(entity.Rappel);
                await _depot.SaveChangesAsync(token);
            }

            _depot.Notes.Remove(entity);

            await _depot.SaveChangesAsync(token);

            entity.File = null;

            return Ok(entity);
        }
        else
        {
            return NotFound();
        }
    }

}
