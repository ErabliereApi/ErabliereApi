using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Test.Autofixture;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

public class DocumentationControllerTest
{
    [Theory, AutoApiData]
    public async Task AddDocumentationWithoutFile(
        DocumentationController controller,
        ErabliereDbContext context,
        PostDocumentation postDocumentation)
    {
        var e = context.Erabliere.GetRandom();
        Assert.NotNull(e.Id);
        postDocumentation.IdErabliere = e.Id;

        var result = await controller.Ajouter(e.Id.Value, postDocumentation, CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
    }

    [Theory, AutoApiData]
    public async Task AddDocumentationWithFile(
        DocumentationController controller,
        ErabliereDbContext context,
        PostDocumentation postDocumentation)
    {
        var e = context.Erabliere.GetRandom();
        Assert.NotNull(e.Id);
        postDocumentation.IdErabliere = e.Id;
        postDocumentation.File = Convert.ToBase64String(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        postDocumentation.FileExtension = ".bin";

        var result = await controller.Ajouter(e.Id.Value, postDocumentation, CancellationToken.None);
        Assert.NotNull(result);
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
    }
}
