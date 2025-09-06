import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MapNavBarComponent } from './map-nav-bar/map-nav-bar.component';

@Component({
    selector: 'map-view',
    template: `
    <header class="mb-2 border-bottom">
        <map-nav-bar />
    </header>
    <main class="container-fluid">
        <router-outlet></router-outlet>
    </main>
    `,
    imports: [
        RouterOutlet,
        MapNavBarComponent
    ]
})
export class MapViewComponent {

    constructor() { }

}