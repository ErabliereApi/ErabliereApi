using ErabliereApi.Services.LoRaWAN;
using System;
using Xunit;

namespace ErabliereApi.Test;

public class LoRaWANDecoderTest
{
    [Fact]
    public void WeatherStationDocExemple()
    {
        byte[] data = {
            0x01,
            0x01,
            0x15,
            0x41,
            0x00,
            0x00,
            0x00,
            0x5E, 
            0x05,
            0x00,
            0x11,
            0x02,
            0x01,
            0x56,
            0x00,
            0x00,
            0x00,
            0xFE,
            0x27,
            0x03
        };

        var decoded = LoRaWANPacketDecoder.TryDecodeData(Convert.ToBase64String(data));

        Assert.NotNull(decoded);
        Assert.NotNull(decoded.Mesurements);
        Assert.Equal(9, decoded.Mesurements.Length);

        Assert.Equal(4097, decoded.Mesurements[0].Mesure);
        Assert.Equal(27.7m, decoded.Mesurements[0].Value);
    }

    [Fact]
    public void TestShort()
    {
        byte[] data = {
            0b11111111,
            0b11111111
        };

        short s = (short)((data[0] << 8) | data[1]);

        Assert.Equal(-1, s);
    }

    [Fact]
    public void WeatherStationOutsideTempMinusExemple()
    {
        byte[] data = {
            0x01,
            0b11111111,
            0b11111111,
            0x41,
            0x00,
            0x00,
            0x00,
            0x5E,
            0x05,
            0x00,
            0x11,
            0x02,
            0x01,
            0x56,
            0x00,
            0x00,
            0x00,
            0xFE,
            0x27,
            0x03
        };

        var decoded = LoRaWANPacketDecoder.TryDecodeData(Convert.ToBase64String(data));

        Assert.NotNull(decoded);
        Assert.NotNull(decoded.Mesurements);
        Assert.Equal(9, decoded.Mesurements.Length);

        Assert.Equal(4097, decoded.Mesurements[0].Mesure);
        Assert.Equal(-0.1m, decoded.Mesurements[0].Value);
    }
}
