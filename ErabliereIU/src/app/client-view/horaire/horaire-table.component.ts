import { Component, Input, OnChanges, SimpleChanges } from "@angular/core";
import { Horaire } from "src/model/horaire";

@Component({
    selector: "horaire-table",
    template: `
        <table class="table table-striped">
            <thead>
                <tr>
                    <th>Jour</th>
                    <th>Horaire</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Lundi</td>
                    <td>{{ horaire?.lundi }}</td>
                </tr>
                <tr>
                    <td>Mardi</td>
                    <td>{{ horaire?.mardi }}</td>
                </tr>
                <tr>
                    <td>Mercredi</td>
                    <td>{{ horaire?.mercredi }}</td>
                </tr>
                <tr>
                    <td>Jeudi</td>
                    <td>{{ horaire?.jeudi }}</td>
                </tr>
                <tr>
                    <td>Vendredi</td>
                    <td>{{ horaire?.vendredi }}</td>
                </tr>
                <tr>
                    <td>Samedi</td>
                    <td>{{ horaire?.samedi }}</td>
                </tr>
                <tr>
                    <td>Dimanche</td>
                    <td>{{ horaire?.dimanche }}</td>
                </tr>
            </tbody>
        </table>
    `
})
export class HoraireTableComponent implements OnChanges {
    @Input() horaire?: Horaire;

    constructor() { }
    
    ngOnChanges(changes: SimpleChanges): void {
        if (changes['horaire']) {
            this.horaire = changes['horaire'].currentValue;
        }
    }
}