import { Component, Input, OnInit } from "@angular/core";
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { Horaire } from "src/model/horaire";
import { EinputComponent } from "src/generic/einput.component";

@Component({
    selector: "horaire-form",
    templateUrl: "./horaire-form.component.html",
    imports: [FormsModule, EinputComponent, ReactiveFormsModule]
})
export class HoraireComponent implements OnInit
{
    @Input() horaire?: Horaire;
    horaireForm: FormGroup;

    constructor() {
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

    onSubmit() {
        // Handle form submission logic here
        console.log("Horaire submitted:", this.horaire);
    }
}