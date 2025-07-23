import { Component, Input, OnInit } from '@angular/core';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/core/authorisation/iauthorisation-service';
import { EnvironmentService } from 'src/environments/environment.service';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { MsalService } from '@azure/msal-angular';
import {ConnectionButtonComponent} from "src/core/authorisation/connection-button/connection-button.component";

@Component({
    selector: 'client-nav-bar',
    templateUrl: 'client-nav-bar.component.html',
    imports: [RouterLink, RouterLinkActive, ConnectionButtonComponent]
})
export class ClientNavBarComponent implements OnInit {
  private readonly _authService: IAuthorisationSerivce

  @Input() idErabliereSelectionee?: string;
  @Input() thereIsAtLeastOneErabliere: boolean;

  useAuthentication: boolean = false;
  isLoggedIn: boolean;
  isAdminUser: boolean = false;
  isErabliereAIUser: boolean = false;
  mapFeatureEnable: boolean = false;

  constructor(
      authFactoryService: AuthorisationFactoryService,
      private readonly _environmentService: EnvironmentService,
      private readonly _api: ErabliereApi,
      private readonly _msalService: MsalService) {
    this._authService = authFactoryService.getAuthorisationService()
    this.useAuthentication = this._environmentService.authEnable ?? false;
    this.thereIsAtLeastOneErabliere = false
    this.isLoggedIn = !this.useAuthentication
  }

  ngOnInit(): void {
    this.checkApiFeaturesEnable();
    this.checkRoles();
  }

  checkApiFeaturesEnable() {
    // look at the openapi spec to see if the call endpoint is enable
    this._api.getOpenApiSpec().then(spec => {
      this.mapFeatureEnable = spec.paths['/api/Map/access-token/{provider}'] != null
    })
    .catch(err => {
        console.error(err);
    });
  }

  private checkRoles() {
      const account = this._msalService.instance.getActiveAccount();
      this.isAdminUser = false;
      if (account?.idTokenClaims) {
          const roles = account?.idTokenClaims['roles'];
          if (roles != null) {
              this.isAdminUser = roles.includes("administrateur");
              this.isErabliereAIUser = roles.includes("ErabliereAIUser");
          }
      }
  }

  onLoginChange(loginState: boolean) {
      this.isLoggedIn = loginState;
      this.checkRoles();
  }
}
