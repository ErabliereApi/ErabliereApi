using System;
using System.Collections.Generic;

namespace ErabliereApi.Donnees;

public class PortService
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Produit { get; set; }
    public string? ExtraInfo { get; set; }
    public string? Methode { get; set; }
    public List<string>? CPEs { get; set; }
    public Guid IdPortEtat { get; set; }
}