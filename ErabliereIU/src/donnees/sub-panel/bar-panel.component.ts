import { Component, Input, ViewChild } from '@angular/core';
import { ChartDataset, ChartOptions, ChartType } from 'chart.js';
import { BaseChartDirective, NgChartsModule } from 'ng2-charts';

@Component({
    selector: 'bar-panel',
    template: `
        <div>
            <h3>{{ titre }} {{ valeurActuel }} {{ symbole }}</h3>

            <div class="btn-group">
                <div class="dropdown show">
                    <a class="btn btn-secondary dropdown-toggle"
                       href="#" role="button"
                       id="dropdownMenuLink"
                       data-toggle="dropdown"
                       aria-haspopup="true"
                       aria-expanded="false">
                        Durée {{ duree }}
                    </a>

                <div class="dropdown-menu" aria-labelledby="dropdownMenuLink">
                    <a class="dropdown-item" href="#">12h</a>
                </div>
            </div>

            </div>
            <div class="chart-wrapper">
                <canvas baseChart class="chart"
                    [datasets]="datasets"
                    [labels]="timeaxes"
                    [options]="lineChartOptions"
                    [legend]="lineChartLegend"
                    [plugins]="lineChartPlugins"
                    [type]="barChartType">
                </canvas>
            </div>
        </div>
    `,
    imports: [NgChartsModule]
})
export class BarPanelComponent {
    @ViewChild(BaseChartDirective) chart?: BaseChartDirective;

    @Input() datasets: ChartDataset[]

    @Input() timeaxes: string[]

    @Input() barChartType: ChartType;

    lineChartOptions: ChartOptions

    lineChartLegend: boolean
    lineChartPlugins: never[]

    @Input() titre:string
    duree:string
    @Input() valeurActuel:string
    @Input() symbole:string

    constructor() {
        this.datasets = []
        this.timeaxes = []
        this.barChartType = 'bar' as ChartType
        this.lineChartOptions = {
            maintainAspectRatio: false,
            aspectRatio: 1.7,
            borderColor: 'black',
            backgroundColor: 'rgba(33,42,234,0.78)',
            scales: {
                x: {
                    grid: {
                        offset: true
                    }
                }
            }
        }

        this.lineChartLegend = true
        this.lineChartPlugins = []
        this.titre = ""
        this.duree = "12h"
        this.valeurActuel = ""
        this.symbole = ""
        this.chart = undefined
    }
}
