namespace ErabliereApi.Services.MeteoMaticModels;

#pragma warning disable CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement

public class MeteoMaticLocationResponse
{
    public string? documentation { get; set; }

    public Licenses? licenses { get; set; }

    public List<MeteoMaticLocationResult>? result { get; set; }
}

public class Licenses
{
    public string? name { get; set; }
}

public class MeteoMaticLocationResult
{
    public Geometry? geometry { get; set; }
}

public class Geometry
{
    public double lat { get; set; }

    public double lng { get; set; }
}

#pragma warning restore CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement
