// This a component that allows to add a new erabliere
import { Component, OnInit, Input, Output, EventEmitter, ViewChild, OnChanges, SimpleChanges } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { ErabliereFormComponent } from '../erablieres/erabliere-form.component'
import { ErabliereAccessListComponent } from 'src/app/admin-view/access/erabliere-access-list/erabliere-access-list.component';
import { Erabliere } from "src/model/erabliere";
import { HoraireComponent } from '../horaire/horaire-form.component';
import { HoraireTableComponent } from '../horaire/horaire-table.component';
import { Horaire } from 'src/model/horaire';
import { EModalComponent } from 'src/generic/modal/emodal.component';
import { EButtonComponent } from 'src/generic/ebutton.component';

@Component({
    selector: 'modifier-erabliere',
    templateUrl: './modifier-erabliere.component.html',
    imports: [
        ErabliereFormComponent,
        ErabliereAccessListComponent,
        HoraireComponent,
        HoraireTableComponent,
        EModalComponent,
        EButtonComponent
    ]
})
export class ModifierErabliereComponent implements OnInit, OnChanges {
    @ViewChild(ErabliereFormComponent) erabliereForm?: ErabliereFormComponent;

    @Output() shouldReloadErablieres = new EventEmitter();

    @Input() authEnabled: boolean = false;
    @Input() erabliere?: Erabliere;

    modalTitle: string = "Modifier une érablière";
    afficherSectionDeleteErabliere: boolean = false;
    afficherSectionAcces: boolean = false;

    displayHoraireForm: boolean = false;
    horaire: Horaire = new Horaire();

    constructor(private readonly _api: ErabliereApi) { }

    ngOnInit() {
        if (this.erabliereForm) {
            this.erabliereForm.erabliere = { ...this.erabliere };
        }
        this.afficherSectionDeleteErabliere = false;
        this.afficherSectionAcces = false;
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['erabliere']?.currentValue) {
            if (this.erabliereForm) {
                this.erabliereForm.erabliere = { ...this.erabliere };
            }
            this.afficherSectionDeleteErabliere = false;
            this.afficherSectionAcces = false;
        }
    }

    modifierErabliere(): Promise<void> {
        if (this.erabliereForm != null) {
            if (this.erabliereForm.erabliere != null) {
                let erabliere = this.erabliereForm.erabliere;

                return this._api.putErabliere(erabliere).then(() => {
                    if (this.erabliereForm != null) {
                        this.erabliereForm.generalError = undefined;
                        this.erabliereForm.errorObj = undefined;
                    }
                    this.shouldReloadErablieres.emit();
                }).catch(error => {
                    if (this.erabliereForm != undefined) {
                        if (error.status == 400) {
                            this.erabliereForm.errorObj = error;
                            this.erabliereForm.generalError = undefined;
                        }
                        else {
                            this.erabliereForm.generalError = "Une erreur est survenue lors de la modification de l'érablière. Veuillez réessayer plus tard."
                        }
                    }
                })
            }
        }
        
        return Promise.reject(new Error("Erabliere Form non initialisé"));
    }

    showDeleteErabliere() {
        this.afficherSectionDeleteErabliere = true;
    }

    deleteErabliere() {
        const e = `Voulez-vous vraiment supprimer ${this.erabliere?.nom} ? Cette action est irréversible.`;

        if (confirm(e)) {
            let erabliere = this.erabliereForm?.erabliere;
            if (erabliere != undefined) {
                this._api.deleteErabliere(this.erabliere?.id, erabliere).then(() => {
                    this.afficherSectionDeleteErabliere = true;
                    this.shouldReloadErablieres.emit({ event: 'delete' });

                    // Hide the modal with the ID modifierErabliereFormModal
                    let modal = document.getElementById("modifierErabliereFormModal");
                    if (modal != null) {
                        modal.style.display = "none";

                        // remove the div with class modal-backdrop
                        let modalBackdrop = document.getElementsByClassName("modal-backdrop");
                        if (modalBackdrop.length > 0) {
                            modalBackdrop[0].remove();
                        }
                    }

                }).catch(error => {
                    console.error(error);
                    alert("Une erreur est survenue lors de la suppression de l'érablière. Veuillez réessayer plus tard.");
                });
            }
        }
    }

    changerAffichageAcces() {
        this.afficherSectionAcces = !this.afficherSectionAcces;
    }

    onHideModal() {
        this.afficherSectionAcces = false;
        this.afficherSectionDeleteErabliere = false;
    }
}
