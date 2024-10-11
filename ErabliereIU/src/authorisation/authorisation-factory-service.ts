import { Injectable } from "@angular/core";
import { MsalService } from "@azure/msal-angular";
import { environment } from "src/environments/environment";
import { EnvironmentService } from "src/environments/environment.service";
import { AuthorisationBypassService } from "./authorisation-bypass-service";
import { AuthorisationService } from "./authorisation-service";
import { AzureADAuthorisationService } from "./azuread-autorisation-service";
import { AzureADCypressAuthorisationService } from "./azuread-cypress-autorisation-service";
import { IAuthorisationSerivce } from "./iauthorisation-service";

@Injectable({ providedIn: 'root' })
export class AuthorisationFactoryService {
    constructor(private readonly _environment: EnvironmentService, private readonly _msalService: MsalService) {

    }

    private _cache?: IAuthorisationSerivce

    getAuthorisationService(): IAuthorisationSerivce {
        if (this._cache == null) {
            if (this._environment.authEnable) {
                if (this._environment.tenantId != undefined && this._environment.tenantId?.length > 1) {
                    if (!environment.production && window.Cypress) {
                        this._cache = new AzureADCypressAuthorisationService();
                    }
                    else {
                        this._cache = new AzureADAuthorisationService(this._msalService, this._environment);
                    }
                }
                else {
                    this._cache = new AuthorisationService(this._environment);
                }
            }
            else {
                this._cache = new AuthorisationBypassService();
            }
        }
        
        return this._cache;
    }
}