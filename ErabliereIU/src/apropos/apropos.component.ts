import { Component } from "@angular/core";
import { EnvironmentService } from "src/environments/environment.service";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { NgIf } from "@angular/common";

@Component({
    selector: 'apropos',
    templateUrl: "./apropos.component.html",
    imports: [NgIf]
})
export class AproposComponent {
    urlApi?: string
    checkoutEnabled?: boolean
    supportEmail?: string
    tenantId?: string

    demoMode?: boolean
    realAppUrl?: string
    
    constructor(private _enviromentService: EnvironmentService, private _erbliereApi: ErabliereApi){}

    ngOnInit(): void {
        this.urlApi = this._enviromentService.apiUrl;
        this.tenantId = this._enviromentService.tenantId;

        this._erbliereApi.getOpenApiSpec().then(spec => {
            this.supportEmail = spec.info.contact.email;
            this.checkoutEnabled = spec.paths['/Checkout'] !== undefined;
            this.demoMode = spec.info.demoMode;
            this.realAppUrl = spec.info.prodAppUrl;
        })
        .catch(err => {
            console.error(err);
        });
    }

    buyApiKey(): void {
        this._erbliereApi.startCheckoutSession().then(resonse => {
            window.location.href = resonse.url;
        });
    }
}