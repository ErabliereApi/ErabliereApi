import { NgFor } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Capteur } from 'src/model/capteur';
import { PostDegresJoursRepportRequest, ResponseRapportDegreeJours } from 'src/model/postDegresJoursRepportRequest';
import { EinputComponent } from "../../formsComponents/einput.component";
import { InputErrorComponent } from "../../formsComponents/input-error.component";
import { FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
    selector: 'app-rapport-degre-jour',
    templateUrl: './rapport-degre-jour.component.html',
    styleUrls: ['./rapport-degre-jour.component.css'],
    standalone: true,
    imports: [
    NgFor,
    FormsModule,
    ReactiveFormsModule,
    EinputComponent,
    InputErrorComponent
]
})
export class RapportDegreJourComponent implements OnInit {
    form: PostDegresJoursRepportRequest = new PostDegresJoursRepportRequest();
    capteurs: Capteur[] = [];
    degresJours?: ResponseRapportDegreeJours;
    idErabliere: any;
    errorObj: any;
    myGroup: FormGroup;
    generalError?: string;

    constructor(private api: ErabliereApi, private route: ActivatedRoute) {
        this.myGroup = new FormGroup({
            
        });
    }

    ngOnInit(): void {
        console.log('RapportDegreJourComponent onInit');
        this.route.params.subscribe(params => {
            this.idErabliere = params['idErabliereSelectionee'];
            this.api.getCapteurs(this.idErabliere).then((capteurs: Capteur[]) => {
                this.capteurs = capteurs;
            });
        });
    }

    async onSubmit() {
        console.log('RapportDegreJourComponent onSubmit');
        const rapport = await this.api.getDegresJours(this.idErabliere, this.form);
        this.degresJours = rapport;
    }

    onKeyPress(event: KeyboardEvent) {
        console.log('RapportDegreJourComponent onKeyPress', event);
    }
}