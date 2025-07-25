
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Capteur } from 'src/model/capteur';
import { PostDegresJoursRepportRequest, ResponseRapportDegreeJours } from 'src/model/postDegresJoursRepportRequest';
import { InputErrorComponent } from "src/generic/input-error.component";
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
    selector: 'app-rapport-degre-jour',
    templateUrl: './rapport-degre-jour.component.html',
    styleUrls: ['./rapport-degre-jour.component.css'],
    imports: [
        FormsModule,
        ReactiveFormsModule,
        InputErrorComponent
    ]
})
export class RapportDegreJourComponent implements OnInit {
    form: PostDegresJoursRepportRequest = new PostDegresJoursRepportRequest();
    capteurs: Capteur[] = [];
    idErabliere: any;
    errorObj: any;
    generalError?: string;
    @Output() notifierAffichageRapport = new EventEmitter<ResponseRapportDegreeJours>();

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
            console.log('RapportDegreJourComponent notifierAffichageRapport.emit', rapport);
            this.notifierAffichageRapport.emit(rapport);
            this.errorObj = null;
            this.generalError = undefined;
        }
        catch (error: any) {
            console.error('RapportDegreJourComponent error', error);
            if (error.status >= 500) {
                this.generalError = "Erreur interne du serveur (" + error.name + "). Veuillez réessayer plus tard.";
                this.errorObj = null;
            }
            else {
                this.errorObj = error;
                if (error.error.errors) {
                    this.generalError = error.error.title;
                }
                else {
                    this.generalError = error.error;
                }
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