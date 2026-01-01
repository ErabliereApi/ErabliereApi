import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from "@angular/core";
import { EinputComponent } from "src/generic/einput.component";
import { Rapport } from "src/model/rapport";
import { UntypedFormBuilder, UntypedFormGroup, FormsModule, ReactiveFormsModule } from "@angular/forms";

@Component({
    selector: 'app-modifier-rapport',
    templateUrl: './app-modifier-rapport.component.html',
    imports: [
        EinputComponent,
        FormsModule,
        ReactiveFormsModule
    ]
})
export class ModifierRapportsComponent implements OnChanges {
    isOpenModifierRapportModal: boolean = false;
    @Input() rapport?: Rapport | null;
    @Output() rapportModified = new EventEmitter<Rapport>();
    @Output() closeModal = new EventEmitter<void>();
    generalError: string = 'Ce formulaire est en construction. Revenez plus tard.';

    editRapportForm: UntypedFormGroup

    constructor(private readonly formBuilder: UntypedFormBuilder) {
        this.editRapportForm = this.formBuilder.group({
            nom: [''],
            type: [''],
            dateDebut: [''],
            dateFin: ['']
        });
    }
    ngOnChanges(changes: SimpleChanges): void {
        if (changes['rapport'] && this.rapport) {
            this.editRapportForm.patchValue({
                nom: this.rapport.nom,
                dateDebut: this.rapport.dateDebut,
                dateFin: this.rapport.dateFin
            });
        }
    }

    onClickEnregistrer() {
        if (!this.rapport) {
            this.generalError = 'Aucun rapport à modifier.';
            return;
        }
        this.rapportModified.emit(this.rapport);
    }
}