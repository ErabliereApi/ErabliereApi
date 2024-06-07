export class HourlyWeatherForecast {
    dateTime?: string;
    epochDateTime?: number;
    weatherIcon?: number;
    iconPhrase?: string;
    hasPrecipitation?: boolean;
    precipitationType?: string;
    precipitationIntensity?: string;
    temperature?: HourlyTemperature;
    mobileLink?: string;
    link?: string;
}

export class HourlyTemperature {
    value?: number;
    unit?: string;
    unitType?: number;
}