using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErabliereApi.Donnees.Interfaces;

/// <summary>
/// Interface pour les entités qui ont un stockage de fichier
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// L'extension du fichier
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// Le fichier de la documentation
    /// </summary>
    public byte[]? File { get; set; }

    /// <summary>
    /// La taille du fichier en byte
    /// </summary>
    public int? FileSize { get; set; }

    /// <summary>
    /// Le nom du fichier
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Le type du stockage externe
    /// </summary>
    public string? ExternalStorageType { get; set; }

    /// <summary>
    /// L'url du fichier depuis le stockage externe
    /// </summary>
    public string? ExternalStorageUrl { get; set; }
}
