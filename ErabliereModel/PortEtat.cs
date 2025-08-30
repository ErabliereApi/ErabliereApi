using System;

namespace ErabliereApi.Donnees;

public class PortEtat
{
    public Guid Id { get; set; }
    public string Etat { get; set; } = string.Empty;
    public string Raison { get; set; } = string.Empty;
    public string RaisonTTL { get; set; } = string.Empty;
    public Guid IdPortAppareil { get; set; }
}