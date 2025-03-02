import { Component, OnInit } from '@angular/core';
import { RapportDegreJourComponent } from './degreejour/rapport-degre-jour.component';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { ActivatedRoute } from '@angular/router';
import { Rapport } from 'src/model/rapport';
import { format } from 'date-fns';

@Component({
    selector: 'app-reports',
    templateUrl: './rapports.component.html',
    styleUrls: ['./rapports.component.css'],
    imports: [
        RapportDegreJourComponent
    ]
})
export class ReportsComponent implements OnInit {
    typeRapport: string = 'degreJour';

    rapportSavedError: string = '';
    rapportsSaved: Rapport[] = [];
    idErabliereSelectionee?: string | null;

    constructor(private readonly _api: ErabliereApi, private readonly _route: ActivatedRoute) { }

    ngOnInit(): void {
        this._route.paramMap.subscribe(params => {
            this.idErabliereSelectionee = params.get('idErabliereSelectionee');

            if (this.idErabliereSelectionee) {
                this._api.getRapports(this.idErabliereSelectionee).then(rapports => {
                    this.rapportsSaved = rapports;
                    this.rapportSavedError = '';
                }).catch(err => {
                    this.rapportSavedError = 'Erreur lors de la récupération des rapports. ' + JSON.stringify(err);
                    console.error(err);
                });
            }
            else {
                this.rapportSavedError = 'Erreur lors de la récupération des rapports. Aucune érablière sélectionnée.';
                this.rapportsSaved = [];
            }
        });
    }

    typeRapportChanged($event: Event) {
        console.log("Type rapport changed", $event);
        this.typeRapport = ($event.target as HTMLSelectElement).value;
        console.log("Type rapport", this.typeRapport);
    }

    rapportSelected(_t17: any) {
        console.log("Rapport selected", _t17);
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
}