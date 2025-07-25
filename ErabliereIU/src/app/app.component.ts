import { Component, OnDestroy, OnInit } from '@angular/core';
import { EntraRedirectComponent } from './entra-redirect.component';
import { RouterOutlet } from '@angular/router';
import { ErabliereAIComponent } from 'src/generic/erabliereai/erabliereai-chat.component';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { MsalBroadcastService, MsalModule, MsalService } from '@azure/msal-angular';
import { IAuthorisationSerivce } from 'src/core/authorisation/iauthorisation-service';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { filter, Subject, takeUntil } from 'rxjs';
import { InteractionStatus } from '@azure/msal-browser';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  imports: [
    RouterOutlet,
    EntraRedirectComponent,
    ErabliereAIComponent,
    MsalModule
  ]
})
export class AppComponent implements OnInit, OnDestroy {
  erabliereAIEnable: boolean = false;
  erabliereAIUserRole: boolean = false;
  authService: IAuthorisationSerivce;
  private readonly _destroying$ = new Subject<void>();
  msalEncryptionInitialize: boolean = false;

  constructor(private readonly api: ErabliereApi,
    authServiceFactory: AuthorisationFactoryService,
    private readonly msalService: MsalService,
    private readonly msalBroadcastService: MsalBroadcastService) {
    this.authService = authServiceFactory.getAuthorisationService();
    if (this.authService.type == "AzureAD") {
      this.authService.loginChanged.subscribe(() => {
        this.checkRoleErabliereAI();
      });
    }
  }

  ngOnInit(): void {
    this.api.getOpenApiSpec().then(spec => {
      this.erabliereAIEnable = spec.paths['/ErabliereAI/Conversations'] !== undefined;
    })
      .catch(err => {
        console.error(err);
      });

    if (this.authService.type == "AzureAD") {
      this.msalBroadcastService.inProgress$
        .pipe(
          filter(
            function (status: InteractionStatus) {
              return status === InteractionStatus.Startup || status === InteractionStatus.None;
            }
          ),
          takeUntil(this._destroying$)
        )
        .subscribe(async (status) => {
          if (this.msalEncryptionInitialize) {
            return;
          }

          await this.authService.init();

          this.msalEncryptionInitialize = true;

          // get the user role to see if it got the ErabliereAIUser role
          // if so, enable the chat widget
          if (this.authService.type == "AzureAD") {
            this.checkRoleErabliereAI();
          }
        });
    }
    else {
      this.msalEncryptionInitialize = true;
    }
  }

  private async checkRoleErabliereAI() {
    await this.msalService.instance.initialize();
    const account = this.msalService.instance.getActiveAccount();
    if (account?.idTokenClaims) {
      const roles = account?.idTokenClaims['roles'];
      if (roles != null) {
        this.erabliereAIUserRole = roles.includes("ErabliereAIUser");
      }
      else {
        this.erabliereAIUserRole = false;
      }
    }
    else {
      this.erabliereAIUserRole = false;
    }
  }

  ngOnDestroy(): void {
    this._destroying$.next(undefined);
    this._destroying$.complete();
  }
}
