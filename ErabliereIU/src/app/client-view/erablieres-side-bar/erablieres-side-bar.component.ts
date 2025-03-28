import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { AuthorisationFactoryService } from 'src/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/authorisation/iauthorisation-service';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Erabliere } from 'src/model/erabliere';
import { AjouterErabliereComponent } from 'src/erablieres/ajouter-erabliere.component';
import { ModifierErabliereComponent } from 'src/erablieres/modifier-erabliere.component';

@Component({
    selector: 'erablieres-side-bar',
    templateUrl: 'erablieres-side-bar.component.html',
    imports: [AjouterErabliereComponent, ModifierErabliereComponent]
})
export class ErabliereSideBarComponent implements OnInit {
  private readonly _authService: IAuthorisationSerivce

  @ViewChild(ModifierErabliereComponent) modifierErabliereComponent?: ModifierErabliereComponent;

  @Input() idSelectionne?: string;
  @Input() thereIsAtLeastOneErabliere: boolean = false;

  @Output() thereIsAtLeastOneErabliereChange = new EventEmitter<boolean>();
  @Output() idSelectionneChange = new EventEmitter<string>();

  authDisabled: boolean = false;
  erablieres: Array<Erabliere> = [];
  etat: string = "";
  erabliereSelectionnee?: Erabliere;
  loggedIn: boolean = false;

  search: string = "";
  displaySearch: boolean = false;

  constructor(private readonly _erabliereApi: ErabliereApi,
      authFactory: AuthorisationFactoryService,
      private readonly _router: Router) {
    this._authService = authFactory.getAuthorisationService();
    if (this._authService.type == "AuthDisabled") {
      this.authDisabled = true;
    }
    this._authService.loginChanged.subscribe(async loggedIn => {
      this.loggedIn = loggedIn;
      console.log("erablieres-side-bar: logged in changed. New value is " + loggedIn);
      this.erablieres = [];
      await this.loadErablieresPage();
    });
  }

  ngOnInit() {
    this._authService.isLoggedIn().then(loggedIn => {
      this.loggedIn = loggedIn;
      this.loadErablieresPage();
    });
  }

  async searchChanged($event: any) {
    this.search = $event.target.value;
    this.displaySearch = true;
    await this.loadErablieresPage();
  }

  async loadErablieresPage() {
    const titreChargement = "Chargement des érablières...";

    if (this.etat == titreChargement) {
      return new Promise<void>((resolve, reject) => { });
    }

    this.etat = titreChargement;

    const erablieres = await (this._erabliereApi.getErablieres(10, this.search).catch(err => {
      console.error(err);
      this.etat = "Erreur lors du chargement des érablieres";
    }));

    if (erablieres != null) {
      this.erablieres = erablieres.sort((a, b) => {
        let ioa = a.indiceOrdre ?? 2_147_483_647;
        let iob = b.indiceOrdre ?? 2_147_483_647;

        return ioa - iob;
      });

      if (this.erablieres.length > 0) {
        this.etat = "Chargement des erablieres terminé";
        if (this.erablieres.length >= 10 && !this.search) {
          this.displaySearch = true;
        }

        this.erabliereSelectionnee = this.erablieres.find(e => e.id === this.idSelectionne);
        if(!this.erabliereSelectionnee) {
          this.erabliereSelectionnee = this.erablieres[0];
          this.handleErabliereLiClick(this.erabliereSelectionnee.id);
        }
        this.thereIsAtLeastOneErabliere = true;
        this.thereIsAtLeastOneErabliereChange.emit(true);
      }
      else {
        this.etat = "Aucune erablière";
        this.thereIsAtLeastOneErabliere = false;
        this.thereIsAtLeastOneErabliereChange.emit(false);
        if (this.search == "") {
          this.displaySearch = false;
        }
      }
    }
  }

  handleErabliereLiClick(idErabliere: number) {
    if (!this.erablieres){
      return;
    }

    this.erabliereSelectionnee = this.erablieres.find(e => e.id === idErabliere);

    this.idSelectionne = this.erabliereSelectionnee!.id;
    this.idSelectionneChange.emit(this.idSelectionne);

    const urlParts = this._router.url.split("/");
    if (urlParts.length > 1 && urlParts[1] == "e") {
      if (urlParts.length > 3) {
        let page = this._router.url.split("/")[3];
        this._router.navigate(["/e", idErabliere, page]);
      } else {
        this._router.navigate(["/e", idErabliere, "graphiques"]);
      }
    }
  }

  async openEditErabliereForm(erabliere: Erabliere) {
    if (this.modifierErabliereComponent != undefined) {
      if (this.modifierErabliereComponent.erabliereForm != undefined) {
        this.modifierErabliereComponent.erabliereForm.erabliere = { ...erabliere };
      }
      else {
        console.log("erabliereForm is undefined");
      }
    }
    else {
      console.log("modifierErabliereComponent is undefined");
    }
  }
}
