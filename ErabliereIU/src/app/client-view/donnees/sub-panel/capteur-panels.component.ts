import { NgClass } from '@angular/common';
import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Capteur } from 'src/model/capteur';
import { Erabliere } from 'src/model/erabliere';
import { GraphPanelComponent } from './graph-panel.component';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { TablePanelComponent } from './table-panel.component';
import { Rapport } from 'src/model/rapport';
import { RapportPanelComponent } from './rapport-panel.component';
import { ActivatedRoute } from '@angular/router';
import { HoraireTableComponent } from "../../horaire/horaire-table.component";
import { Horaire } from 'src/model/horaire';

@Component({
    selector: 'capteur-panels',
    templateUrl: './capteur-panels.component.html',
    imports: [GraphPanelComponent, TablePanelComponent, RapportPanelComponent, NgClass, HoraireTableComponent]
})
export class CapteurPanelsComponent implements OnInit, OnChanges {
    @Input() capteurs?: Capteur[] = []
    @Input() erabliere?: Erabliere
    isLogged: boolean = false;
    rapports: Rapport[] = [];

    public tailleGraphiques?: number = 6;

    constructor(private readonly _api: ErabliereApi, 
                private readonly _authService: AuthorisationFactoryService,
                private readonly _route: ActivatedRoute) {
        if (this._authService.getAuthorisationService().type == "AuthDisabled") {
            this.isLogged = true;
        }
        else {
            this._authService.getAuthorisationService().loginChanged.subscribe(loggedIn => {
                this.isLogged = loggedIn;
            });
        }
    }

    ngOnInit(): void {
        this._route.paramMap.subscribe(params => {
            const idErabliereSelectionee = params.get('idErabliereSelectionee');

            if (idErabliereSelectionee) {
                this._api.getRapports(idErabliereSelectionee, 'afficherDansDashboard eq true').then(rapports => {
                    this.rapports = rapports;
                }).catch(err => {
                    this.rapports = [];
                    console.error(err);
                });
            }
            else {
                this.rapports = [];
            }
        });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.capteurs) {
            let taille = this.capteurs?.find(capteur => capteur.taille)?.taille;
            if (taille) {
                this.tailleGraphiques = taille;
            } else {
                this.tailleGraphiques = 6;
            }
        }
        this._authService.getAuthorisationService().isLoggedIn().then(loggedIn => {
            this.isLogged = loggedIn;
        }).catch(err => {
            console.error(err);
            this.isLogged = false;
        });
    }

    changerDimension(taille: number) {
        this.tailleGraphiques = taille;
        if (this.capteurs) {
            for (let capteur of this.capteurs) {
                capteur.taille = taille
            }

            this._api.putCapteurs(this.erabliere?.id, this.capteurs);
        }
    }

    keyUpChangerDimension(event: KeyboardEvent, taille: number) {
        if (event.key == "Enter") {
            this.changerDimension(taille);
        }
    }

    getFirstHoraire(): Horaire | undefined {
        const horaires = this.erabliere?.horaires;
        if (horaires && horaires.length > 0) {
            return horaires[0];
        }
        return undefined;
    }
}
