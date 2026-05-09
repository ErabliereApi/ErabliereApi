using ErabliereApi.Controllers;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Test.Autofixture;
using System;
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
        var guidId = Guid.NewGuid();

        var payload = Constants.ChirpStackExOk.Replace("<replace-guid-erabliere>", guidId.ToString());

        var eventInfo = JsonSerializer.Deserialize<PostChirpstackEvent>(payload);

        Assert.NotNull(eventInfo);
        Assert.NotNull(eventInfo.deviceInfo?.tags?.idErabliere);
        Assert.Equal(guidId, eventInfo.deviceInfo.tags.idErabliere.Value);
    }

    [Theory, AutoApiData]
    public async Task CreerErabliereAnonyme(
        ChirpstackController controller, PostChirpstackEvent rootobject)
    {
        var resp = await controller.EventListener("up", rootobject, CancellationToken.None);

        Assert.NotNull(resp);
    }
}
