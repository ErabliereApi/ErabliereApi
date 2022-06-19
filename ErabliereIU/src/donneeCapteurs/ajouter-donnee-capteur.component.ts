import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";
import { UntypedFormBuilder, UntypedFormGroup } from "@angular/forms";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { PostDonneeCapteur } from "src/model/donneeCapteur";

@Component({
    selector: 'ajouter-donnee-capteur',
    template: `
        <button *ngIf="!display" class="btn btn-primary" (click)="afficherForm()">Ajouter</button>
        <div *ngIf="display" class="border-top">
            <h3>Ajouter une donnée</h3>
            <form [formGroup]="donneeCapteurForm">
                <div class="form-group">
                    <span *ngIf="generalErrorMessage" class="text-danger">{{ generalErrorMessage }}</span>
                </div>
                <div class="form-group">
                    <label for="valeur">Valeur</label>
                    <input type="number" class="form-control" id="valeur" name="valeur" placeholder="Valeur" formControlName="valeur">
                    <div *ngIf="this.donneeCapteurForm.controls['valeur'].errors">
                        <span class="text-danger">{{ this.donneeCapteurForm.controls['valeur'].errors.message }}</span>
                    </div>
                </div>
                <div class="form-group">
                    <label for="date">Date</label>
                    <input type="datetime-local" class="form-control" id="date" name="date" placeholder="Date" formControlName="date">
                    <div *ngIf="this.donneeCapteurForm.controls['date'].errors">
                        <span class="text-danger">{{ this.donneeCapteurForm.controls['date'].errors.message }}</span>
                    </div>
                </div>
                <button type="button" class="btn btn-primary" (click)="ajouterDonnee()">Ajouter</button>
                <button type="button" class="btn btn-secondary" (click)="annuler()">Annuler</button>
            </form>
        </div>
    `,
    styles: [`
        .border-top {
            border-top: 1px solid #ccc;
        }
    `]
})
export class AjouterDonneeCapteurComponent implements OnInit {
    @Input() idCapteur: any;
    donneeCapteurForm: UntypedFormGroup;
    display: boolean = false;
    generalErrorMessage: string | null = null;

    @Output() needToUpdate = new EventEmitter();

    constructor(private api: ErabliereApi, private fb: UntypedFormBuilder) {
        this.donneeCapteurForm = this.fb.group({});
     }

    ngOnInit(): void {
        this.donneeCapteurForm = this.fb.group({
            valeur: '',
            date: ''
        });
    }

    onSubmit() {

    }

    ajouterDonnee() {
        this.generalErrorMessage = null;
        var donneeCapteur = new PostDonneeCapteur();
        var validationError = false;
        try {
            donneeCapteur.v = parseInt(this.donneeCapteurForm.controls['valeur'].value);
        } catch (error) {
            this.donneeCapteurForm.controls['valeur'].setErrors({
                'incorrect': true,
                'message': 'Impossible de convertir la valeur en entier'
            })
            validationError = true;
        }
        try {
            donneeCapteur.d = new Date(this.donneeCapteurForm.controls['date'].value).toISOString();
        } catch (error) {
            this.donneeCapteurForm.controls['date'].setErrors({
                'incorrect': true,
                'message': "Impossible d'interpreter la date"
            })
            validationError = true;
        }
        
        if (validationError == false) {
            donneeCapteur.idCapteur = this.idCapteur;

            this.api.postDonneeCapteur(this.idCapteur, donneeCapteur).then(() => {
                this.donneeCapteurForm.reset();
                this.needToUpdate.emit();
            }).catch(e => {
                if (e.status == 400) {
                    if (e.error.errors.V != undefined) {
                        this.donneeCapteurForm.controls['valeur'].setErrors({
                            'incorrect': true,
                            'message': e.error.errors.V.join(', ')
                        })
                    }
                    if (e.error.errors['$.D'] != undefined) {
                        this.donneeCapteurForm.controls['date'].setErrors({
                            'incorrect': true,
                            'message': e.error.errors['$.D'].join(', ')
                        })
                    }
                }
                else {
                    this.generalErrorMessage = "Une erreur est survenue lors de l'ajout de la donnée";
                }
            });
        }
    }

    annuler() {
        this.donneeCapteurForm.reset();
        this.display = false;
    }

    afficherForm() {
        this.display = true;
    }
}