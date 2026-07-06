// This component is part of @azure/msal-angular and can be imported and bootstrapped
import { ChangeDetectionStrategy, Component, OnInit } from "@angular/core";
import { MsalService } from "@azure/msal-angular";
import { AuthorisationFactoryService } from "src/core/authorisation/authorisation-factory-service";

@Component({
    selector: 'entra-redirect',
    template: '',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: true
})
export class EntraRedirectComponent implements OnInit {

    constructor(
        private readonly authService: MsalService,
        private readonly authFactoryService: AuthorisationFactoryService) { }

    ngOnInit(): void {
        this.authService.handleRedirectObservable().subscribe(result => {
            // A non-null result means a redirect (login) just completed. In redirect
            // mode the response arrives asynchronously after the page reload, so we
            // must notify the authorisation service to update its login state and emit
            // loginChanged. Otherwise the UI only reflects the login after a manual refresh.
            if (result != null) {
                const authorisationService = this.authFactoryService.getAuthorisationService();
                if (authorisationService.type === "AzureAD") {
                    authorisationService.completeLogin();
                }
            }
        });
    }

}
