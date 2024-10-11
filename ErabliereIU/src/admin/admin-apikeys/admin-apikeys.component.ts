import { Component, Inject, OnInit } from '@angular/core';
import { IErabliereApi } from 'src/core/erabliereapi.interface';
import { ApiKeyListComponent } from './api-key-list/api-key-list.component';
import { ApiKey } from 'src/model/apikey';

@Component({
    selector: 'app-admin-apikeys',
    template: `
        <div class="m-3">
            <h3>Clé d'API</h3>
            <hr />
            <div class="container-fluid">
                <api-key-list [apiKeys]="apikeys"></api-key-list>
            </div>
        </div>
    `,
    standalone: true,
    imports: [ApiKeyListComponent]
})
export class AdminAPIKeysComponent implements OnInit {

    constructor(@Inject('IErabliereApi') private readonly _api: IErabliereApi) { }

    ngOnInit(): void {
        this._api.getApiKeys().then(apikeys => {
            this.apikeys = apikeys;
        });
    }

    apikeys: ApiKey[] = [];

}