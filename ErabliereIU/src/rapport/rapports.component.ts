import { Component } from '@angular/core';
import { RapportDegreJourComponent } from './degreejour/rapport-degre-jour.component';
import { RapportMoyenneComponent } from './moyenne/rapport-moyenne.component';

@Component({
    selector: 'app-reports',
    templateUrl: './rapports.component.html',
    styleUrls: ['./rapports.component.css'],
    imports: [
        RapportDegreJourComponent,
        RapportMoyenneComponent
    ]
})
export class ReportsComponent {

    typeRapport: string = 'degreJour';

    constructor() { }

    typeRapportChanged($event: Event) {
        console.log("Type rapport changed", $event);
        this.typeRapport = ($event.target as HTMLSelectElement).value;
        console.log("Type rapport", this.typeRapport);
    }

}