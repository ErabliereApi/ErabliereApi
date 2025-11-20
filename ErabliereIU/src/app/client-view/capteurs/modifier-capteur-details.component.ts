import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import {
    ReactiveFormsModule,
    FormsModule,
    UntypedFormBuilder,
    UntypedFormGroup
} from "@angular/forms";
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { EButtonComponent } from 'src/generic/ebutton.component';
import { EinputComponent } from 'src/generic/einput.component';
import { InputErrorComponent } from 'src/generic/input-error.component';
import { Capteur } from 'src/model/capteur';
import { ErrorObj } from 'src/model/errorObj';

@Component({
    selector: 'modifier-capteur-details',
    template: `
    <form [formGroup]="editCapteurForm" class="row mb-3" novalidate>
        <!-- Section des erreurs générales -->
         @if (generalError) {
        <span class="text-danger">
            {{ generalError }}
        </span>
         }

        <div class="col-md-6 mb-3">
            <einput
                id="nom"
                name="nom"
                placeholder="Nom du capteur"
                [formGroup]="editCapteurForm"
                [maxlength]="200"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <div class="col-md-6 mb-3">
            <einput
                id="symbole"
                name="symbole"
                arialabel="Symbole"
                placeholder="Symbole du capteur"
                [formGroup]="editCapteurForm"
                [maxlength]="7"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <div class="col-md-6 mb-3">
            <einput
                id="indiceOrdre"
                name="indiceOrdre"
                arialabel="Indice d'ordre"
                placeholder="Indice d'ordre du capteur"
                type="number"
                [formGroup]="editCapteurForm"
                [maxlength]="7"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <div class="col-md-6 mb-3">
            <einput
                id="taille"
                name="taille"
                arialabel="Taille"
                placeholder="Taille du capteur (en colonnes)"
                type="number"
                [formGroup]="editCapteurForm"
                [maxlength]="7"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <div class="col-md-6">
            <div class="form-check form-switch mb-3">
                <label for="afficherCapteurDashboard" class="form-label">Afficher dans les graphiques:</label>
                <input
                    type="checkbox"
                    class="form-check-input"
                    id="afficherCapteurDashboard"
                    formControlName="afficherCapteurDashboard"
                />
                <input-error [errorObj]="errorObj" controlName="afficherCapteurDashboard"></input-error>
            </div>

            <div class="form-check form-switch mb-3">
                <label for="ajouterDonneeDepuisInterface" class="form-check-label">Saisie manuelle</label>
                <input
                    type="checkbox"
                    class="form-check-input"
                    id="ajouterDonneeDepuisInterface"
                    formControlName="ajouterDonneeDepuisInterface"
                />
                <input-error [errorObj]="errorObj" controlName="ajouterDonneeDepuisInterface"></input-error>
            </div>
        </div>

        <div class="col-md-6 mb-3">
            <einput
                id="externalId"
                name="externalId"
                placeholder="Id de système externe"
                [formGroup]="editCapteurForm"
                [maxlength]="100"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <div class="col-md-6 mb-3">
            <einput
                id="type"
                name="type"
                placeholder="Type"
                [formGroup]="editCapteurForm"
                [maxlength]="100"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <div class="col-md-6 mb-3">
            <einput
                id="displayType"
                name="displayType"
                placeholder="Type d'affichage"
                [formGroup]="editCapteurForm"
                [maxlength]="100"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <div class="col-md-12 mb-3">
            <einput
                id="displayTop"
                name="displayTop"
                placeholder="Quantité par défaut"
                type="number"
                [formGroup]="editCapteurForm"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <div class="col-md-6 mb-3">
            <einput
                id="displayMin"
                name="displayMin"
                placeholder="Échel y minimum"
                type="number"
                [formGroup]="editCapteurForm"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <div class="col-md-6 mb-3">
            <einput
                id="displayMax"
                name="displayMax"
                placeholder="Échel y maximum"
                type="number"
                [formGroup]="editCapteurForm"
                [errorObj]="errorObj"
            ></einput>
        </div>

        <!-- Section des erreurs générales -->
         @if (generalError) {
        <span class="text-danger">
            {{ generalError }}
        </span>
         }

        <div class="col-12 d-flex gap-2">
            <ebutton type="primary" (clicked)="saveChanges()" [inProgress]="editInProgress">Enregistrer</ebutton>
            <ebutton type="secondary" (clicked)="cancel()">Annuler</ebutton>
        </div>
    </form>
    `,
    standalone: true,
    imports: [ReactiveFormsModule, FormsModule, EinputComponent, InputErrorComponent, EButtonComponent]
})
export class ModifierCapteurDetailsComponent implements OnInit {
    @Input() inputCapteur: Capteur;
    capteur: Capteur;
    @Output() needToUpdate = new EventEmitter();
    @Output() closeForm = new EventEmitter();
    errorObj: ErrorObj | null = null;
    generalError?: string;
    editCapteurForm: UntypedFormGroup;
    editInProgress: boolean = false;

    constructor(private readonly api: ErabliereApi, private readonly formBuilder: UntypedFormBuilder) {
        this.inputCapteur = new Capteur();
        this.capteur = new Capteur();
        this.editCapteurForm = this.formBuilder.group({
            indiceOrdre: [0],
            nom: [''],
            symbole: [''],
            afficherCapteurDashboard: [true],
            ajouterDonneeDepuisInterface: [false],
            type: [''],
            externalId: [''],
            displayType: [''],
            displayTop: [null],
            displayMin: [null],
            displayMax: [null],
            taille: [6]
        });
    }

    ngOnInit(): void {
        this.capteur = { ...this.inputCapteur };
        this.editCapteurForm.patchValue({
            indiceOrdre: this.capteur.indiceOrdre,
            nom: this.capteur.nom,
            symbole: this.capteur.symbole,
            afficherCapteurDashboard: this.capteur.afficherCapteurDashboard,
            ajouterDonneeDepuisInterface: this.capteur.ajouterDonneeDepuisInterface,
            type: this.capteur.type,
            externalId: this.capteur.externalId,
            displayType: this.capteur.displayType,
            displayTop: this.capteur.displayTop,
            displayMin: this.capteur.displayMin,
            displayMax: this.capteur.displayMax,
            taille: this.capteur.taille as any == "" ? undefined : this.capteur.taille
        });
    }

    saveChanges(): void {
        if (this.editCapteurForm.invalid) {
            this.editCapteurForm.markAllAsTouched();
            return;
        }
        this.editInProgress = true;
        const putPayload = {
            ...this.capteur,
            ...this.editCapteurForm.value
        } as Capteur;
        this.generalError = undefined;
        this.errorObj = null;
        this.api.putCapteur(putPayload).then(() => {
            this.needToUpdate.emit();
        }).catch((error: any) => {
            console.error(error);
            if (error.status == 400) {
                this.errorObj = error;
                this.generalError = error.error.title;
            } else {
                this.generalError = "Une erreur est survenue lors de la modification du capteur. Veuillez réessayer plus tard.";
            }
        }).finally(() => {
            this.editInProgress = false;
        });
    }

    cancel(): void {
        this.closeForm.emit();
    }
}