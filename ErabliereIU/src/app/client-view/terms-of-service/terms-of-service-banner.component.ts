import { Component, OnInit } from '@angular/core';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/core/authorisation/iauthorisation-service';
import { ErabliereApi } from 'src/core/erabliereapi.service';

@Component({
  selector: 'terms-of-service-banner',
  templateUrl: './terms-of-service-banner.component.html',
})
export class TermsOfServiceBannerComponent implements OnInit {
  hasAcceptTerms: boolean = true;
  readonly authSvc: IAuthorisationSerivce;
  isLoggedIn: boolean = false;

  constructor(private readonly api: ErabliereApi, private readonly authSvcFactory: AuthorisationFactoryService) {
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
  }

  checkTermsAcceptance(): void {
    if (!this.isLoggedIn) {
      this.hasAcceptTerms = true;
      return;
    }

    this.api.getCustomerAcceptTerms().then((response) => {
      this.hasAcceptTerms = response.hasAcceptedTerms;
    });
  }
}