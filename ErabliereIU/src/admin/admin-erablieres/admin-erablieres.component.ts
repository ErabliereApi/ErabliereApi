import { Component, OnInit } from '@angular/core';
import { Erabliere } from "../../model/erabliere";
import { ErabliereApi } from "../../core/erabliereapi.service";
import { ErabliereListComponent } from "./erabliere-list/erabliere-list.component";
import {ModifierErabliereComponent} from "./modifier-erabliere/modifier-erabliere.component";
import { PaginationComponent } from "../../pagination/pagination.component";

@Component({
  selector: 'admin-erablieres',
  standalone: true,
    imports: [
        ErabliereListComponent,
        ModifierErabliereComponent,
        PaginationComponent
    ],
  templateUrl: './admin-erablieres.component.html'
})
export class AdminErablieresComponent implements OnInit {
    erablieres: Erabliere[] = [];
    erabliereAModifier: Erabliere | null = null;
    search: string = "";
    top: number = 10;
    skip: number = 0;
    totalErabliere: number = 0;

    constructor(private _api: ErabliereApi) { }

    ngOnInit() {
        this.chargerErablieres();
    }

    chargerErablieres() {
        this._api.getErablieresAdminExpandAccess(this.search, this.top, this.skip).then(erablieres => {
            this.erablieres = erablieres;
        }).catch(error => {
            this.erablieres = [];
            throw error;
        });
    }

    supprimerErabliere(erabliere: Erabliere) {
        if (confirm("Voulez-vous vraiment supprimer l'érablière " + erabliere.nom + " ? ")) {
            this._api.deleteErabliereAdmin(erabliere.id)
                .then(a => {
                    this.chargerErablieres();
                });
        }
    }

    demarrerModifierErabliere(erabliere: Erabliere) {
        this.erabliereAModifier = erabliere;
    }

    terminerModifierErabliere(update: boolean) {
        this.erabliereAModifier = null;
        if (update) {
            this.chargerErablieres();
        }
    }

    rechercher($event: any) {
        this.search = $event.target.value;
        this.chargerErablieres();
    }
}
