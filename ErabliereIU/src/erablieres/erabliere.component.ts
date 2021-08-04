import { Component, Input, OnInit } from '@angular/core';
import { AuthorisationFactoryService } from 'src/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/authorisation/iauthorisation-service';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Erabliere } from 'src/model/erabliere';

@Component({
    selector: 'erablieres',
    templateUrl: 'erabliere.component.html'
})
export class ErabliereComponent implements OnInit {
    erablieres?: Array<Erabliere>;

    erabliereSelectionnee?:number;

    @Input() cacheMenuErabliere?:boolean;

    @Input() pageSelectionnee?:number = 0;

    alertes?: Array<any>;

    private _authService: IAuthorisationSerivce

    constructor(private _erabliereApi: ErabliereApi, authFactory: AuthorisationFactoryService){
        this.erabliereSelectionnee = undefined;
        this._authService = authFactory.getAuthorisationService();
    }

    async ngOnInit() {
        this._authService.loginChanged.subscribe(loggedIn => {
            console.debug("Erabliere component loggin listner");
            console.debug(loggedIn);
            if (loggedIn) {
                this.ngOnInit();
            }
        });

        const erablieres = await this._erabliereApi.getErablieresExpandCapteurs();

        console.debug("On result of getErablieres");
        this.erablieres = erablieres;

        if (this.erablieres.length > 0) {
            this.erabliereSelectionnee = this.erablieres[0].id;
        }
        else {
            // TODO : Aucun érablière trouvé
        }
    }

    handleErabliereLiClick(idErabliere: number) {
        this.erabliereSelectionnee = idErabliere;
    }

    handleAlerteClick() {
        this.loadAlertes();
    }

    loadAlertes() {
        this._erabliereApi.getAlertes(this.erabliereSelectionnee).then(alertes => {
            this.alertes = alertes;
        });
    }
}