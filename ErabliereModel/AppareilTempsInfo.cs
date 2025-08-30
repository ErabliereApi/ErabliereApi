using System;

namespace ErabliereApi.Donnees;

public class AppareilTempsInfo
{
    public Guid Id { get; set; }

    public long DateDebut { get; set; }

    public long DateFin { get; set; }

    public long Strtt { get; set; }

    public long Rttvar { get; set; }

    public long To { get; set; }

    public Guid IdAppareil { get; set; }
}