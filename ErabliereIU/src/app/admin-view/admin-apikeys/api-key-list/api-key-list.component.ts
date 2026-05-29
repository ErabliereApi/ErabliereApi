import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ApiKey } from 'src/model/apikey';
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";
import { DatePipe } from '@angular/common';
import { EButtonComponent } from 'src/generic/ebutton.component';

@Component({
    selector: 'api-key-list',
    templateUrl: './api-key-list.component.html',
    standalone: true,
    imports: [CopyTextButtonComponent, DatePipe, EButtonComponent]
})
export class ApiKeyListComponent {
    @Input() apiKeys: ApiKey[] = [];
    @Output() editNameFormOpen = new EventEmitter<ApiKey>();
    @Output() editAccessFormOpen = new EventEmitter<ApiKey>();
    @Output() revokeApiKey = new EventEmitter<string|undefined>();

    constructor() { }

    revoquer(apikeyId: string|undefined) {
        if (confirm("Êtes-vus sur de vouloir supprimer la clé d'api avec l'id: " + apikeyId + " ?")) {
            this.revokeApiKey.emit(apikeyId);
        }
    }
}