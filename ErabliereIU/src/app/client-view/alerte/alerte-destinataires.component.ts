import { Component, Input } from '@angular/core';

@Component({
    selector: 'alerte-destinataires',
    template: `
        @if (destinataires.length > 0) {
        <div>
            @for (destinataire of destinatairesList; track $index) {
            <div>{{destinataire}}</div>
            }
        </div>
        } @else {
        <div>
            Aucun destinataire.
        </div>
        }
    `
})
export class AlerteDestinatairesComponent {
    @Input() destinataires: string = '';

    get destinatairesList(): string[] {
        return this.destinataires
            .split(';')
            .map(d => d.trim())
            .filter(d => d.length > 0);
    }
}