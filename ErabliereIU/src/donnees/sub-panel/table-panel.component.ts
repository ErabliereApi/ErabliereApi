import { Component, Input, OnInit } from '@angular/core';
import { PanelHeaderComponent } from './header/panel-header.component';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { DonneeCapteur } from 'src/model/donneeCapteur';
import { DatePipe } from '@angular/common';
import { AjouterDonneeCapteurComponent } from 'src/donneeCapteurs/ajouter-donnee-capteur.component';

@Component({
    selector: 'table-panel',
    template: `
        <div id="graph-pannel-{{idCapteur}}" [className]="online == undefined || online ? '' : 'border-top border-danger'">
          <panel-header
            [titre]="titre"
            [symbole]="symbole"
            [idCapteur]="idCapteur"
            [batteryLevel]="batteryLevel"
            [online]="online"
            [ajouterDonneeDepuisInterface]="ajouterDonneeDepuisInterface"
          [valeurActuel]="valeurActuel"></panel-header>
          <div class="container">
            <div class="row">
              @if (ajouterDonneeDepuisInterface) {
                <ajouter-donnee-capteur
                  class="col-md-auto px-0 mb-2"
                  [idCapteur]="idCapteur"
                  (needToUpdate)="updateDonneesCapteur($event)"
                  [symbole]="symbole" />
                }
              </div>
            </div>
            <table class="table table-striped table-bordered">
              <thead>
                <tr>
                  <th scope="col">Date</th>
                  <th scope="col">Valeur</th>
                  <th scope="col">Unité</th>
                </tr>
              </thead>
              <tbody>
                @for (donnee of donnees; track donnee) {
                  <tr>
                    <td>{{donnee.d | date:'short'}}</td>
                    <td>{{donnee.valeur}}</td>
                    <td>{{symbole}}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        `,
    imports: [
    PanelHeaderComponent,
    DatePipe,
    AjouterDonneeCapteurComponent
]
})
export class TablePanelComponent implements OnInit {
    @Input() titre?: string;
    @Input() symbole?: string;
    @Input() idCapteur?: any;
    @Input() batteryLevel?: number;
    @Input() online?: boolean;
    @Input() ajouterDonneeDepuisInterface?: boolean;
    @Input() displayTop?: number;

    donnees: DonneeCapteur[] = [];

    constructor(private readonly _api: ErabliereApi) { }

    interval?: any
    fixRange = false;
    valeurActuel?: number;

    ngOnInit(): void {
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

    doHttpCall(): void {
        this._api.getDonneesCapteurTop(this.idCapteur, this.displayTop ?? 10).then(
            (data) => {
                if (data) {
                    this.donnees = data;
                    if (data.length > 0) {
                        this.valeurActuel = data[0].valeur;
                    }
                    else {
                        this.valeurActuel = undefined;
                    }
                }
                    
                if (!data) {
                    this.donnees = [];
                    this.valeurActuel = undefined;
                }
            },
            (error) => {
                console.error(error);
            }
        );
    }

    updateDonneesCapteur($event: any) {
        this.doHttpCall();
    }
}