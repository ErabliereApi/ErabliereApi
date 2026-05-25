import { Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { ApiKeyListComponent } from './api-key-list/api-key-list.component';
import { ApiKey } from 'src/model/apikey';
import { EButtonComponent } from 'src/generic/ebutton.component';
import { EModalComponent } from 'src/generic/modal/emodal.component';
import { ApiKeyEditNameComponent } from "./api-key-edit-name/api-key-edit-name.component";

@Component({
    selector: 'app-admin-apikeys',
    template: `
        <div class="m-3">
            <h3>Clé d'API</h3>
            <hr />
            <ebutton type="primary" (clicked)="displayAddModal(true)">Ajouter</ebutton>
            @if (isDisplayAddModal)
            {
                <emodal
                    (closeModal)="displayAddModal(false)">
                </emodal>
            }
            @if (isDisplayEditNameModal)
            {
                <emodal
                    (closeModal)="displayEditNameModal(false)">
                    <api-key-edit-name-compoent />
                </emodal>
            }
            @if (isDisplayAccessModal)
            {
                <emodal
                    (closeModal)="displayEditAccessModal(false)">
                </emodal>
            }
            <div>
                <api-key-list [apiKeys]="apikeys"></api-key-list>
            </div>
        </div>
    `,
    imports: [ApiKeyListComponent, EButtonComponent, EModalComponent, ApiKeyEditNameComponent]
})
export class AdminAPIKeysComponent implements OnInit {
    isDisplayAddModal: boolean = false;
    isDisplayEditNameModal: boolean = false;
    isDisplayAccessModal: boolean = false;

    constructor(private readonly _api: ErabliereApi) { }

    ngOnInit(): void {
        this._api.getApiKeys().then(apikeys => {
            this.apikeys = apikeys;
        });
    }

    apikeys: ApiKey[] = [];

    displayAddModal(arg0: boolean) {
        this.isDisplayAddModal = arg0;
    }

    displayEditNameModal(arg0: boolean) {
        this.isDisplayEditNameModal = arg0;
    }

    displayEditAccessModal(arg0: boolean) {
        this.isDisplayAccessModal = arg0;
    }
}