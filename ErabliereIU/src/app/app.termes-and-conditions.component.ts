import { DatePipe } from "@angular/common";
import { Component, OnInit } from "@angular/core"
import { Router } from "@angular/router";
import { AuthorisationFactoryService } from "src/core/authorisation/authorisation-factory-service";
import { IAuthorisationSerivce } from "src/core/authorisation/iauthorisation-service";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { EnvironmentService } from "src/environments/environment.service";

@Component({
    selector: 'terme-and-condition',
    templateUrl: 'app.termes-and-conditions.component.html',
    imports: [
        DatePipe
    ]
})
export class TermeAndConditionComponent implements OnInit {
    hasAcceptTerms: boolean = false;
    acceptTermsDate: string | null = null;
    readonly authSvc: IAuthorisationSerivce;
    isLoggedIn: boolean = false;
    emailContact: string | null = null;
    authEnable: boolean = false;

    constructor(private readonly api: ErabliereApi, private readonly authSvcFactory: AuthorisationFactoryService, private readonly env: EnvironmentService, private readonly router: Router) {
        this.authSvc = this.authSvcFactory.getAuthorisationService();
    }

    ngOnInit(): void {
        this.authSvc.loginChanged.subscribe((loggedIn) => {
            this.isLoggedIn = loggedIn;
            if (loggedIn) {
                this.checkTermsAcceptance();
            }
            else {
                this.hasAcceptTerms = true;
            }
        });
        this.authSvc.isLoggedIn().then((loggedIn) => {
            this.isLoggedIn = loggedIn;
            this.checkTermsAcceptance();
        });
        this.api.getOpenApiSpec().then((spec) => {
            this.emailContact = spec.info.contact?.email ?? null;
        }).catch((error) => {
            console.error("Failed to load OpenAPI spec:", error);
            this.emailContact = null; // Fallback if the spec cannot be loaded
        });
        this.authEnable = this.env.authEnable ?? false;
    }

    checkTermsAcceptance(): void {
        if (!this.isLoggedIn) {
            this.hasAcceptTerms = true;
            return;
        }

        this.api.getCustomerAcceptTerms().then((response) => {
            this.hasAcceptTerms = response.hasAcceptedTerms;
            this.acceptTermsDate = response.acceptTermsAt;
        });
    }

    acceptTerms(): void {
        if (!this.isLoggedIn) {
            return;
        }

        this.api.acceptTerms().then(() => {
            this.hasAcceptTerms = true;
            this.router.navigate(['/']);
        }).catch((error) => {
            console.error("Failed to accept terms:", error);
            alert("An error occurred while accepting the terms. Please try again later. Error: " + error.message);
        });
    }
}