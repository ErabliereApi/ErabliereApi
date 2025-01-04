import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Capteur } from 'src/model/capteur';

@Component({
    selector: 'modifier-capteur-details',
    template: `
    <form>
        <h4>{{capteur.nom}}</h4>
        <div class="form-group">
            <div class="form-group">
                <label for="type">Type</label>
                <input type="text" class="form-control" id="type" name="type" [value]="editedCapteur.type" (change)="updateType($event)">
            </div>
            <div class="form-group">
                <label for="externalId">Id de système externe</label>      
                <input type="text" class="form-control" id="externalId" name="externalId" [value]="editedCapteur.externalId" (change)="updateExernalId($event)">
            </div>
            <div class="form-group">
                <label for="displayType">Type d'affichage</label>
                <input type="text" class="form-control" id="displayType" name="displayType" [value]="editedCapteur.displayType" (change)="updateDisplayType($event)">
            </div>
            <div class="form-group">
                <label for="displayTop">Quantité par défaut</label>
                <input type="number" class="form-control" id="displayTop" name="displayTop" [value]="editedCapteur.displayTop" (change)="updateDisplayTop($event)">
            </div>
            <div class="form-group">
                <label for="displayMin">Échel y minimum</label>
                <input type="number" class="form-control" id="displayMin" name="displayMin" [value]="editedCapteur.displayMin" (change)="updateDisplayMin($event)">
            </div>
            <div class="form-group">
                <label for="displayMax">Échel y maximum</label>
                <input type="number" class="form-control" id="displayMax" name="displayMax" [value]="editedCapteur.displayMax" (change)="updateDisplayMax($event)">
            </div>
            <button type="button" class="btn btn-primary" (click)="saveChanges()">Enregistrer</button>
            <button type="button" class="btn btn-secondary" (click)="cancel()">Annuler</button> 
        </div>
    </form>
    `,
    standalone: true
})
export class ModifierCapteurDetailsComponent implements OnInit {
    @Input() inputCapteur: Capteur;
    capteur: Capteur;
    editedCapteur: Capteur;
    @Output() needToUpdate = new EventEmitter();
    @Output() closeForm = new EventEmitter();

    constructor(private readonly api: ErabliereApi) {
        this.inputCapteur = new Capteur();
        this.capteur = new Capteur();
        this.editedCapteur = new Capteur();
     }

    ngOnInit(): void {
        this.capteur = { ... this.inputCapteur };
        this.editedCapteur = { ...this.capteur };
    }

    saveChanges(): void {
        let putPayload = new Capteur();
        putPayload.id = this.capteur.id;
        putPayload.type = this.editedCapteur.type;
        putPayload.externalId = this.editedCapteur.externalId;
        putPayload.idErabliere = this.capteur.idErabliere;
        putPayload.displayType = this.editedCapteur.displayType;
        putPayload.displayTop = this.editedCapteur.displayTop;
        putPayload.ajouterDonneeDepuisInterface = this.capteur.ajouterDonneeDepuisInterface;
        putPayload.displayMin = this.editedCapteur.displayMin;
        putPayload.displayMax = this.editedCapteur.displayMax;
        this.api.putCapteur(putPayload);
        this.needToUpdate.emit();
    }

    cancel(): void {
        this.closeForm.emit();
    }

    updateExernalId($event: Event) {
        let inputElement = $event.target as HTMLInputElement;
        this.editedCapteur.externalId = inputElement.value;
    }

    updateType($event: Event) {
        let inputElement = $event.target as HTMLInputElement;
        this.editedCapteur.type = inputElement.value;
    }

    updateDisplayType($event: Event) {
        let inputElement = $event.target as HTMLInputElement;
        this.editedCapteur.displayType = inputElement.value;
    }

    updateDisplayTop($event: Event) {
        let inputElement = $event.target as HTMLInputElement;
        this.editedCapteur.displayTop = parseInt(inputElement.value);
    }

    updateDisplayMin($event: Event) {
        let inputElement = $event.target as HTMLInputElement;
        this.editedCapteur.displayMin = parseInt(inputElement.value);
    }

    updateDisplayMax($event: Event) {
        let inputElement = $event.target as HTMLInputElement;
        this.editedCapteur.displayMax = parseInt(inputElement.value);
    }
}