import { Component, Input, OnChanges, OnDestroy, SimpleChanges } from '@angular/core';
import { Chart, TooltipItem } from 'chart.js';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { WeatherForecast } from 'src/model/weatherForecast';
import { Erabliere } from "src/model/erabliere";
import { HttpErrorResponse } from '@angular/common/http';

@Component({
    selector: 'weather-forecast',
    templateUrl: 'weather-forecast.component.html',
    standalone: true,
})
export class WeatherForecastComponent implements OnChanges, OnDestroy {
    @Input() erabliere?: Erabliere;

    chart: any;
    weatherData?: WeatherForecast;
    text?: string;
    error?: any;
    interval?: NodeJS.Timeout;

    constructor(private readonly api: ErabliereApi) {
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['erabliere']) {
            if (this.interval != null) {
                clearInterval(this.interval);
            }
            this.chart?.destroy();
            this.chart = null;
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
        this.chart?.destroy();
        this.chart = null;
    }

    getWeatherData() {
        this.api.getWeatherForecast(this.erabliere?.id).then((data: WeatherForecast) => {
            this.weatherData = data;
            this.error = null;
            this.createChart();
        }).catch((error: HttpErrorResponse) => {
            console.error(error);
            this.error = error;
            this.weatherData = undefined;
        });
    }

    convertToCelsius(fahrenheit: number) {
        return (fahrenheit - 32) * 5 / 9;
    }

    createChart() {
        if (this.weatherData == null) {
            return;
        }

        this.text = this.weatherData.headline?.text;

        const dataF = this.weatherData.dailyForecasts;
        const dates = dataF?.map(entry => {
            const d = new Date(entry.date);
            return d.toLocaleDateString('fr-CA', { day: '2-digit', month: 'short' });
        });
        const minTemperatures = dataF?.map(entry => this.convertToCelsius(entry.temperature.minimum.value));
        const maxTemperatures = dataF?.map(entry => this.convertToCelsius(entry.temperature.maximum.value));
        // Additional metrics
        const hasPrecipitation = dataF?.map(entry => entry.day.hasPrecipitation);
        const precipitationType = dataF?.map(entry => entry.day.precipitationType ?? '');
        const precipitationIntensity = dataF?.map(entry => entry.day.precipitationIntensity ?? '');

        if (this.chart != null) {
            try {
                this.chart.destroy();
                this.chart = null;
            } catch (error) {
                console.error(error);
            }
        }

        setTimeout(() => {
            const ctx = (document.getElementById('weatherChart') as HTMLCanvasElement)?.getContext('2d');
            let gradientMax = ctx?.createLinearGradient(0, 0, 0, 400);
            gradientMax?.addColorStop(0, 'rgba(255,0,0,0.4)');
            gradientMax?.addColorStop(1, 'rgba(255,255,255,0)');
            let gradientMin = ctx?.createLinearGradient(0, 0, 0, 400);
            gradientMin?.addColorStop(0, 'rgba(0,0,255,0.4)');
            gradientMin?.addColorStop(1, 'rgba(255,255,255,0)');

            this.chart = new Chart('weatherChart', {
                type: 'line',
                data: {
                    labels: dates,
                    datasets: [
                        {
                            label: 'Maximum',
                            data: maxTemperatures,
                            borderColor: 'red',
                            backgroundColor: gradientMax,
                            pointRadius: 6,
                        },
                        {
                            label: 'Minimum',
                            data: minTemperatures,
                            borderColor: 'blue',
                            backgroundColor: gradientMin,
                            pointRadius: 6,
                        },
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    aspectRatio: 1.7,
                    scales: {
                        x: {
                            type: 'category'
                        },
                        y: {
                            title: {
                                display: true,
                                text: 'Temperature (°C)'
                            },
                            position: 'left'
                        },
                    },
                    interaction: {
                        intersect: false,
                        mode: 'index',
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                title: (tti: TooltipItem<"line">[]) => {
                                    const i = tti[0].dataIndex;
                                    let dt = this.weatherData?.dailyForecasts?.[i].day.iconPhrase;
                                    let nt = this.weatherData?.dailyForecasts?.[i].night.iconPhrase;
                                    let precip = hasPrecipitation?.[i] ?
                                        `Précipitation: ${precipitationType?.[i] || ''} (${precipitationIntensity?.[i] || ''})` :
                                        'Pas de précipitation';
                                    return `Jour: ${dt} - Nuit: ${nt}\n${precip}`;
                                },
                                label: (context: any) => {
                                    let label = context.dataset.label ?? '';
                                    if (label) {
                                        label += ': ';
                                    }
                                    if (context.dataset.label === 'Maximum' || context.dataset.label === 'Minimum') {
                                        if (context.parsed.y !== null) {
                                            label += context.parsed.y.toFixed(2) + '°C';
                                        }
                                    }
                                    return label;
                                }
                            }
                        }
                    }
                }
            });
        }, 0);
    }
}
