import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthorisationFactoryService } from 'src/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/authorisation/iauthorisation-service';


@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {

  private readonly authServiceInstance: IAuthorisationSerivce;

  constructor(authFactory: AuthorisationFactoryService, private readonly router: Router) {
    this.authServiceInstance = authFactory.getAuthorisationService();
  }

  async canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Promise<boolean> {
    const userHasRoleAdministrateur = await this.authServiceInstance.userIsInRole('administrateur');
    console.log('Can active /a, User has role administrateur: ', userHasRoleAdministrateur);
    if (!userHasRoleAdministrateur) {
      this.router.navigate(['/page401']);
    }
    return userHasRoleAdministrateur;
  }
}