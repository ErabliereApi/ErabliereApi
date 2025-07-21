import { Component, OnInit } from '@angular/core';
import {Router, RouterOutlet} from "@angular/router";
import {AdminNavBarComponent} from "./admin-nav-bar/admin-nav-bar.component";
import {IAuthorisationSerivce} from "../../authorisation/iauthorisation-service";
import {AuthorisationFactoryService} from "../../authorisation/authorisation-factory-service";

@Component({
    selector: 'admin-view',
    imports: [
        RouterOutlet,
        AdminNavBarComponent
    ],
    templateUrl: './admin-view.component.html'
})
export class AdminViewComponent implements OnInit {

    private readonly authSvc: IAuthorisationSerivce;

    constructor(authSvcFactory: AuthorisationFactoryService, private readonly router: Router ) {
        this.authSvc = authSvcFactory.getAuthorisationService();
    }

    ngOnInit(): void {
        this.authSvc.loginChanged.subscribe(async isLoggedIn => {
            if (!isLoggedIn) {
                this.router.navigate(['/']);
            }
            else {
                const user = await this.authSvc.getUserInfo();
                if (!user.roles.includes('administrator')) {
                    this.router.navigate(['/']);
                }
            }
        });
    }
  
}
