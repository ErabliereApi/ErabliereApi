using System;
using System.Text.Json.Serialization;
namespace ErabliereApi.Donnees.Action.Post;


public class PostChirpstackEvent
{
    public string deduplicationId { get; set; }
    public string time { get; set; }
    public Deviceinfo deviceInfo { get; set; }
    public string devAddr { get; set; }
    public bool adr { get; set; }
    public int dr { get; set; }
    public int fCnt { get; set; }
    public int fPort { get; set; }
    public bool confirmed { get; set; }
    public string data { get; set; }

    [JsonPropertyName("object")]
    public Object _object { get; set; }
    public Rxinfo[] rxInfo { get; set; }
    public Txinfo txInfo { get; set; }
    public string regionConfigId { get; set; }
}

public class Deviceinfo
{
    public string tenantId { get; set; }
    public string tenantName { get; set; }
    public string applicationId { get; set; }
    public string applicationName { get; set; }
    public string deviceProfileId { get; set; }
    public string deviceProfileName { get; set; }
    public string deviceName { get; set; }
    public string devEui { get; set; }
    public string deviceClassEnabled { get; set; }
    public Tags tags { get; set; }
}

public class Tags
{
}

public class Object
{
    public string payload { get; set; }
    public bool valid { get; set; }
    public object[] messages { get; set; }
    public float err { get; set; }
}

public class Txinfo
{
    public int frequency { get; set; }
    public Modulation modulation { get; set; }
}

public class Modulation
{
    public Lora lora { get; set; }
}

public class Lora
{
    public int bandwidth { get; set; }
    public int spreadingFactor { get; set; }
    public string codeRate { get; set; }
}

public class Rxinfo
{
    public string gatewayId { get; set; }
    public long uplinkId { get; set; }
    public string nsTime { get; set; }
    public int rssi { get; set; }
    public float snr { get; set; }
    public Location location { get; set; }
    public string context { get; set; }
    public string crcStatus { get; set; }
}

public class Location
{
}

