import { Component, OnInit } from '@angular/core';
import { RapportDegreJourComponent } from './degreejour/rapport-degre-jour.component';
import { NgIf } from '@angular/common';

@Component({
    selector: 'app-reports',
    templateUrl: './rapports.component.html',
    styleUrls: ['./rapports.component.css'],
    standalone: true,
    imports: [
        RapportDegreJourComponent,
        NgIf
    ]
})
export class ReportsComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
        console.log('ReportsComponent onInit');
    }

}