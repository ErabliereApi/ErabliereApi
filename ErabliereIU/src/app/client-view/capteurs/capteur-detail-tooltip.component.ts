import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Capteur } from 'src/model/capteur';

@Component({
    selector: 'capteur-detail-tooltip',
    template: `
        <div class="tooltip-container">
            <div class="tooltip-column">
                <dt>Id</dt>
                <dd>{{capteur.id}}</dd>

                <dt>Nom</dt>
                <dd>{{capteur.nom}}</dd>

                <dt>Symbole</dt>
                <dd>{{capteur.symbole}}</dd>

                <dt>Afficher Capteur Dashboard</dt>
                <dd>{{capteur.afficherCapteurDashboard}}</dd>

                <dt>DC</dt>
                <dd>{{capteur.dc}}</dd>

                <dt>Taille</dt>
                <dd>{{capteur.taille}}</dd>

                <dt>Type</dt>
                <dd>{{capteur.type}}</dd>

                <dt>Last Message Time</dt>
                <dd>{{capteur.lastMessageTime}}</dd>
            </div>
            <div class="tooltip-column">
                <dt>Id Erabliere</dt>
                <dd>{{capteur.idErabliere}}</dd>

                <dt>Ajouter Donnee Depuis Interface</dt>
                <dd>{{capteur.ajouterDonneeDepuisInterface}}</dd>

                <dt>Indice Ordre</dt>
                <dd>{{capteur.indiceOrdre}}</dd>

                <dt>Battery Level</dt>
                <dd>{{capteur.batteryLevel}}</dd>

                <dt>External Id</dt>
                <dd>{{capteur.externalId}}</dd>

                <dt>Online</dt>
                <dd>{{capteur.online}}</dd>

                <dt>Display Type</dt>
                <dd>{{capteur.displayType}}</dd>

                <dt>Display Top</dt>
                <dd>{{capteur.displayTop}}</dd>

                <dt>Report Frequency</dt>
                <dd>{{capteur.reportFrequency}}</dd>
            </div>
        </div>
    `,
    styles: [`
        .tooltip-container {
            display: flex;
            gap: 2rem;
        }
        .tooltip-column {
            flex: 1;
        }
        dt {
            font-weight: bold;
        }
        dd {
            margin-bottom: 1em;
            margin-left: 0;
        }
    `]
})
export class CapteurDetailTooltipComponent {
    @Input() capteur: Capteur;
    @Output() needToUpdate = new EventEmitter();

    constructor() {
        this.capteur = new Capteur();
    }

    closeTooltip() {
        this.needToUpdate.emit();
    }
}