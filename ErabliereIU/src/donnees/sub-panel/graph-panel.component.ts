import { Component, EventEmitter, Input, OnInit, Output, SimpleChange, ViewChild } from '@angular/core';
import { ChartDataset, ChartOptions, ChartType } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { AjouterDonneeCapteurComponent } from '../../donneeCapteurs/ajouter-donnee-capteur.component';
import { DateTimeSelectorComponent } from './userinput/date-time-selector.component';
import { calculerMoyenne } from '../util';
import { CapteurStyle } from 'src/model/capteur';

@Component({
    selector: 'graph-panel',
    templateUrl: './graph-panel.component.html',
    imports: [
        DateTimeSelectorComponent,
        AjouterDonneeCapteurComponent,
        BaseChartDirective,
    ]
})
export class GraphPanelComponent implements OnInit {
    @ViewChild(BaseChartDirective) chart?: BaseChartDirective;
    @Input() datasets: ChartDataset[] = [];
    @Input() timeaxes: string[] = [];
    @Input() lineChartType = 'line' as ChartType;
    @Input() lineScaleType: 'time' | 'timeseries' = 'time'
    @Input() displayMin: number | undefined = undefined;
    @Input() displayMax: number | undefined = undefined;
    lineChartOptions?: ChartOptions;

    lineChartLegend = true;
    lineChartPlugins = [];

    @Input() titre: string | undefined = "";
    @Input() valeurActuel: string | null | number | undefined;
    @Input() symbole: string | undefined;
    @Input() textActuel: string | undefined | null;
    @Input() ajouterDonneeDepuisInterface: boolean = false;
    @Input() batteryLevel: number | undefined;
    @Input() online: boolean | undefined;
    @Input() capteurStyle?: CapteurStyle;

    errorMessage: any;

    col12: string = "col-12";
    col10: string = "col-10";

    constructor(private readonly _api: ErabliereApi) {
        this.chart = undefined;
    }

    @Input() idCapteur?: any;

    interval?: any;

    getGradient(ctx: any, chartArea: any) {
        let width, height, gradient;
        const chartWidth = chartArea.right - chartArea.left;
        const chartHeight = chartArea.bottom - chartArea.top;
        if (!gradient || width !== chartWidth || height !== chartHeight) {
            gradient = ctx.createLinearGradient(0, chartArea.bottom, 0, chartArea.top);
            gradient.addColorStop(0, 'blue');
            gradient.addColorStop(0.5, 'yellow');
            gradient.addColorStop(1, 'red');
        }

        return gradient;
    }

    ngOnInit(): void {
        this.lineChartOptions = {
            maintainAspectRatio: false,
            aspectRatio: 1.7,
            backgroundColor: this.capteurStyle?.backgroundColor ?? 'rgba(255,255,0,0.28)',
            color: this.capteurStyle?.color ?? 'black',
            borderColor: this.capteurStyle?.borderColor ?? 'black',
            scales: {
                x: {
                    type: this.lineScaleType,
                    time: {
                        tooltipFormat: 'yyyy-MM-dd HH:mm:ss',
                        displayFormats: {
                            minute: 'dd MMM HH:mm'
                        },
                    },
                    ticks: {
                        autoSkip: true,
                        maxTicksLimit: 6,
                    }
                },
                y: {
                    min: this.displayMin,
                    max: this.displayMax,
                }
            }
        };

        if (this.idCapteur != null) {
            this.doHttpCall();

            this.interval = setInterval(() => {
                if (!this.fixRange) {
                    this.doHttpCall();
                }
            }, 1000 * 60);
        }
    }

    ngOnDestroy() {
        clearInterval(this.interval);
    }

    updateDonneesCapteur(event: any) {
        this.cleanGraphComponentCache();
        this.doHttpCall();
    }

    dernierDonneeRecu?: string = undefined;
    ddr?: string = undefined;

    ids: Array<number> = []

    doHttpCall(): void {
        let debutFiltre = this.obtenirDebutFiltre().toISOString();
        let finFiltre = new Date().toISOString();

        if (this.fixRange) {
            debutFiltre = this.dateDebutFixRange;
            finFiltre = this.dateFinFixRange;
        }

        let xddr = null;
        if (this.dernierDonneeRecu != undefined) {
            xddr = this.dernierDonneeRecu.toString();
        }

        this.errorMessage = undefined;

        this._api.getDonneesCapteur(this.idCapteur, debutFiltre, finFiltre, xddr).then(resp => {
            const h = resp.headers;

            this.dernierDonneeRecu = h.get("x-dde")?.valueOf();
            this.ddr = h.get("x-ddr")?.valueOf();

            let json = resp.body;

            if (json == null) {
                console.log("donneeCapteur response body was null. Return immediatly");
                return;
            }

            let ids = json.map(ee => ee.id);

            let donnees: Array<ChartDataset> = [
                {
                    data: json.map(donneeCapteur => donneeCapteur.valeur ?? null), 
                    label: this.titre,
                    fill: this.capteurStyle?.fill ?? true,
                    borderColor: this.capteurStyle?.useGradient ? this.getGradient(this.chart?.ctx, this.chart?.chart?.chartArea) : null,
                    pointBackgroundColor: this.capteurStyle?.pointBackgroundColor ?? 'rgba(255,255,0,0.8)',
                    pointBorderColor: this.capteurStyle?.pointBackgroundColor ?? 'black',
                    tension: this.capteurStyle?.tension ?? 0.5,
                }
            ];

            let timeaxes = json.map(donneeCapteur => donneeCapteur.d);

            if (json.length > 0) {
                let actualData = json[json.length - 1];
                let tva = actualData.valeur;
                this.valeurActuel = tva;
                this.textActuel = actualData.text;
            }

            if (h.has("x-ddr") && this.ddr != undefined && h.get("x-ddr")?.valueOf() == this.ddr) {

                if (ids.length > 0 && this.ids[this.ids.length - 1] === ids[0]) {
                    this.datasets[0].data?.pop();
                    this.timeaxes.pop();

                    this.datasets[0].data?.push(donnees[0].data.shift() as any);
                    this.timeaxes.push(timeaxes.shift() as any);
                }

                donnees[0].data.forEach(t => this.datasets[0].data?.push(t as any));
                timeaxes.forEach(t => this.timeaxes.push(t as any));
                ids.forEach((n: number) => this.ids.push(n));

                while (this.timeaxes.length > 0 &&
                    new Date(this.timeaxes[0].toString()) < new Date(debutFiltre)) {
                    this.timeaxes.shift();
                    this.datasets[0].data?.shift();
                    this.ids.shift();
                }
            }
            else {
                this.datasets = donnees;
                this.timeaxes = timeaxes as any[];
                this.ids = ids;
            }

            this.chart?.update();

            if (this.fixRange) {
                this.mean = " Moyenne: " + calculerMoyenne(this.datasets[0]) + this.symbole;
            }
            else {
                this.mean = undefined;
            }
        }).catch(err => {
            console.error(err);
            this.errorMessage = err?.message;
        });
    }

    duree: string = "12h"
    debutEnHeure: number = 12;

    obtenirDebutFiltre(): Date {
        const twelve_hour = 1000 * 60 * 60 * this.debutEnHeure;

        return new Date(Date.now() - twelve_hour);
    }

    @Output() updateGraphCallback = new EventEmitter();
    @Output() updateGraphUsingFixRangeCallback = new EventEmitter();

    updateGraph(days: number, hours: number): void {
        this.fixRange = false;
        if (this.idCapteur != null) {
            this.duree = "";

            if (days != 0) {
                this.duree = days + " jours";
            }

            if (hours != 0) {
                this.duree = this.duree + " " + hours + "h";
            }

            this.debutEnHeure = hours + (24 * days);

            this.cleanGraphComponentCache();

            this.doHttpCall();
        }
        else {
            // When this.idCapteur is null, we are in a component such as 'donnees.component.ts'
            this.updateGraphCallback.emit({ days, hours });
        }
    }

    private cleanGraphComponentCache() {
        this.dernierDonneeRecu = undefined;
        this.ddr = undefined;
        this.ids = [];
    }

    updateDuree(duree: string) {
        this.duree = duree;
    }

    fixRange: boolean = false;

    @Input() mean?: string = undefined;

    updateGraphUsingFixRange() {
        this.fixRange = true;
        if (this.idCapteur != null) {
            this.cleanGraphComponentCache();
            this.updateDuree(this.dateDebutFixRange + " - " + this.dateFinFixRange);
            this.doHttpCall();
        }
        else {
            // When this.idCapteur is null, we are in a component such as 'donnees.component.ts'
            let obj = {
                dateDebutFixRange: this.dateDebutFixRange,
                dateFinFixRange: this.dateFinFixRange
            }
            this.updateGraphUsingFixRangeCallback.emit(obj);
        }
    }

    dateDebutFixRange: any = undefined;
    dateFinFixRange: any = undefined;

    captureDateDebut($event: SimpleChange) {
        this.dateDebutFixRange = $event.currentValue;
    }

    captureDateFin($event: SimpleChange) {
        this.dateFinFixRange = $event.currentValue;
    }

    dropDownKeyUpEvent($event: KeyboardEvent) {
        console.log($event);
    }
}
