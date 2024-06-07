import { NgFor } from '@angular/common';
import { Component, Input, SimpleChanges } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Erabliere } from 'src/model/erabliere';
import { HourlyWeatherForecast } from 'src/model/hourlyweatherforecast';

@Component({
    selector: 'hourly-weather-forecast',
    template: `
        <table class="table table-striped table-bordered table-hover">
            <thead>
                <tr>
                    <th>Heure</th>
                    <th>Température</th>
                    <th>Info</th>
                    <th>Précipitation</th>
                    <th>Type</th>
                    <th>Intensité</th>
                </tr>
            </thead>
            <tbody>
                <tr *ngFor="let forecast of weatherData">
                    <td>{{ getHour(forecast) }}</td>
                    <td>{{ convertToCelsius(forecast.temperature?.value) }}°C</td>
                    <td>{{ forecast.iconPhrase }}</td>
                    <td>{{ forecast.hasPrecipitation }}</td>
                    <td>{{ forecast.precipitationType }}</td>
                    <td>{{ forecast.precipitationIntensity }}</td>
                </tr>
            </tbody>
        </table>
    `,
    standalone: true,
    imports: [
        NgFor
    ]
})
export class HourlyWeatherForecastComponent {
    @Input() erabliere?: Erabliere;

    weatherData: HourlyWeatherForecast[] = [];
    text?: string;
    error?: any;
    interval?: NodeJS.Timeout;

    constructor(private api: ErabliereApi) {
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['erabliere']) {
            if (this.interval != null) {
                clearInterval(this.interval);
            }
            if (this.erabliere?.id && this.erabliere?.codePostal) {
                this.getWeatherData();
                this.interval = setInterval(() => {
                    this.getWeatherData();
                }, 1000 * 60 * 60);
            }
        }
    }

    ngOnDestroy() {
        clearInterval(this.interval);
    }

    getWeatherData() {
        this.api.geHourlyWeatherForecast(this.erabliere?.id).then((data: HourlyWeatherForecast[]) => {
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
            var date = new Date(_t12.dateTime);

            var hours = date.getHours();

            return hours + 'h';
        }
        return '';
    }
}
