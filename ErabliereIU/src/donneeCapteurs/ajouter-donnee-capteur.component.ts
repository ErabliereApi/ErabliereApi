import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";
import { UntypedFormBuilder, UntypedFormGroup, ReactiveFormsModule } from "@angular/forms";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { PostDonneeCapteur } from "src/model/donneeCapteur";
import { EinputComponent } from "../generic/einput.component";


@Component({
    selector: 'ajouter-donnee-capteur',
    template: `
        @if (!display) {
          <button class="btn btn-primary" (click)="afficherForm()">Ajouter</button>
        }
        @if (display) {
          <div class="ms-3">
            <h3>Ajouter une donnée</h3>
            <form [formGroup]="donneeCapteurForm">
              <div class="form-group">
                @if (generalErrorMessage) {
                  <span class="text-danger">{{ generalErrorMessage }}</span>
                }
              </div>
              <div class="form-group">
                <einput type="number" id="valeur" name="valeur" [formGroup]="donneeCapteurForm" [symbole]="symbole" />
                @if (this.donneeCapteurForm.controls['valeur'].errors) {
                  <div>
                    <span class="text-danger">{{ this.donneeCapteurForm.controls['valeur'].errors.message }}</span>
                  </div>
                }
              </div>
              <div class="form-group mb-2">
                <label for="date" class="form-label">Date</label>
                <input type="datetime-local" class="form-control" id="date" name="date" placeholder="Date" formControlName="date">
                @if (this.donneeCapteurForm.controls['date'].errors) {
                  <div>
                    <span class="text-danger">{{ this.donneeCapteurForm.controls['date'].errors.message }}</span>
                  </div>
                }
              </div>
              <button type="button" class="btn btn-primary me-2" (click)="ajouterDonnee()">Ajouter</button>
              <button type="button" class="btn btn-secondary" (click)="annuler()">Annuler</button>
            </form>
          </div>
        }
        `,
    styles: [`
        .border-top {
            border-top: 1px solid #ccc;
        }
    `],
    imports: [ReactiveFormsModule, EinputComponent]
})
export class AjouterDonneeCapteurComponent implements OnInit {
    @Input() idCapteur: any;
    @Input() symbole?: string
    @Input() noAutomaticDate?: boolean;
    donneeCapteurForm: UntypedFormGroup;
    display: boolean = false;
    generalErrorMessage: string | null = null;

    @Output() needToUpdate = new EventEmitter();

    constructor(private readonly api: ErabliereApi, private readonly fb: UntypedFormBuilder) {
        this.donneeCapteurForm = this.fb.group({});
    }

    ngOnInit(): void {
        let nowLocal = '';
        if (!this.noAutomaticDate) {
            const offset = new Date().getTimezoneOffset();
            const offsetMs = offset * 60000;
            const nowMs = new Date().getTime();
            nowLocal = new Date(nowMs - offsetMs).toISOString().slice(0, 16);
        }
        this.donneeCapteurForm = this.fb.group({
            valeur: '',
            date: nowLocal
        });
    }

    onSubmit() {

    }

    ajouterDonnee() {
        this.generalErrorMessage = null;
        let donneeCapteur = new PostDonneeCapteur();
        let validationError = false;
        try {
            donneeCapteur.v = parseFloat(this.donneeCapteurForm.controls['valeur'].value);
        } catch (error) {
            console.error("Erreur lors de la conversion de la valeur en nombre décimal", error);
            this.donneeCapteurForm.controls['valeur'].setErrors({
                'incorrect': true,
                'message': 'Impossible de convertir la valeur en nombre décimal'
            })
            validationError = true;
        }
        try {
            donneeCapteur.d = new Date(this.donneeCapteurForm.controls['date'].value).toISOString();
        } catch (error) {
            console.error("Erreur lors de la conversion de la date", error);
            this.donneeCapteurForm.controls['date'].setErrors({
                'incorrect': true,
                'message': "Impossible d'interpreter la date"
            })
            validationError = true;
        }

        if (!validationError) {
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
