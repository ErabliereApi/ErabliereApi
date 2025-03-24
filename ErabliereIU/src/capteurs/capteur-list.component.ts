import {Component, EventEmitter, Input, OnChanges, Output, SimpleChanges} from "@angular/core";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Capteur } from "src/model/capteur";
import {
    AbstractControl,
    FormArray,
    FormControl,
    FormGroup,
    ReactiveFormsModule,
    UntypedFormBuilder,
    Validators
} from "@angular/forms";
import { CapteurDetailTooltipComponent } from "./capteur-detail-tooltip.component";
import { ModifierCapteurDetailsComponent } from "./modifier-capteur-details.component";
import { ModifierCapteurStyleComponent } from "./modifier-capteur-style.component";

@Component({
    selector: 'capteur-list',
    templateUrl: 'capteur-list.component.html',
    styleUrl: 'capteur-list.component.css',
    imports: [
        ReactiveFormsModule, 
        CapteurDetailTooltipComponent, 
        ModifierCapteurDetailsComponent,
        ModifierCapteurStyleComponent
    ]
})
export class CapteurListComponent implements OnChanges {
    @Input() idErabliere?: string;
    @Input() capteurs: Capteur[] = [];

    @Output() shouldRefreshCapteurs = new EventEmitter<void>();

    formArrayIdToKey: Map<string, number> = new Map<string, number>();
    form: FormGroup;
    displayEdits: { [id: string]: boolean } = {};
    displayStyleEdits: { [id: string]: boolean } = {};
    editedCapteurs: { [id: string]: Capteur } = {};
    capteurTT: Capteur;
    displayTooltip: boolean = false;
    topPosition?: number;
    leftPosition?: number;

    displayEditDetailsForm: boolean = false;
    editDetailsCapteurSelected?: Capteur;

    displayEditStyleForm: boolean = false;
    editStyleSelected?: Capteur;

    constructor(private readonly erabliereApi: ErabliereApi, private readonly fb: UntypedFormBuilder) {
        this.form = this.fb.group({
            capteurs: new FormArray([])
        });
        this.capteurTT = new Capteur();
    }

    ngOnChanges(changes: SimpleChanges) {
        if(changes['capteurs']) {
            this.capteurs.forEach((capteur, key) => {
                this.formArrayIdToKey.set(capteur.id ?? '', key);
                const capteurFormGroup = this.fb.group({
                    indice: new FormControl(
                        capteur.indiceOrdre,
                        {
                            validators: [
                                Validators.required,
                                Validators.min(0)
                            ]
                        }
                    ),
                    nom: new FormControl(
                        capteur.nom,
                        {
                            validators: [
                                Validators.required,
                                Validators.maxLength(50)
                            ],
                            updateOn: 'blur'
                        }
                    ),
                    symbole: new FormControl(
                        capteur.symbole,
                        {
                            validators: [
                                Validators.maxLength(7)
                            ],
                            updateOn: 'blur'
                        }
                    ),
                    estGraphiqueAffiche: new FormControl(
                        capteur.afficherCapteurDashboard
                    ),
                    estSaisieManuelle: new FormControl(
                        capteur.ajouterDonneeDepuisInterface
                    )
                });
                this.getCapteurs().push(capteurFormGroup);
                this.displayEdits[capteur.id ?? ''] = false;
            });
        }
    }

    isDisplayEditForm(capteurId?: string): boolean {
        if (!capteurId) {
            return false;
        }

        return this.displayEdits[capteurId];
    }

    showModifierCapteur(capteur: Capteur) {
        if (capteur.id) {
            this.displayEdits[capteur.id] = true;
            const formCapteur = this.getCapteur(capteur.id);
            formCapteur.controls['indice'].setValue(capteur.indiceOrdre);
            formCapteur.controls['nom'].setValue(capteur.nom);
            formCapteur.controls['symbole'].setValue(capteur.symbole);
            formCapteur.controls['estGraphiqueAffiche'].setValue(capteur.afficherCapteurDashboard);
            formCapteur.controls['estSaisieManuelle'].setValue(capteur.ajouterDonneeDepuisInterface);
        }
    }

    hideModifierCapteur(capteur: Capteur) {
        if (capteur.id) {
            this.displayEdits[capteur.id] = false;
            this.editedCapteurs[capteur.id] = { ...capteur };
        }
    }

    modifierCapteur(capteur: Capteur) {
        if (!capteur.id) {
            console.error("capteur.id is undefined in modifierCapteur()");
        } else {
            const formCapteur = this.getCapteur(capteur.id);
            this.validateForm(capteur.id);
            if (formCapteur.valid) {
                this.editedCapteurs[capteur.id] = {...capteur};
                this.editedCapteurs[capteur.id].indiceOrdre = formCapteur.controls['indice'].value;
                this.editedCapteurs[capteur.id].nom = formCapteur.controls['nom'].value;
                this.editedCapteurs[capteur.id].symbole = formCapteur.controls['symbole'].value;
                this.editedCapteurs[capteur.id].afficherCapteurDashboard = formCapteur.controls['estGraphiqueAffiche'].value;
                this.editedCapteurs[capteur.id].ajouterDonneeDepuisInterface = formCapteur.controls['estSaisieManuelle'].value;
                
                const putCapteur = new Capteur();
                putCapteur.id = capteur.id;
                putCapteur.idErabliere = capteur.idErabliere;
                putCapteur.indiceOrdre = this.editedCapteurs[capteur.id].indiceOrdre;
                putCapteur.nom = this.editedCapteurs[capteur.id].nom;
                putCapteur.symbole = this.editedCapteurs[capteur.id].symbole;
                putCapteur.afficherCapteurDashboard = this.editedCapteurs[capteur.id].afficherCapteurDashboard;
                putCapteur.ajouterDonneeDepuisInterface = this.editedCapteurs[capteur.id].ajouterDonneeDepuisInterface;

                this.erabliereApi.putCapteur(putCapteur).then(() => {
                    this.shouldRefreshCapteurs.emit();
                    if (capteur.id) {
                        this.displayEdits[capteur.id] = false;
                    }
                }).catch(() => {
                    alert('Erreur lors de la modification du capteur');
                });
            }
        }
    }

    async supprimerCapteur(capteur: Capteur) {
        if (confirm('Cette action est irréversible, vous perderez tout les données associé au capteur. Voulez-vous vraiment supprimer ce capteur?')) {
            try {
                await this.erabliereApi.deleteCapteur(this.idErabliere, capteur)
                this.shouldRefreshCapteurs.emit();
            }
            catch (e) {
                alert('Erreur lors de la suppression du capteur');
            }
        }
    }

    formatDateJour(date?: Date | string): string {
        if (!date) {
            return "";
        }

        if (typeof date === "string") {
            date = new Date(date);
        }
        return date.toLocaleDateString("fr-CA");
    }

    getCapteurs() {
        return this.form.get('capteurs') as FormArray;
    }

    getCapteur(capteurId: string) {
        const arrayKey = this.formArrayIdToKey.get(capteurId);
        return this.getCapteurs().at(arrayKey ?? 0) as FormGroup;
    }
    getIndice(capteurId: string): AbstractControl<any, any> | null {
        return this.getCapteur(capteurId).controls['indice'] ?? null;
    }
    getNom(capteurId: string): AbstractControl<any, any> | null {
        return this.getCapteur(capteurId).controls['nom'] ?? null;
    }
    getSymbole(capteurId: string): AbstractControl<any, any> | null {
        return this.getCapteur(capteurId).controls['symbole'] ?? null;
    }
    getEstGraphiqueAffiche(capteurId: string) {
        return this.getCapteur(capteurId).controls['estGraphiqueAffiche'] ?? null;
    }
    getEstSaisieManuelle(capteurId: string) {
        return this.getCapteur(capteurId).controls['estSaisieManuelle'] ?? null;
    }

    copyId(event: MouseEvent, capteurId?: string) {
        if (!capteurId) {
            return;
        }

        const button = event.target as HTMLButtonElement;
        button.innerText = "Copié!";
        navigator.clipboard.writeText(capteurId);
        setTimeout(() => {
            button.innerHTML = "&#x2398;"
        }, 750);
    }

    validateForm(capteurId: string) {
        const form = document.getElementById('capteur-' + capteurId);
        this.getCapteur(capteurId).updateValueAndValidity();
        form?.classList.add('was-validated');
    }

    showModifierCapteurDetails(_t17: Capteur) {
        this.displayEditDetailsForm = true;
        this.editDetailsCapteurSelected = _t17;
    }

    showModifierCapteurStyle(capteur: Capteur) {
        if (capteur.id) {
            this.displayEditStyleForm = true;
            this.editStyleSelected = capteur;
        }
    }

    openTooltip(_t19: Capteur, e: MouseEvent) {
        if (this.displayTooltip) {
            this.closeTooltip();
        }
        this.capteurTT = _t19;
        this.displayTooltip = true;
        this.topPosition = e.clientY;
        this.leftPosition = e.clientX;
        console.log("openTooltip");
    }

    keyUpTooltip(_t21: Capteur,$event: KeyboardEvent) {
        if ($event.key === "Escape") {
            this.closeTooltip();
        }

        if ($event.key === "Enter" || $event.key === " ") {
            this.showModifierCapteurDetails(_t21);
        }
    }

    closeTooltip() {
        this.capteurTT = new Capteur();
        this.displayTooltip = false;
        this.topPosition = undefined;
        this.leftPosition = undefined;
        console.log("closeTooltip");
    }

    editDetailsNeedToUdate($event: any) {
        this.shouldRefreshCapteurs.emit();
        this.displayEditDetailsForm = false;
    }

    closeEditDetailsForm($event: any) {
        this.displayEditDetailsForm = false;
    }
}
