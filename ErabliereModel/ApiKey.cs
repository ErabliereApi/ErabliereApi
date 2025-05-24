﻿using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Modèle contenant représentant une clé d'api est l'information relié à cette clé
/// </summary>
public class ApiKey
{
    /// <summary>
    /// La clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La clé d'api sous forme de hash
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Nom de la clé d'api
    /// </summary>
    [MaxLength(50)]
    public string Name { get; set; } = "";

    /// <summary>
    /// Date de création de la clé
    /// </summary>
    public DateTimeOffset CreationTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Date de révocation de la clé
    /// </summary>
    public DateTimeOffset? RevocationTime { get; set; }

    /// <summary>
    /// Date de suppression de la clé
    /// </summary>
    public DateTimeOffset? DeletionTime { get; set; }

    /// <summary>
    /// L'id du <see cref="Customer"/> possédant la clé d'api
    /// </summary>
    public Guid CustomerId {get;set;}
    
    /// <summary>
    /// Le <see cref="Customer"/> possédant la clé d'api
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// The stripe subscription id related to the api key
    /// </summary>
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// Tel if the api key is active
    /// </summary>
    /// <returns></returns>
    public bool IsActive()
    {
        var now = DateTimeOffset.Now;

        return (RevocationTime == null || now < RevocationTime) &&
                (DeletionTime == null || now < CreationTime) &&
                DeletionTime == null;
    }
}
