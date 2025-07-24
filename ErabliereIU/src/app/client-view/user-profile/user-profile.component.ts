import { Component, OnInit } from "@angular/core";
import { AuthorisationFactoryService } from "src/core/authorisation/authorisation-factory-service";
import { IAuthorisationSerivce } from "src/core/authorisation/iauthorisation-service";
import { AppUser } from "src/model/appuser";
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Customer } from "src/model/customer";
import { DatePipe } from "@angular/common";

@Component({
    selector: 'app-user-profile',
    templateUrl: './user-profile.component.html',
    imports: [CopyTextButtonComponent, DatePipe],
})
export class UserProfileComponent implements OnInit {
    user: AppUser | null = null;
    customer: Customer | null = null;
    errorToken: string | null = null;
    errorCustomer: string | null = null;
    errorApiKey: string | null = null;
    private readonly authSvc: IAuthorisationSerivce;

    constructor(authSvcFactory: AuthorisationFactoryService, private readonly api: ErabliereApi) {
        this.authSvc = authSvcFactory.getAuthorisationService();
    }

    ngOnInit(): void {
        this.loadUserProfile();
        this.authSvc.loginChanged.subscribe(() => {
            this.loadUserProfile();
        });
    }

    loadUserProfile(): void {
        this.authSvc.getUserInfo().then(userInfo => {
            console.log("User profile loaded:", userInfo);
            this.user = userInfo;
            this.errorToken = null;
        }).catch(error => {
            console.error("Error loading user profile:", error);
            this.errorToken = "Erreur lors du chargement du profil utilisateur.";
            this.user = null;
        });
        this.api.getCurrentCustomer().then(customer => {
            this.customer = customer;
            this.errorCustomer = null;
        }).catch(error => {
            console.error("Error loading customer profile:", error);
            this.errorCustomer = "Erreur lors du chargement du profil client.";
            this.customer = null;
        });
    }

    deleteApiKey(arg0: string | undefined) {
        this.api.deleteApiKey(arg0).then(() => {
            console.log("API key deleted successfully.");
            this.errorApiKey = null;
            this.loadUserProfile(); // Reload user profile to reflect changes
        }).catch(error => {
            console.error("Error deleting API key:", error);
            if (error.status === 403) {
                this.errorApiKey = "Vous n'avez pas les droits pour supprimer cette clé API.";
            }
            else {
                this.errorApiKey = "Erreur lors de la suppression de la clé API.";
            }
        });
    }

    buyApiKey(): void {
        this.api.startCheckoutSession().then(resonse => {
            window.location.href = resonse.url;
        });
    }
}