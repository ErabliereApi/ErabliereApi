import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
    selector: 'panel-header',
    template: `
        <div class="row">
            <div [className]="batteryLevel ? col10 : col12">
                <h3>{{ titre }} {{ valeurActuel }} {{ symbole }} {{ mean }}</h3>
            </div>
            @if (batteryLevel != null && (online == undefined || online)) {
            <div class="col-2">
                    <div class="mt-2">
                        <div class="progress" title="Niveau de la batterie {{ batteryLevel }}%">
                            <div
                                    [className]="batteryLevel < 20 ? 'progress-bar bg-danger text-dark overflow-visible' : batteryLevel < 50 ? 'progress-bar bg-warning overflow-visible' : 'progress-bar bg-success'"
                                    [style.width.%]="batteryLevel"
                                    [attr.aria-valuenow]="batteryLevel"
                                    [textContent]="batteryLevel + '%'"
                                    aria-valuemin="0"
                                    aria-valuemax="100">
                                {{ batteryLevel }}%
                        </div>
                        </div>
                    </div>
                </div>
            }
            @if (online === false) {
                <div class="col-2">
                    <div title="Capteur hors ligne">
                        <div class="text-danger">
                            Hors ligne
                        </div>
                    </div>
                </div>
            }
        </div>
    `,
    standalone: true,
    imports: [
        NgIf
    ]
})
export class PanelHeaderComponent {
    @Input() titre?: string;
    @Input() symbole?: string;
    @Input() idCapteur?: any;
    @Input() batteryLevel?: number;
    @Input() online?: boolean;
    @Input() ajouterDonneeDepuisInterface?: boolean;
    @Input() valeurActuel?: string | null | number;
    @Input() mean?: string | null | number;

    col12: string = "col-12";
    col10: string = "col-10";

    constructor() { }
}