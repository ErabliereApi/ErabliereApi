import { Component, Output, EventEmitter, ViewChild } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { ErabliereFormComponent } from 'src/app/client-view/erablieres/erabliere-form.component'
import { EButtonComponent } from 'src/generic/ebutton.component';

@Component({
    selector: 'ajouter-erabliere',
    templateUrl: './ajouter-erabliere.component.html',
    imports: [ErabliereFormComponent, EButtonComponent]
})
export class AjouterErabliereComponent {
    @ViewChild(ErabliereFormComponent) erabliereForm?: ErabliereFormComponent;
    modalTitle: string = "Ajouter une érablière";
    @Output() shouldReloadErablieres = new EventEmitter();
    postErabliereInProgress: boolean = false;
    erreurAjout?: string = undefined;

    constructor(private readonly _api: ErabliereApi) { }

    ajouterErabliere() {
        if (this.erabliereForm?.erabliere != null) {
            let erabliere = this.erabliereForm.erabliere

            this.erreurAjout = undefined;
            this.postErabliereInProgress = true;
            this._api.postErabliere(erabliere).then(() => {
                if (this.erabliereForm != undefined) {
                    this.erabliereForm.generalError = undefined
                    this.erabliereForm.errorObj = undefined
                    this.erabliereForm.erabliere = this.erabliereForm.getDefaultErabliere()
                }
                this.shouldReloadErablieres.emit();
            }).catch(error => {
                console.error("Erreur lors de l'ajout de l'érablière", error);
                if (this.erabliereForm != null) {
                    if (error.status == 400) {
                        this.erabliereForm.errorObj = error
                        this.erabliereForm.generalError = undefined
                    }
                    else {
                        this.erabliereForm.generalError = "Une erreur est survenue lors de l'ajout de l'érablière. Veuillez réessayer plus tard."
                    }
                }
                this.erreurAjout = "Une erreur est survenue lors de l'ajout de l'érablière. Veuillez réessayer plus tard.";
            }).finally(() => {
                this.postErabliereInProgress = false
            });
        }

        console.log(this.erabliereForm);
    }
}
