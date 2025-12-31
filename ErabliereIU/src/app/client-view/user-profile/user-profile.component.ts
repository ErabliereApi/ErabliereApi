import { Component, OnInit } from "@angular/core";
import { AuthorisationFactoryService } from "src/core/authorisation/authorisation-factory-service";
import { IAuthorisationSerivce } from "src/core/authorisation/iauthorisation-service";
import { AppUser } from "src/model/appuser";
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Customer } from "src/model/customer";
import { CurrencyPipe, DatePipe } from "@angular/common";
import { EButtonComponent } from "src/generic/ebutton.component";
import { ApiKey } from "src/model/apikey";
import { EModalComponent } from "src/generic/modal/emodal.component";
import { EinputComponent } from "src/generic/einput.component";
import {
    ReactiveFormsModule,
    FormsModule,
    UntypedFormBuilder,
    Validators,
    FormControl
} from "@angular/forms";

@Component({
    selector: 'app-user-profile',
    templateUrl: './user-profile.component.html',
    imports: [
        CopyTextButtonComponent, 
        CurrencyPipe,
        DatePipe, 
        EButtonComponent, 
        EModalComponent, 
        EinputComponent, 
        ReactiveFormsModule, 
        FormsModule
    ],
})
export class UserProfileComponent implements OnInit {
    user: AppUser | null = null;
    customer: Customer | null = null;
    errorToken: string | null = null;
    errorCustomer: string | null = null;
    errorApiKey: string | null = null;
    editApiKeyNameForm: any;
    private readonly authSvc: IAuthorisationSerivce;

    constructor(authSvcFactory: AuthorisationFactoryService, private readonly api: ErabliereApi, private readonly fb: UntypedFormBuilder) {
        this.authSvc = authSvcFactory.getAuthorisationService();
        this.editApiKeyNameForm = this.fb.group({
            name: new FormControl(
                '',
                {
                    validators: [Validators.required, Validators.maxLength(100)],
                    updateOn: 'blur'
                }
            )
        });
    }

    ngOnInit(): void {
        this.loadUserProfile();
        this.authSvc.loginChanged.subscribe(() => {
            this.loadUserProfile();
        });
    }

    loadUserProfileClicked = false;

    loadUserProfile(): void {
        this.loadUserProfileClicked = true;
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
        }).finally(() => {
            this.loadUserProfileClicked = false;
        });
        this.getSubscriptions();
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

    buyApiKeyClicked = false;

    buyApiKey(): void {
        this.buyApiKeyClicked = true;
        this.api.startCheckoutSession().then(resonse => {
            globalThis.location.href = resonse.url;
        }).catch(error => {
            console.error("Error starting checkout session:", error);
            this.errorApiKey = "Erreur lors de la création de la session d'achat de clé API.";
        })
            .finally(() => {
                this.buyApiKeyClicked = false;
            });
    }

    editApiKeyNameActive: boolean = false;
    editApiKeyNameModalTitle: string = "Modifier le nom de la clé API";
    editedApiKey: ApiKey | null = null;

    editApiKeyName(_t55: ApiKey) {
        this.editedApiKey = { ..._t55 }; // Create a copy to edit
        this.editApiKeyNameActive = true;
    }

    saveEditApiKeyNameInProgress: boolean = false;
    editApiKeyErrorObj: any = null;

    saveEditedApiKeyName() {
        if (this.editedApiKey) {
            this.saveEditApiKeyNameInProgress = true;
            this.errorApiKey = null;
            this.editApiKeyErrorObj = null;
            let name = this.editApiKeyNameForm.controls['name'].value;
            this.api.updateApiKeyName(this.editedApiKey.id, name).then(() => {
                console.log("API key name updated successfully.");
                this.errorApiKey = null;
                this.loadUserProfile(); // Reload user profile to reflect changes
                this.editApiKeyNameActive = false;
            }).catch(error => {
                console.error("Error updating API key name:", error);
                this.errorApiKey = "Erreur lors de la mise à jour du nom de la clé API.";
                this.editApiKeyErrorObj = error.error;
            }).finally(() => {
                this.saveEditApiKeyNameInProgress = false;
            });
        }
    }

    closeEditApiKeyNameModal() {
        this.editApiKeyNameActive = false;
        this.editedApiKey = null;
    }

    gettingSubscriptions: boolean = false;
    errorGettingSubscriptions: string | null = null;
    subscriptions: any[] | null = null;

    getSubscriptions() {
        this.errorGettingSubscriptions = null;
        this.gettingSubscriptions = true;
        this.api.getCustomerSubscriptions().then(subscriptions => {
            if (subscriptions.length > 0) {
                this.subscriptions = subscriptions;
                this.upcomingInvoices = new Array(subscriptions.length);
                for (let i = 0; i < subscriptions.length; i++) {
                    this.getUpcommingInvoice(i, subscriptions[i].id);
                }
            } else {
                this.subscriptions = null;
                this.upcomingInvoices = null;
                this.errorGettingUpcomingInvoices = null;
            }
            this.errorGettingSubscriptions = null;
        }).catch(error => {
            console.error("Error getting subscriptions:", error);
            this.errorGettingSubscriptions = "Erreur lors de la récupération des abonnements.";
            this.subscriptions = null;
        }).finally(() => {
            this.gettingSubscriptions = false;
        });
    }

    upcomingInvoices: any[] | null = null;
    errorGettingUpcomingInvoices: string | null = null;
    gettingUpcomingInvoices: boolean = false;

    getUpcommingInvoice(index: number, subscriptionId: string) {
        this.gettingUpcomingInvoices = true;
        this.errorGettingUpcomingInvoices = null;
        this.api.getUpcomingInvoice(subscriptionId).then(invoice => {
            this.upcomingInvoices ??= [];
            this.upcomingInvoices[index] = invoice;
        }).catch(error => {
            console.error("Error getting upcoming invoice:", error);
            this.errorGettingUpcomingInvoices = "Erreur lors de la récupération de la prochaine facture.";
            this.upcomingInvoices = null;
        }).finally(() => {
            this.gettingUpcomingInvoices = false;
        });
    }
}