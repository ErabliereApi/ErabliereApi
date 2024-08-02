import { Component, Input } from '@angular/core';
import { Capteur } from 'src/model/capteur';

@Component({
    selector: 'capteur-detail-tooltip',
    template: `	
    @if (showsTooltip) {
<div class="mytooltip" 
    [style.top]="topPosition + 'px'"
    [style.left]="leftPosition + 'px'">

    <!-- add a x button to close the tooltip -->
     <div class="row mb-5">

        <button type="button" class="btn-close position-absolute top-0 end-0 mt-3 me-3" aria-label="Close" (click)="showsTooltip = false"></button>
        </div>
        <data>
                    <dt>Id</dt>
                    <dd>{{capteur.id}}</dd>

                    <dt>Id Erabliere</dt>
                    <dd>{{capteur.idErabliere}}</dd>

                    <dt>Symbole</dt>
                    <dd>{{capteur.symbole}}</dd>

                    <dt>Afficher Capteur Dashboard</dt>
                    <dd>{{capteur.afficherCapteurDashboard}}</dd>

                    <dt>Ajouter Donnee Depuis Interface</dt>
                    <dd>{{capteur.ajouterDonneeDepuisInterface}}</dd>

                    <dt>DC</dt>
                    <dd>{{capteur.dc}}</dd>

                    <dt>Indice Ordre</dt>
                    <dd>{{capteur.indiceOrdre}}</dd>

                    <dt>Taille</dt>
                    <dd>{{capteur.taille}}</dd>

                    <dt>Battery Level</dt>
                    <dd>{{capteur.batteryLevel}}</dd>

                    <dt>Type</dt>
                    <dd>{{capteur.type}}</dd>

                    <dt>External Id</dt>
                    <dd>{{capteur.externalId}}</dd>

                    <dt>Last Message Time</dt>
                    <dd>{{capteur.lastMessageTime}}</dd>

                    <dt>Online</dt>
                    <dd>{{capteur.online}}</dd>

                    <dt>Report Frequency</dt>
                    <dd>{{capteur.reportFrequency}}</dd>
    </data>
            </div>
    }
    
        `,
    styles: [`
        .mytooltip {
            position: absolute;
            font-size: 14px;
            line-height: 14px;
            padding: 5px 10px;
            background-color: #000000;
            color: black;
            border-radius: 3px;
            display: inline-block;
            box-shadow: 4px 6px 12px #00000020;
            background: #1ecb2f;
            /* Chrome 10-25, Safari 5.1-6 */
            background: -webkit-linear-gradient(to bottom right, rgb(144, 251, 165), rgb(33, 250, 69));
            /* W3C, IE 10+/ Edge, Firefox 16+, Chrome 26+, Opera 12+, Safari 7+ */
            background: linear-gradient(to bottom right, rgb(91, 243, 116), rgb(22, 248, 82));
            }
        `],
    standalone: true
})
export class CapteurDetailTooltipComponent {
    @Input() capteur: Capteur;
    @Input() showsTooltip = false;

    @Input() topPosition? = 215;
    @Input() leftPosition? = 400;

    constructor() {
        this.capteur = new Capteur();
    }

    closeTooltip() {
        this.showsTooltip = false;
    }
}