import { Component, Input, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Capteur } from 'src/model/capteur';

@Component({
    selector: 'modifier-capteur-details',
    template: `
    <form>
        <h4>{{capteur.nom}}</h4>
        <div class="form-group">
            <div class="form-group">
                <label for="externalId">Id de système externe</label>      
                <input type="text" class="form-control" id="externalId" name="externalId" (change)="updateExernalId($event)">
            </div>
            <button type="button" class="btn btn-primary" (click)="saveChanges()">Enregistrer</button>      
        </div>
    </form>
    `,
    standalone: true
})
export class ModifierCapteurDetailsComponent implements OnInit {
    @Input() inputCapteur: Capteur;
    capteur: Capteur;
    editedCapteur: Capteur;

    constructor(private api: ErabliereApi) {
        this.inputCapteur = new Capteur();
        this.capteur = new Capteur();
        this.editedCapteur = new Capteur();
     }

    ngOnInit(): void {
        this.capteur = { ... this.inputCapteur }; // Replace with your own logic to get the Capteur object
        this.editedCapteur = { ...this.capteur }; // Create a copy of the Capteur object for editing
    }

    saveChanges(): void {
        let putPayload = new Capteur();
        putPayload.id = this.capteur.id;
        putPayload.externalId = this.editedCapteur.externalId;
        putPayload.idErabliere = this.capteur.idErabliere;
        putPayload.ajouterDonneeDepuisInterface = this.capteur.ajouterDonneeDepuisInterface;
        this.api.putCapteur(putPayload); // Replace with your own logic to update the Capteur object
    }

    updateExernalId($event: Event) {
        let inputElement = $event.target as HTMLInputElement;
        this.editedCapteur.externalId = inputElement.value;
    }
}