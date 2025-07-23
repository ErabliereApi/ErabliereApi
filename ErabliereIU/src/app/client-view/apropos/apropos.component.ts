import { Component } from "@angular/core";
import { EnvironmentService } from "src/environments/environment.service";
import { ErabliereApi } from "src/core/erabliereapi.service";


@Component({
    selector: 'apropos',
    templateUrl: "./apropos.component.html",
    imports: []
})
export class AproposComponent {
    urlApi?: string
    checkoutEnabled?: boolean
    supportEmail?: string
    tenantId?: string

    demoMode?: boolean
    realAppUrl?: string
    
    constructor(private readonly env: EnvironmentService, private readonly api: ErabliereApi){}

    ngOnInit(): void {
        this.urlApi = this.env.apiUrl;
        this.tenantId = this.env.tenantId;

        this.api.getOpenApiSpec().then(spec => {
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
        this.api.startCheckoutSession().then(resonse => {
            window.location.href = resonse.url;
        });
    }
}