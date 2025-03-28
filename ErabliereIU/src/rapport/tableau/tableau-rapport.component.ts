import { NgFor } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Rapport } from 'src/model/rapport';

@Component({
    selector: 'app-tableau-rapport',
    template: `
        <div>
            @if (erreur) {
                <div class="alert alert-danger" role="alert">
                    {{ erreur }}
                </div>
            }
            <div class="card mb-3 mt-3">
                <div class="card-header">
                    <h5 class="card-title">{{rapport?.type}} - {{ formatDate(rapport?.dateDebut) }} - {{ formatDate(rapport?.dateFin) }}</h5>
                </div>
                <div class="card-body">
                    <div class="row">
                        <div class="col-md-3">
                            <strong>Moyenne:</strong>
                            {{ formatNumber(rapportData?.moyenne) }}
                        </div>
                        <div class="col-md-3">
                            <strong>Somme:</strong>
                            {{ formatNumber(rapportData?.somme) }}
                        </div>
                        <div class="col-md-3">
                            <strong>Min:</strong>
                            {{ formatNumber(rapportData?.min) }}
                        </div>
                        <div class="col-md-3">
                            <strong>Max:</strong>
                            {{ formatNumber(rapportData?.max) }}
                        </div>
                    </div>
                </div>
            </div>
            <table class="table table-striped table-responsive">
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Moyenne</th>
                        <th>Degré jour</th>
                        <th>Min</th>
                        <th>Max</th>
                    </tr>
                </thead>
                <tbody>
                    <tr *ngFor="let ligne of rapportData?.donnees">
                        <td>{{ formatDate(ligne.date) }}</td>
                        <td>{{ formatNumber(ligne.moyenne) }}</td>
                        <td>{{ formatNumber(ligne.somme) }}</td>
                        <td>{{ formatNumber(ligne.min) }}</td>
                        <td>{{ formatNumber(ligne.max) }}</td>
                    </tr>
                </tbody>
            </table>
        </div>
    `,
    styleUrls: ['./tableau-rapport.component.css'],
    imports: [NgFor]
})
export class TableauRapportComponent implements OnChanges {
    @Input() rapport: Rapport | null = null;
    rapportData: Rapport | null = null;
    erreur: string | null = null;

    constructor(private readonly _api: ErabliereApi) {

    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['rapport']?.currentValue) {
            const rapId = changes['rapport'].currentValue.id;
            const erabliereId = changes['rapport'].currentValue.idErabliere;
            this._api.getRapport(erabliereId, rapId).then(rapport => {
                this.erreur = null;
                this.rapportData = rapport;
            }).catch(err => {
                console.error("TableauRapportComponent error fetching rapport:", err);
                this.erreur = "Erreur lors de la récupération du rapport. " + JSON.stringify(err);
            });
        }
    }

    formatNumber(value?: number): string {
        if (value === undefined || value === null) {
            return 'N/A';
        }
        return value.toFixed(2).replace('.', ',');
    }

    formatDate(date?: Date): string {
        if (date === undefined || date === null) {
            return 'N/A';
        }
        const options: Intl.DateTimeFormatOptions = { year: 'numeric', month: '2-digit', day: '2-digit' };
        return new Intl.DateTimeFormat('fr-CA', options).format(new Date(date));
    }
}