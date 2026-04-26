
import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { HourlyTemperature, HourlyWeatherForecast } from 'src/model/hourlyweatherforecast';

@Component({
    selector: 'hourly-weather-forecast',
    template: `
    @if (displayAccueatherLogo && provider == "AccuWeatherService") {
    <div class="text-center">
                    <small class="text-muted">
                        Prévisions 12 heures fournies par <a href="https://www.accuweather.com" target="_blank"><img
                                src="/assets/weathericons/accuweather/favicon.ico" alt="AccuWeather Logo" width="20"
                                height="20"> AccuWeather</a>
                    </small>
                </div>
    }
    @if (displayAccueatherLogo && provider == "GouvCAWeatherService") {
        <div class="text-center">
                    <small class="text-muted">
                        Prévisions 24 heures fournies par <a href="https://weather.gc.ca/" target="_blank"><img
                    src="/assets/weathericons/weathergcca/favicon.ico" alt="Weather GC CA Logo" width="20" height="20">
                weather.qc.ca</a>
                    </small>
                </div>
    }
        <table class="table table-striped table-bordered table-hover">
          <thead>
            <tr>
              <th>Heure</th>
              <th>Température</th>
              <th>Icône</th>
              <th>Type</th>
              <th>Intensité</th>
            </tr>
          </thead>
          <tbody>
            @if (error) {
              <tr>
                <td colspan="5" class="text-danger">{{ error.message }}</td>
              </tr>
            } @else if (weatherData.length === 0) {
              <tr>
                <td colspan="5">Aucune donnée disponible</td>
              </tr>
            }
            @for (forecast of weatherData; track forecast.epochDateTime) {
              <tr>
                <td title={{forecast.link}}>{{ getHour(forecast) }}</td>
                <td>{{ convertToCelsius(forecast.temperature) }}°C</td>
                <td>
                  <img
                    [src]="'/assets/weathericons/' + getfolder() + '/' + pad2(forecast.weatherIcon) + '-s.png'"
                    [alt]="forecast.iconPhrase"
                    [title]="forecast.iconPhrase">
                  </td>
                  <td>{{ precipitationTypeText(forecast.precipitationType) }}</td>
                  <td>{{ precipitationIntensityText(forecast.precipitationIntensity) }}</td>
                </tr>
              }
            </tbody>
          </table>
        `,
    imports: []
})
export class HourlyWeatherForecastComponent implements OnInit, OnDestroy {
    weatherData: HourlyWeatherForecast[] = [];
    text?: string;
    error?: any;
    interval?: NodeJS.Timeout;
    idErabliere: any;
    @Input() displayAccueatherLogo: boolean = true;
    @Input() provider: string = "";

    constructor(private readonly api: ErabliereApi, private readonly route: ActivatedRoute) {
    }

    ngOnInit(): void {
        this.route.paramMap.subscribe((params) => {
            this.idErabliere = params.get('idErabliereSelectionee');

            if (this.interval != null) {
                clearInterval(this.interval);
            }

            if (this.idErabliere) {
                this.getWeatherData();
                this.interval = setInterval(() => {
                    this.getWeatherData();
                }, 1000 * 60 * 60);
            }
        });
        this.idErabliere = this.route.snapshot.params.idErabliereSelectionee;
        this.getWeatherData();
    }

    ngOnDestroy() {
        clearInterval(this.interval);
    }

    getWeatherData() {
        this.api.geHourlyWeatherForecast(this.idErabliere).then((data: HourlyWeatherForecast[]) => {
            this.weatherData = data;
            this.error = null;
        }).catch((error: any) => {
            console.error(error);
            this.error = error;
            this.weatherData = [];
        });
    }

    convertToCelsius(hourlyTemp?: HourlyTemperature) {
        if (hourlyTemp?.value == null) {
            return '';
        }

        if (hourlyTemp.unit === "C") {
            return hourlyTemp.value;
        }

        if (hourlyTemp.unit === "F") {
            return Math.round((hourlyTemp.value - 32) * 5 / 9);
        }

        throw new Error("Cannot convert unit " + hourlyTemp.unit + " to celcius");
    }

    getHour(_t12: HourlyWeatherForecast): string {
        if (_t12.dateTime) {
            const date = new Date(_t12.dateTime);

            const hours = date.getHours();

            return hours + 'h';
        }
        return '';
    }

    precipitationTypeText(arg0: string | undefined) {
        if (arg0 == null) {
            return '';
        }

        switch (arg0) {
            case 'Rain':
                return 'Pluie';
            case 'Snow':
                return 'Neige';
            case 'Ice':
                return 'Glace';
            case 'Mixed':
                return 'Mixte';
            default:
                return arg0;
        }
    }

    precipitationIntensityText(arg0: string | undefined) {
        if (arg0 == null) {
            return '';
        }

        switch (arg0) {
            case 'Light':
                return 'Légère';
            case 'Moderate':
                return 'Modérée';
            case 'Heavy':
                return 'Forte';
            default:
                return arg0;
        }
    }

    pad2(n?: number): string {
        if (n == null) {
            return '';
        }

        return n.toString().padStart(2, '0');
    }

    getfolder() {
        if (this.provider == "GouvCAWeatherService") {
            return 'weathergcca'
        }
        if (this.provider == "AccuWeatherService") {
            return 'accuweateher'
        }
        throw new Error("Provider inconnue")
    }
}
