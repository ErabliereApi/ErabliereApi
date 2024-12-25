import { Component, OnInit } from '@angular/core';
import { RapportDegreJourComponent } from './degreejour/rapport-degre-jour.component';
import { NgIf } from '@angular/common';

@Component({
    selector: 'app-reports',
    templateUrl: './rapports.component.html',
    styleUrls: ['./rapports.component.css'],
    imports: [
        RapportDegreJourComponent
    ]
})
export class ReportsComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
        console.log('ReportsComponent onInit');
    }

}