import { Component, Input } from '@angular/core';
import { Rapport } from 'src/model/rapport';

@Component({
    selector: 'rapport-panel',
    template: `
        <div class="card text-center">
            <div class="card-header">
                <h5 class="card-title d-flex justify-content-between align-items-center">
                    <span>{{rapport?.type}} - {{rapport?.dateDebut?.toString()?.substring(0, 4)}}</span>
                </h5>
            </div>
            <div class="card-body">
                <p class="card-text h1 p-5">{{rapport?.somme}} {{getSymbol()}}</p>
            </div>
        </div>
    `,
})
export class RapportPanelComponent {
    @Input() rapport?: Rapport

    constructor() { }

    getSymbol(): string {
        if (this.rapport?.utiliserTemperatureTrioDonnee) {
            return "°C";
        }
        else {
            return "";
        }
    }
}