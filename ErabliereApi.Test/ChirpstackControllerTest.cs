using ErabliereApi.Controllers;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Test.Autofixture;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

public class ChirpstackControllerTest
{
    [Fact]
    public void CanDeserialize()
    {
        var str = JsonSerializer.Deserialize<PostChirpstackEvent>(Constants.ChirpStackEx1);

        Assert.NotNull(str);
    }

    [Theory, AutoApiData]
    public async Task CreerErabliereAnonyme(
        ChirpstackController controller, PostChirpstackEvent rootobject)
    {
        var resp = await controller.EventListener("up", rootobject, CancellationToken.None);

        Assert.NotNull(resp);
    }
}
