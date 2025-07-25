import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/core/authorisation/iauthorisation-service';
import { EnvironmentService } from '../../../environments/environment.service';

@Component({
    selector: 'you-are-not-connected',
    templateUrl: 'you-are-not-connected.component.html',
    standalone: true
})
export class YouAreNotConnectedComponent implements OnInit {
  useAuthentication: boolean;
  isLoggedIn: boolean;
  tenantId?: string;

  private readonly _authService: IAuthorisationSerivce

  constructor(
      authFactoryService: AuthorisationFactoryService,
      private readonly environmentService: EnvironmentService,
      private readonly cdr: ChangeDetectorRef) {
    this._authService = authFactoryService.getAuthorisationService()
    this.useAuthentication = environmentService.authEnable ?? false
    this.isLoggedIn = false
  }
  ngOnInit(): void {
    this._authService.isLoggedIn().then(isLoggedIn => {
      this.isLoggedIn = isLoggedIn;
    });
    this._authService.loginChanged.subscribe(isLoggedIn => {
      this.isLoggedIn = isLoggedIn;
      this.cdr.detectChanges();
    });
    this.tenantId = this.environmentService.tenantId;
  }

  login() {
    this._authService.login();
  }
}
