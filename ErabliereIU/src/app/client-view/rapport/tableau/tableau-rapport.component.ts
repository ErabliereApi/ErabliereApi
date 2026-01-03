import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Rapport } from 'src/model/rapport';

@Component({
    selector: 'app-tableau-rapport',
    templateUrl: './tableau-rapport.component.html',
    styleUrls: ['./tableau-rapport.component.css'],
    imports: [CommonModule]
})
export class TableauRapportComponent implements OnChanges {
    @Input() rapport: Rapport | null = null;
    rapportData: Rapport | null = null;
    erreur: string | null = null;

    constructor(private readonly _api: ErabliereApi) {

    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['rapport']?.currentValue) {
            const rapportId = changes['rapport'].currentValue.id;
            const erabliereId = changes['rapport'].currentValue.idErabliere;
            this._api.getRapport(erabliereId, rapportId).then(rapport => {
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

    sortPropName?: string;
    sortDirection: 'asc' | 'desc' = 'asc';

    keyPressSortBy($event: KeyboardEvent, arg0: 'date' | 'moyenne' | 'somme' | 'min' | 'max') {
        if ($event.key === 'Enter') {
            this.sortBy(arg0);
        }
    }

    sortBy(arg0: 'date' | 'moyenne' | 'somme' | 'min' | 'max') {
        this.rapportData?.donnees?.sort((a, b) => {
            if (a[arg0] === undefined || a[arg0] === null) return 1;
            if (b[arg0] === undefined || b[arg0] === null) return -1;
            const an = a[arg0] as number;
            const bn = b[arg0] as number;
            if (an < bn) return -1;
            if (an > bn) return 1;
            return 0;
        });
        if (this.sortPropName === arg0) {
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
            if (this.sortDirection === 'desc') {
                this.rapportData?.donnees?.reverse();
            }
        }
        this.sortPropName = arg0;
    }

    getHeaderClass(arg0: string) {
        if (this.sortPropName === arg0) {
            return this.sortDirection === 'asc' ? 'sortable-down' : 'sortable-up';
        }
        return 'sortable-down';
    }
}