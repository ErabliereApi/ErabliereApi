import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from "@angular/core";
import { EinputComponent } from "src/generic/einput.component";
import { Rapport } from "src/model/rapport";
import { UntypedFormBuilder, UntypedFormGroup, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { InputErrorComponent } from "src/generic/input-error.component";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { EButtonComponent } from "src/generic/ebutton.component";

@Component({
    selector: 'app-modifier-rapport',
    templateUrl: './app-modifier-rapport.component.html',
    imports: [
        EinputComponent,
        FormsModule,
        ReactiveFormsModule,
        InputErrorComponent,
        EButtonComponent
    ]
})
export class ModifierRapportsComponent implements OnChanges {
    isOpenModifierRapportModal: boolean = false;
    @Input() rapport?: Rapport | null;
    @Input() idErabliereSelectionee?: any;
    @Output() closeModal = new EventEmitter<void>();
    generalError: string = '';

    editRapportForm: UntypedFormGroup

    errorObj?: any;

    constructor(private readonly formBuilder: UntypedFormBuilder, private readonly api: ErabliereApi) {
        this.editRapportForm = this.formBuilder.group({
            nom: [''],
            type: [''],
            dateDebut: [''],
            dateFin: [''],
            utiliserTemperatureTrioDonnee: [false],
            seuilTemperature: [''],
            afficherDansDashboard: [false]
        });
    }
    ngOnChanges(changes: SimpleChanges): void {
        if (changes['rapport'] && this.rapport) {
            this.editRapportForm.patchValue({
                nom: this.rapport.nom,
                type: this.rapport.type,
                utiliserTemperatureTrioDonnee: this.rapport.utiliserTemperatureTrioDonnee,
                dateDebut: this.rapport.dateDebut,
                dateFin: this.rapport.dateFin,
                seuilTemperature: this.rapport.seuilTemperature,
                afficherDansDashboard: this.rapport.afficherDansDashboard
            });
        }
    }

    saveInProgress: boolean = false;

    onClickEnregistrer() {
        if (!this.rapport) {
            this.generalError = 'Aucun rapport à modifier.';
            return;
        }
        this.saveInProgress = true;
        this.api.putRapport(this.idErabliereSelectionee, this.rapport.id, this.editRapportForm.value).then((updatedRapport) => {
            this.closeModal.emit();
        }).catch((error) => {
            if (error.status === 400) {
                this.errorObj = error.error;
            } else {
                this.generalError = 'Une erreur est survenue lors de la modification du rapport.';
            }
        }).finally(() => {
            this.saveInProgress = false;
        });
    }
}