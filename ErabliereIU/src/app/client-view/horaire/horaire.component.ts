import { Component, Input, OnInit } from "@angular/core";
import { FormGroup, FormsModule } from "@angular/forms";
import { Horaire } from "src/model/horaire";
import { EinputComponent } from "src/generic/einput.component";

@Component({
    selector: "horaire",
    templateUrl: "./horaire.component.html",
    imports: [FormsModule, EinputComponent]
})
export class HoraireComponent implements OnInit
{
    @Input() horaire?: Horaire;
    horaireForm: FormGroup;

    constructor() {
        this.horaireForm = new FormGroup({});
    }

    ngOnInit(): void {
        this.horaire ??= new Horaire();
    }

    onSubmit() {
        // Handle form submission logic here
        console.log("Horaire submitted:", this.horaire);
    }
}