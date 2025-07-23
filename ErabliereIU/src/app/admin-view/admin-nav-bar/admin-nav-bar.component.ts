import { Component } from '@angular/core';
import {RouterLink, RouterLinkActive} from "@angular/router";
import {ConnectionButtonComponent} from "src/core/authorisation/connection-button/connection-button.component";
import {EnvironmentService} from "../../../environments/environment.service";

@Component({
    selector: 'admin-nav-bar',
    imports: [
        RouterLink,
        RouterLinkActive,
        ConnectionButtonComponent
    ],
    templateUrl: './admin-nav-bar.component.html'
})
export class AdminNavBarComponent {
    useAuthentication: boolean = false;

    constructor(private readonly environmentService: EnvironmentService) {
        this.useAuthentication = this.environmentService.authEnable ?? false;
    }
}
