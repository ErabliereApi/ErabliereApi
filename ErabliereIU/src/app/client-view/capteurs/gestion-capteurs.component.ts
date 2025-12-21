import { Component, Input, OnChanges, SimpleChanges } from "@angular/core";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Capteur } from "src/model/capteur";
import { CapteurListComponent } from "./capteur-list.component";
import { AjouterCapteurComponent } from "./ajouter-capteur.component";
import { AjouterCapteurImageComponent } from "./images/ajouter-capteur-image.component";
import { CapteurImage } from "src/model/capteurImage";
import { CapteurImageListComponent } from "./images/capteur-image-list.component";
import { EModalComponent } from "src/generic/modal/emodal.component";

@Component({
    selector: 'gestion-capteurs',
    templateUrl: 'gestion-capteurs.component.html',
    imports: [
        AjouterCapteurComponent, 
        CapteurListComponent, 
        AjouterCapteurImageComponent, 
        CapteurImageListComponent,
        EModalComponent
    ]
})
export class GestionCapteursComponent implements OnChanges {
    @Input() idErabliereSelectionee?: any;
    capteurs: Capteur[] = [];
    capteursImage: CapteurImage[] = [];
    afficherSectionAjouterCapteur: boolean = false;
    afficherSectionAjouterCapteurImage: boolean = false;
    loadingCapteurs: boolean = false;

    constructor(
        private readonly _api: ErabliereApi) {
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.idErabliereSelectionee) {
            this.getCapteurs();
            this.getCapteursImage();
        }
    }

    showAjouterCapteur() {
        this.afficherSectionAjouterCapteur = true;
    }

    hideAjouterCapteur() {
        this.afficherSectionAjouterCapteur = false;
    }

    async getCapteurs() {
        if (this.idErabliereSelectionee) {
            this.loadingCapteurs = true;
            this._api.getCapteurs(this.idErabliereSelectionee).then(capteurs => {
                this.capteurs = capteurs;
            }).finally(() => {
                this.loadingCapteurs = false;
            });
        }
    }

    async getCapteursImage() {
        if (this.idErabliereSelectionee) {
            this._api.getCapteursImage(this.idErabliereSelectionee).then(capteurs => {
                this.capteursImage = capteurs;
            })
        }
    }
}
