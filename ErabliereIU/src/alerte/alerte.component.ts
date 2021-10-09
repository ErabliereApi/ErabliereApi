import { Component, Input, OnInit } from "@angular/core";
import { Alerte } from "src/model/alerte";

@Component({
    selector: 'alerte-page',
    template: `
        <h3>Alertes</h3>
        <ajouter-alerte-modal *ngIf="!displayEditForm"
            [idErabliereSelectionee]="idErabliereSelectionee"
            [alertes]="alertes">
        </ajouter-alerte-modal>
        <modifier-alerte-modal
            [idErabliereSelectionee]="idErabliereSelectionee"
            [display]="displayEditForm">
        </modifier-alerte-modal>
        <div>
            <p *ngIf="alertes != null && alertes.length == 0">Aucune alerte de configuré</p>
            <table *ngIf="alertes != null && alertes.length > 0" class="table">
                <thead>
                    <tr>
                        <th>
                            Id
                        </th>
                        <th>
                            Envoyer à
                        </th>
                        <th>
                            Temperature min.
                        </th>
                        <th>
                            Temperature max.
                        </th>
                        <th>
                            Vaccium min.
                        </th>
                        <th>
                            Vaccium max.
                        </th>
                        <th>
                            Niveau bassin min.
                        </th>
                        <th>
                            Niveau bassin max.
                        </th>
                        <th></th>
                    <tr>
                </thead>
                <tbody>
                    <tr *ngFor="let alerte of alertes">
                        <td>
                            {{alerte.id}}
                        </td>
                        <td>
                            {{alerte.envoyerA}}
                                
                        </td>
                        <td>
                            {{alerte.temperatureThresholdHight}}
                        </td>
                        <td>
                            {{alerte.temperatureThresholdLow}}
                        </td>
                        <td>
                            {{alerte.vacciumThresholdHight}}
                        </td>
                        <td>
                            {{alerte.vacciumThresholdLow}}
                        </td>
                        <td>
                            {{alerte.niveauBassinThresholdHight}}
                        </td>
                        <td>
                            {{alerte.niveauBassinThresholdLow}}
                        </td>
                        <td>
                            <button (click)="onButtonModifierClick(alerte.id)">modifier</button>
                            <button>supprimer</button>
                        </td>
                    <tr>
                </tbody>
            </table>
        </div>
    `
})
export class AlerteComponent{
    constructor() { }

    @Input() alertes?: Array<Alerte>;

    @Input() idErabliereSelectionee:any

    displayEditForm: boolean = false;

    onButtonModifierClick(alerteId:any) {
        this.displayEditForm = true;
    }
}