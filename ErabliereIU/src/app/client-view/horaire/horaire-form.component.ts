import { Component, Input, OnChanges, OnInit, SimpleChanges } from "@angular/core";
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { Horaire } from "src/model/horaire";
import { EinputComponent } from "src/generic/einput.component";
import { EButtonComponent } from "src/generic/ebutton.component";
import { ErabliereApi } from "src/core/erabliereapi.service";

@Component({
    selector: "horaire-form",
    templateUrl: "./horaire-form.component.html",
    imports: [FormsModule, EinputComponent, ReactiveFormsModule, EButtonComponent]
})
export class HoraireComponent implements OnInit, OnChanges {
    @Input() erabliereId?: any;
    @Input() horaire?: Horaire;
    horaireForm: FormGroup;
    modifierHorairePutCallInProgress = false;

    constructor(private readonly api: ErabliereApi) {
        this.horaireForm = new FormGroup({
            lundi: new FormControl(this.horaire?.lundi || '', [Validators.pattern('^\\d{2}:\\d{2}-\\d{2}:\\d{2}$'), Validators.maxLength(12)]),
            mardi: new FormControl(this.horaire?.mardi || '', [Validators.pattern('^\\d{2}:\\d{2}-\\d{2}:\\d{2}$'), Validators.maxLength(12)]),
            mercredi: new FormControl(this.horaire?.mercredi || '', [Validators.pattern('^\\d{2}:\\d{2}-\\d{2}:\\d{2}$'), Validators.maxLength(12)]),
            jeudi: new FormControl(this.horaire?.jeudi || '', [Validators.pattern('^\\d{2}:\\d{2}-\\d{2}:\\d{2}$'), Validators.maxLength(12)]),
            vendredi: new FormControl(this.horaire?.vendredi || '', [Validators.pattern('^\\d{2}:\\d{2}-\\d{2}:\\d{2}$'), Validators.maxLength(12)]),
            samedi: new FormControl(this.horaire?.samedi || '', [Validators.pattern('^\\d{2}:\\d{2}-\\d{2}:\\d{2}$'), Validators.maxLength(12)]),
            dimanche: new FormControl(this.horaire?.dimanche || '', [Validators.pattern('^\\d{2}:\\d{2}-\\d{2}:\\d{2}$'), Validators.maxLength(12)]),
        });
    }

    ngOnInit(): void {
        this.horaire ??= new Horaire();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['horaire']?.currentValue) {
            this.horaire = changes['horaire'].currentValue;
            if (this.horaire) {
                this.horaireForm.patchValue({
                    lundi: this.horaire.lundi,
                    mardi: this.horaire.mardi,
                    mercredi: this.horaire.mercredi,
                    jeudi: this.horaire.jeudi,
                    vendredi: this.horaire.vendredi,
                    samedi: this.horaire.samedi,
                    dimanche: this.horaire.dimanche
                });
            }
        }
    }

    putHoraire() {
        this.horaire = { ...this.horaireForm.value, idErabliere: this.erabliereId };
        if (!this.horaire) {
            return Promise.reject(new Error("Horaire non initialisé"));
        }
        this.modifierHorairePutCallInProgress = true;
        return this.api.putHoraire(this.erabliereId, this.horaire).then(() => {
            // Handle successful form submission
        }).catch(error => {
            console.error("Error submitting horaire:", error);
        }).finally(() => {
            this.modifierHorairePutCallInProgress = false;
        });
    }
}