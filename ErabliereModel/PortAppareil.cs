using System;

namespace ErabliereApi.Donnees;

public class PortAppareil
{
    public Guid Id { get; set; }
    public int Port { get; set; }
    public string Protocole { get; set; } = string.Empty;
    public PortEtat? Etat { get; set; }
    public PortService? PortService { get; set; }
    public Guid IdAppareil { get; set; }
}