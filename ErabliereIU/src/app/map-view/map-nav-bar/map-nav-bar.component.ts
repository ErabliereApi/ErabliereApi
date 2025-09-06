import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ConnectionButtonComponent } from 'src/core/authorisation/connection-button/connection-button.component';
import { EnvironmentService } from 'src/environments/environment.service';

@Component({
    selector: 'map-nav-bar',
    template: `
    <nav class="navbar navbar-expand-lg bg-body-tertiary">
    <div class="container-fluid">
        <a class="navbar-brand" routerLinkActive="active" ariaCurrentWhenActive="page">
            Érablières
        </a>
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse"
            aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
            <span class="navbar-toggler-icon"></span>
        </button>
        <div class="navbar-collapse collapse d-lg-inline-flex flex-lg-row">
            <ul class="navbar-nav me-auto">
                <li class="nav-item">
                    <a class="nav-link ps-0" routerLink="/map" routerLinkActive="active"
                        ariaCurrentWhenActive="page">
                        Carte
                    </a>
                </li>
            </ul>
            <div class="d-flex gap-2">
                <button id="zone-erabliereiu-button" class="btn btn-outline-primary" routerLink="/e">ÉrablièreIU</button>
                @if (useAuthentication) {
                <connection-button />
                }
            </div>
        </div>
    </div>
</nav>
    `,
    imports: [
        ConnectionButtonComponent,
        RouterLink,
        RouterLinkActive
    ]
})
export class MapNavBarComponent{

    useAuthentication: boolean = false;

    constructor(private readonly environmentService: EnvironmentService) {
        this.useAuthentication = this.environmentService.authEnable ?? false;
    }

}