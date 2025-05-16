﻿namespace ErabliereApi.Donnees.Action.Patch;

/// <summary>
/// Model use to patch conversation
/// </summary>
public class PatchConversation
{
    /// <summary>
    /// New userId to set
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// IsPublic to set
    /// </summary>
    public bool? IsPublic { get; set; }
}
