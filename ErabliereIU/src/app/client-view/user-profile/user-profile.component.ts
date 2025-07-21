import { Component, OnInit } from "@angular/core";
import { AuthorisationFactoryService } from "src/authorisation/authorisation-factory-service";
import { IAuthorisationSerivce } from "src/authorisation/iauthorisation-service";
import { AppUser } from "src/model/appuser";
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Customer } from "src/model/customer";

@Component({
    selector: 'app-user-profile',
    templateUrl: './user-profile.component.html',
    imports: [CopyTextButtonComponent],
})
export class UserProfileComponent implements OnInit {
editProfile() {
throw new Error('Method not implemented.');
}
    user: AppUser | null = null;
    customer: Customer | null = null;
    errorToken: string | null = null;
    errorCustomer: string | null = null;
    private readonly authSvc: IAuthorisationSerivce;

    constructor(authSvcFactory: AuthorisationFactoryService, private readonly api: ErabliereApi) {
        this.authSvc = authSvcFactory.getAuthorisationService();
    }

    ngOnInit(): void {
        this.loadUserProfile();
    }

    loadUserProfile(): void {
        this.authSvc.getUserInfo().then(userInfo => {
            console.log("User profile loaded:", userInfo);
            this.user = userInfo;
            this.errorToken = null;
        }).catch(error => {
            console.error("Error loading user profile:", error);
            this.errorToken = "Erreur lors du chargement du profil utilisateur.";
        });
        this.api.getCurrentCustomer().then(customer => {
            this.customer = customer;
            this.errorCustomer = null;
        }).catch(error => {
            console.error("Error loading customer profile:", error);
            this.errorCustomer = "Erreur lors du chargement du profil client.";
        });
    }
}