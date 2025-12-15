import { Component, EventEmitter, Input, Output } from "@angular/core";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Capteur } from "src/model/capteur";
import {
    ReactiveFormsModule
} from "@angular/forms";
import { CapteurDetailTooltipComponent } from "./capteur-detail-tooltip.component";
import { ModifierCapteurDetailsComponent } from "./modifier-capteur-details.component";
import { ModifierCapteurStyleComponent } from "./modifier-capteur-style.component";
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";
import { EModalComponent } from "src/generic/modal/emodal.component";

@Component({
    selector: 'capteur-list',
    templateUrl: 'capteur-list.component.html',
    styleUrl: 'capteur-list.component.css',
    imports: [
        ReactiveFormsModule,
        CapteurDetailTooltipComponent,
        ModifierCapteurDetailsComponent,
        ModifierCapteurStyleComponent,
        CopyTextButtonComponent,
        EModalComponent
    ]
})
export class CapteurListComponent {
    @Input() idErabliere?: string;
    @Input() capteurs: Capteur[] = [];

    @Output() shouldRefreshCapteurs = new EventEmitter<void>();

    formArrayIdToKey: Map<string, number> = new Map<string, number>();
    displayStyleEdits: { [id: string]: boolean } = {};
    editedCapteurs: { [id: string]: Capteur } = {};
    capteurTT: Capteur;
    displayTooltip: boolean = false;

    displayEditDetailsForm: boolean = false;
    editDetailsCapteurSelected?: Capteur;

    displayEditStyleForm: boolean = false;
    editStyleSelected?: Capteur;
    @Input() loading: boolean = false;

    constructor(private readonly erabliereApi: ErabliereApi) {
        this.capteurTT = new Capteur();
    }

    async supprimerCapteur(capteur: Capteur) {
        if (confirm('Cette action est irréversible, vous perderez tout les données associé au capteur. Voulez-vous vraiment supprimer ce capteur?')) {
            try {
                await this.erabliereApi.deleteCapteur(this.idErabliere, capteur)
                this.shouldRefreshCapteurs.emit();
            }
            catch (e) {
                console.error('Erreur lors de la suppression du capteur:', e);
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
        console.log("openTooltip");
    }

    keyUpTooltip(_t21: Capteur, $event: KeyboardEvent) {
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
        console.log("closeTooltip");
    }

    editDetailsNeedToUdate($event: any) {
        this.shouldRefreshCapteurs.emit();
        this.displayEditDetailsForm = false;
    }

    closeEditDetailsForm($event: any) {
        this.displayEditDetailsForm = false;
    }

    closeEditStyleForm($event: boolean) {
        this.displayEditStyleForm = false;
    }
}
