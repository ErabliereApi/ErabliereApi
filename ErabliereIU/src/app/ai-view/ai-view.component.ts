import {Component} from '@angular/core';
import {RouterOutlet} from "@angular/router";
import {YouAreNotConnectedComponent} from "../client-view/you-are-not-connected/you-are-not-connected.component";
import { AiNavBarComponent } from './ai-nav-bar/ai-nav-bar.component';

@Component({
    selector: 'ai-view',
    imports: [
        RouterOutlet,
        AiNavBarComponent,
        YouAreNotConnectedComponent
    ],
    templateUrl: './ai-view.component.html',
    styles: [`
        body: {
            background-color: #f8f9fa;
        }
    `]      
})
export class AiViewComponent {

}
