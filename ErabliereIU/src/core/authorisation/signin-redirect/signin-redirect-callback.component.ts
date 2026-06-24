import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthorisationFactoryService } from '../authorisation-factory-service';
import { IAuthorisationSerivce } from '../iauthorisation-service';
import { broadcastResponseToMainFrame } from '@azure/msal-browser/redirect-bridge';

@Component({
    selector: 'app-signin-callback',
    template: '<div><p>Authentication en cours...</p></div>',
    standalone: true
})

export class SigninRedirectCallbackComponent implements OnInit {
    private readonly _authService: IAuthorisationSerivce

    constructor(authFactoryService: AuthorisationFactoryService, private readonly _router: Router) {
        this._authService = authFactoryService.getAuthorisationService();
    }

    ngOnInit() {
        if (this._authService.type == "AzureAD") {
            broadcastResponseToMainFrame();
        }
        else {
            this._authService.completeLogin().then(user => {
                this._router.navigate(['/'], { replaceUrl: true });
            });
        }
    }
}