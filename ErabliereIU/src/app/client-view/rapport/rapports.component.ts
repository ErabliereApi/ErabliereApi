import { Component, OnInit } from '@angular/core';
import { RapportDegreJourComponent } from './degreejour/rapport-degre-jour.component';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { ActivatedRoute } from '@angular/router';
import { Rapport } from 'src/model/rapport';
import { format } from 'date-fns';
import { TableauRapportComponent } from "./tableau/tableau-rapport.component";

import { ResponseRapportDegreeJours } from 'src/model/postDegresJoursRepportRequest';
import { EModalComponent } from 'src/generic/modal/emodal.component';
import { ModifierRapportsComponent } from './modifierRapport/app-modifier-rapport.component';

@Component({
    selector: 'app-reports',
    templateUrl: './rapports.component.html',
    styleUrls: ['./rapports.component.css'],
    imports: [
        RapportDegreJourComponent,
        TableauRapportComponent,
        EModalComponent,
        ModifierRapportsComponent
    ]
})
export class ReportsComponent implements OnInit {
    typeRapport: string = 'degreJour';
    rapportSavedError: string = '';
    rapportsSaved: Rapport[] = [];
    idErabliereSelectionee?: string | null;
    rapportSelected: Rapport | null = null;

    constructor(private readonly _api: ErabliereApi, private readonly _route: ActivatedRoute) { }

    ngOnInit(): void {
        this._route.paramMap.subscribe(params => {
            this.idErabliereSelectionee = params.get('idErabliereSelectionee');

            if (this.idErabliereSelectionee) {
                this.fetchRapports();
            }
            else {
                this.rapportSavedError = 'Erreur lors de la récupération des rapports. Aucune érablière sélectionnée.';
                this.rapportsSaved = [];
                this.rapportSelected = null;
            }
        });
    }

    fetchRapports(thenSelectId?: any) {
        if (!this.idErabliereSelectionee) {
            return;
        }
        this._api.getRapports(this.idErabliereSelectionee).then(rapports => {
            this.rapportsSaved = rapports;
            this.rapportSavedError = '';
            if (rapports.length > 0) {
                if (thenSelectId) {
                    this.rapportSelected = rapports.find(r => r.id === thenSelectId) || null;
                }
                else {
                    this.rapportSelected = rapports[0];
                }
            }
        }).catch(err => {
            this.rapportSavedError = 'Erreur lors de la récupération des rapports. ' + JSON.stringify(err);
            this.rapportsSaved = [];
            this.rapportSelected = null;
            console.error(err);
        });
    }

    typeRapportChanged($event: Event) {
        console.log("Type rapport changed", $event);
        this.typeRapport = ($event.target as HTMLSelectElement).value;
        console.log("Type rapport", this.typeRapport);
    }

    selectReport(rapport: Rapport) {
        console.log("Select rapport", rapport);
        this.rapportSelected = rapport;
    }

    refreshRapport(_t15: Rapport) {
        this._api.refreshRapport(this.idErabliereSelectionee, _t15.id).then(res => {
            this.fetchRapports(_t15.id);
            this.rapportSavedError = '';
        }).catch(err => {
            this.rapportSavedError = 'Erreur lors de la mise à jour du rapport. ' + JSON.stringify(err);
            console.error(err);
        });
    }

    deleteRapport(rapport: Rapport) {
        this._api.deleteRapport(this.idErabliereSelectionee, rapport.id).then(() => {
            this.rapportsSaved = this.rapportsSaved.filter(r => r.id !== rapport.id);
            this.rapportSavedError = '';
        }).catch(err => {
            this.rapportSavedError = 'Erreur lors de la suppression du rapport. ' + JSON.stringify(err);
            console.error(err);
        });
    }

    formatDate(date?: Date): string {
        if (!date) {
            return '';
        }
        return format(date, 'yyyy-MM-dd');
    }

    changeSelectedReport($event: ResponseRapportDegreeJours) {
        console.log("Change selected report", $event);
        this.fetchRapports($event.id);
    }

    isOpenModifierRapportModal: boolean = false;

    openModifierRapportModal(_t16: Rapport) {
        this.isOpenModifierRapportModal = true;
    }

    closeModifierRapportModal() {
        this.isOpenModifierRapportModal = false;
    }

    onRapportModifie($event: Event) {
        alert('Méthode non implémentée');
    }
}