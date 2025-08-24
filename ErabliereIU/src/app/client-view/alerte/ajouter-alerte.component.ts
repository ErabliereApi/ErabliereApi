import { Component, Input, OnInit } from "@angular/core";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Alerte } from "src/model/alerte";
import { UntypedFormGroup, UntypedFormBuilder, UntypedFormControl, ReactiveFormsModule } from "@angular/forms";
import { AlerteCapteur } from "src/model/alerteCapteur";
import { Capteur } from "src/model/capteur";
import { convertTenthToNormale } from "src/core/calculator.service";
import { EinputComponent } from "src/generic/einput.component";

import { Erabliere } from "src/model/erabliere";
import { ActivatedRoute } from "@angular/router";
import { EButtonComponent } from "src/generic/ebutton.component";

@Component({
    selector: 'ajouter-alerte-modal',
    templateUrl: 'ajouter-alerte.component.html',
    imports: [ReactiveFormsModule, EinputComponent, EButtonComponent]
})
export class AjouterAlerteComponent implements OnInit {
    typeAlerteSelectListForm: UntypedFormGroup
    alerte:Alerte
    alerteCapteur:AlerteCapteur
    display:boolean
    generalError?: string
    onButtonCreerClickInProgress: boolean = false;
    onButtonCreerAlerteCapteurClickInProgress: boolean = false;

    constructor(
        private readonly _api: ErabliereApi, 
        private readonly fb: UntypedFormBuilder,
        private readonly route: ActivatedRoute) {
        this.alerte = new Alerte();
        this.alerteCapteur = new AlerteCapteur();
        this.typeAlerteSelectListForm = new UntypedFormGroup({
            state: new UntypedFormControl(1)
        });
        this.display = false;
        this.alerteForm = this.fb.group({});
        this.alerteCapteurForm = this.fb.group({});
    }
    
    ngOnInit() {
        this.route.params.subscribe(params => {
            this.idErabliereSelectionee = params['idErabliereSelectionee'];
            this.updateState();
        });
    }

    async updateState() {
        if (this.erabliere == undefined || this.erabliere.id != this.idErabliereSelectionee) {
            const r = await this._api.getErabliere(this.idErabliereSelectionee, true);
            this.erabliere = r;
        }
        this.initializeForms();
        console.log(this.erabliere);
        if (!this.erabliere?.afficherTrioDonnees) {
            this.typeAlerteSelectListForm.controls['state'].setValue(2);
            this.typeAlerte = 2;
            this.onChangeAlerteType({ target: { value: 2 } });
        }
        else {
            this.typeAlerte = 1;
        }
    }

    initializeForms() {
        this.alerteForm = this.fb.group({
            nom: '',
            destinataireCourriel: '',
            destinataireSMS: '',
            temperatureMin: '',
            temperatureMax: '',
            vacciumMin: '',
            vacciumMax: '',
            niveauBassinMin: '',
            niveauBassinMax: ''
        });
        this.alerteCapteurForm = this.fb.group({
            nom: '',
            destinataireCourriel: '',
            destinataireSMS: '',
            min: '',
            max: '',
            idCapteur: ''
        });
    }
    
    capteurSymbole(): string {
        const formIdCapteur = this.alerteCapteurForm.controls['idCapteur'].value
        const capteur = this.capteurs.find(c => c.id == formIdCapteur);
        return capteur?.symbole ?? "";
    }

    @Input() alertes?: Array<Alerte>;
    @Input() alertesCapteur?: Array<AlerteCapteur>;

    @Input() idErabliereSelectionee:any
    @Input() erabliere?: Erabliere
    capteurs: Array<Capteur> = [];

    alerteForm: UntypedFormGroup;
    alerteCapteurForm: UntypedFormGroup;

    typeAlerte:number = 1;

    onButtonAjouterClick() {
        this.display = true;
    }

    onButtonAnnuleClick() {
        this.display = false;
    }

    onChangeAlerteType(event:any) {
        this.typeAlerte = event.target.value;

        if (this.typeAlerte == 2) {
            this._api.getCapteurs(this.idErabliereSelectionee).then(r => {
                this.capteurs = r;
            });
        }
    }

    onButtonCreerClick() {
        if (this.alerte != undefined) {
            this.generalError = "";
            this.alerte.idErabliere = this.idErabliereSelectionee;
            this.alerte.nom = this.alerteForm.controls['nom'].value;
            this.alerte.envoyerA = this.alerteForm.controls['destinataireCourriel'].value;
            this.alerte.texterA = this.alerteForm.controls['destinataireSMS'].value;
            this.alerte.temperatureThresholdLow = convertTenthToNormale(this.alerteForm.controls['temperatureMax'].value)
            this.alerte.temperatureThresholdHight = convertTenthToNormale(this.alerteForm.controls['temperatureMin'].value)
            this.alerte.vacciumThresholdLow = convertTenthToNormale(this.alerteForm.controls['vacciumMax'].value)
            this.alerte.vacciumThresholdHight = convertTenthToNormale(this.alerteForm.controls['vacciumMin'].value)
            this.alerte.niveauBassinThresholdLow = this.alerteForm.controls['niveauBassinMax'].value;
            this.alerte.niveauBassinThresholdHight = this.alerteForm.controls['niveauBassinMin'].value;
            this.onButtonCreerClickInProgress = true;
            this._api.postAlerte(this.idErabliereSelectionee, this.alerte)
                     .then(r => {
                         this.display = false;
                         r.emails = r?.envoyerA?.split(";");
                         r.numeros = r?.texterA?.split(";");
                         this.alertes?.push(r);
                     })
                     .catch(e => {
                        this.generalError = "Erreur lors de la modification de l'alerte";
                    })
                    .finally(() => {
                        this.onButtonCreerClickInProgress = false;
                    });
        }
        else {
            console.log("this.alerte is undefined");
            this.generalError = "L'alerte n'est pas défini";
        }
    }

    onButtonCreerAlerteCapteurClick() {
        if (this.alerteCapteur != undefined) {
            this.generalError = "";
            this.alerteCapteur.idCapteur = this.alerteCapteurForm.controls['idCapteur'].value;
            if (this.alerteCapteur.idCapteur == null || this.alerteCapteur.idCapteur == "") {
                this.generalError = "Le capteur doit être sélectionné";
                return;
            }
            this.alerteCapteur.nom = this.alerteCapteurForm.controls['nom'].value;
            this.alerteCapteur.envoyerA = this.alerteCapteurForm.controls['destinataireCourriel'].value;
            this.alerteCapteur.texterA = this.alerteCapteurForm.controls['destinataireSMS'].value;
            if (this.alerteCapteurForm.controls['min'].value != "") {
                this.alerteCapteur.minVaue = parseFloat(this.alerteCapteurForm.controls['min'].value);
            } else {
                this.alerteCapteur.minVaue = undefined;
            }
            if (this.alerteCapteurForm.controls['max'].value != "") {
                this.alerteCapteur.maxValue = parseFloat(this.alerteCapteurForm.controls['max'].value);
            } else {
                this.alerteCapteur.maxValue = undefined;
            }
            this.onButtonCreerAlerteCapteurClickInProgress = true;
            this._api.postAlerteCapteur(this.alerteCapteur.idCapteur, this.alerteCapteur)
                     .then(r => {
                         this.display = false;
                         r.emails = r?.envoyerA?.split(";");
                         r.numeros = r?.texterA?.split(";");
                         this.alertesCapteur?.push(r);
                     })
                     .catch(e => {
                        this.generalError = "Erreur lors de la modification de l'alerte";
                     })
                     .finally(() => {
                         this.onButtonCreerAlerteCapteurClickInProgress = false;
                     });
        }
        else {
            console.log("this.alerteCapteur is undefined");
            this.generalError = "L'alerte n'est pas défini";
        }
    }
}