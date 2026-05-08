using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ErabliereApi.Donnees;

public class ChirpStackSrvConfig
{
    public Guid? Id { get; set; }

    [MaxLength(100)]
    public string? TenantId { get; set; }

    [MaxLength(100)]
    public string? TenantName { get; set; }

    [MaxLength(100)]
    public string? ApplicationId { get; set; }

    [MaxLength(100)]
    public string? ApplicationName { get; set; }

    [MaxLength(100)]
    public string? DeviceProfileId { get; set; }

    [MaxLength(100)]
    public string? DeviceProfileName { get; set; }

    [MaxLength(100)]
    public string? DeviceName { get; set; }

    [MaxLength(100)]
    public string? DevEui {  get; set; }

    [MaxLength(100)]
    public string? DeviceClassEnabled { get; set; }
}
