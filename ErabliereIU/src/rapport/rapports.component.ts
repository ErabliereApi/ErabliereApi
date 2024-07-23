import { Component, OnInit } from '@angular/core';
import { RapportDegreJourComponent } from './degreejour/rapport-degre-jour.component';

@Component({
    selector: 'app-reports',
    templateUrl: './rapports.component.html',
    styleUrls: ['./rapports.component.css'],
    standalone: true,
    imports: [
        RapportDegreJourComponent
    ]
})
export class ReportsComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
        // Initialize component
    }

}