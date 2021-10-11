import { Component, Input, OnInit } from "@angular/core";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Alerte } from "src/model/alerte";
import { FormGroup, FormBuilder } from "@angular/forms";
import { Subject } from "rxjs";

@Component({
    selector: 'modifier-alerte-modal',
    templateUrl: 'modifier-alerte.component.html'
})
export class ModifierAlerteComponent implements OnInit {
    constructor(private _api: ErabliereApi, private fb: FormBuilder) 
    {
        this.alerteForm = this.fb.group({
            id: '',
            destinataire: '',
            temperatureMin: '',
            temperatureMax: '',
            vacciumMin: '',
            vacciumMax: '',
            niveauBassinMin: '',
            niveauBassinMax: ''
        });
    }
    
    ngOnInit(): void {
        let alerte = this.alerte;

        if (alerte != undefined) {
            this.alerteForm.setValue({
                id: alerte.id,
                destinataire: alerte.envoyerA,
                temperatureMin: alerte.temperatureThresholdHight,
                temperatureMax: alerte.temperatureThresholdLow,
                vacciumMin: alerte.vacciumThresholdHight,
                vacciumMax: alerte.vacciumThresholdLow,
                niveauBassinMin: alerte.niveauBassinThresholdHight,
                niveauBassinMax: alerte.niveauBassinThresholdLow
            });
        }
    }

    @Input() alerte?:Alerte;

    @Input() idErabliereSelectionee:any

    @Input() displayEditFormSubject = new Subject<Boolean>();

    @Input() alerteEditFormSubject = new Subject<Alerte>();

    alerteForm: FormGroup;

    onSubmit() {

    }

    onButtonAnnuleClick() {
        this.displayEditFormSubject.next(false);
    }

    onButtonModifierClick() {
        let alerte = new Alerte();

        alerte.id = this.alerteForm.controls['id'].value;
        alerte.idErabliere = this.idErabliereSelectionee;
        alerte.envoyerA = this.alerteForm.controls['destinataire'].value;
        alerte.temperatureThresholdLow = this.alerteForm.controls['temperatureMax'].value;
        alerte.temperatureThresholdHight = this.alerteForm.controls['temperatureMin'].value;
        alerte.vacciumThresholdLow = this.alerteForm.controls['vacciumMax'].value;
        alerte.vacciumThresholdHight = this.alerteForm.controls['vacciumMin'].value;
        alerte.niveauBassinThresholdLow = this.alerteForm.controls['niveauBassinMax'].value;
        alerte.niveauBassinThresholdHight = this.alerteForm.controls['niveauBassinMin'].value;
        this._api.putAlerte(this.idErabliereSelectionee, alerte)
                 .then(r => {
                     this.displayEditFormSubject.next(false);
                     this.alerteEditFormSubject.next(r);
                 });
    }
}