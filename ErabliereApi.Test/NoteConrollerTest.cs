using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Test.Autofixture;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

public class NoteConrollerTest
{
    [Theory, AutoApiData]
    public async Task AddNoteWithoutFile(
        NotesController controller,
        ErabliereDbContext context,
        PostNote postNote)
    {
        var e = context.Erabliere.GetRandom();
        Assert.NotNull(e.Id);
        postNote.IdErabliere = e.Id;

        var result = await controller.Ajouter(e.Id.Value, postNote, CancellationToken.None);

        Assert.NotNull(result);
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
    }

    [Theory, AutoApiData]
    public async Task AddNoteWithFile(
        NotesController controller,
        ErabliereDbContext context,
        PostNote postNote)
    {
        var e = context.Erabliere.GetRandom();
        Assert.NotNull(e.Id);
        postNote.IdErabliere = e.Id;
        postNote.File = Convert.ToBase64String(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        postNote.FileExtension = ".txt";

        var result = await controller.Ajouter(e.Id.Value, postNote, CancellationToken.None);

        Assert.NotNull(result);
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
    }
}
