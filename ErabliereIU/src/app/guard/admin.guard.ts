import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/core/authorisation/iauthorisation-service';
import { EnvironmentService } from 'src/environments/environment.service';


@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {

  private readonly authSvc: IAuthorisationSerivce;

  constructor(
    authFactory: AuthorisationFactoryService,
    private readonly router: Router,
    private readonly env: EnvironmentService) {
    this.authSvc = authFactory.getAuthorisationService();
  }

  async canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Promise<boolean> {
    const isLoggedIn = await this.authSvc.isLoggedIn();
    if (!isLoggedIn) {
      this.router.navigate(['/page401']);
      return false;
    }
    let userHasRole = false;
    if (this.env.authEnable) {
      userHasRole = await this.authSvc.userIsInRole('administrateur');
      if (!userHasRole) {
        this.router.navigate(['/page401']);
      }
    }
    else {
      return true;
    }
    return userHasRole;
  }
}