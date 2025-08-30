using System;

namespace ErabliereApi.Donnees;

public class AppareilAdresse
{
    public Guid Id { get; set; }
    public string Addr { get; set; } = string.Empty;
    public int Addrtype { get; set; }
    public string Vendeur { get; set; } = string.Empty;
    public Guid IdAppareil { get; set; }
}