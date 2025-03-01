import { NgFor } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Capteur } from 'src/model/capteur';
import { PostDegresJoursRepportRequest, ResponseRapportDegreeJours } from 'src/model/postDegresJoursRepportRequest';
import { InputErrorComponent } from "../../formsComponents/input-error.component";
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
    selector: 'app-rapport-degre-jour',
    templateUrl: './rapport-degre-jour.component.html',
    styleUrls: ['./rapport-degre-jour.component.css'],
    imports: [
        NgFor,
        FormsModule,
        ReactiveFormsModule,
        InputErrorComponent
    ]
})
export class RapportDegreJourComponent implements OnInit {
    form: PostDegresJoursRepportRequest = new PostDegresJoursRepportRequest();
    capteurs: Capteur[] = [];
    degresJours?: ResponseRapportDegreeJours;
    idErabliere: any;
    errorObj: any;
    generalError?: string;

    constructor(private readonly api: ErabliereApi, private readonly route: ActivatedRoute) {

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

    async onGenererRapport(save?: boolean) {
        console.log('RapportDegreJourComponent onSubmit');
        try {
            this.form.idErabliere = this.idErabliere;
            const rapport = await this.api.postDegresJours(this.idErabliere, this.form, save);
            this.degresJours = rapport;
        }
        catch (error: any) {
            console.error('RapportDegreJourComponent error', error);
            this.degresJours = undefined;
            this.errorObj = error;
            if (error.error.errors) {
                this.generalError = error.error.title;
            }
            else {
                this.generalError = error.error;
            }
        }
    }

    onKeyPress(event: KeyboardEvent) {
        console.log('RapportDegreJourComponent onKeyPress', event);
    }

    idCapteurChanged($event: Event) {
        console.log('RapportDegreJourComponent idCapteurChanged', $event);

        this.form.idCapteur = ($event.target as HTMLSelectElement).value;
    }
}