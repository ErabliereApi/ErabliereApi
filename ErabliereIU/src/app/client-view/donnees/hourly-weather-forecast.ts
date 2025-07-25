
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { HourlyWeatherForecast } from 'src/model/hourlyweatherforecast';

@Component({
    selector: 'hourly-weather-forecast',
    template: `
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
            @for (forecast of weatherData; track forecast) {
              <tr>
                <td title={{forecast.link}}>{{ getHour(forecast) }}</td>
                <td>{{ convertToCelsius(forecast.temperature?.value) }}°C</td>
                <td>
                  <img
                    [src]="'/assets/weathericons/accuweather/' + pad2(forecast.weatherIcon) + '-s.png'"
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

    convertToCelsius(fahrenheit?: number) {
        if (fahrenheit == null) {
            return '';
        }

        return Math.round((fahrenheit - 32) * 5 / 9);
    }

    getHour(_t12: HourlyWeatherForecast): string {
        if (_t12.dateTime) {
            const date = new Date(_t12.dateTime);

            const hours = date.getHours();

            return hours + 'h';
        }
        return '';
    }

    precipitationTypeText(arg0: string|undefined) {
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

    precipitationIntensityText(arg0: string|undefined) {
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
}
