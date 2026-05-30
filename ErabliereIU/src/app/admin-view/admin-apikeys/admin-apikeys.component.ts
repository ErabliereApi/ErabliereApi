import { Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { ApiKeyListComponent } from './api-key-list/api-key-list.component';
import { ApiKey, PutApiKeyRestriction } from 'src/model/apikey';
import { EButtonComponent } from 'src/generic/ebutton.component';
import { EModalComponent } from 'src/generic/modal/emodal.component';
import { ApiKeyEditNameComponent } from "./api-key-edit-name/api-key-edit-name.component";
import { GenericFormComponent } from 'src/generic/forms/generic-form.component';
import { FormFieldConfig } from 'src/model/form-field-config';

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
                    <app-generic-form 
                        [formConfig]="newApiKeyField"
                        (submitClicked)="(postNewApiKey)" />    
                </emodal>
            }
            @if (isDisplayEditNameModal)
            {
                <emodal
                    title="Modifier le nom"
                    (closeModal)="displayEditNameModal(false)">
                    <api-key-edit-name-compoent 
                        (needToUpdate)="ngOnInit()"
                        [apiKey]="editNameApiKey" />
                </emodal>
            }
            @if (isDisplayAccessModal)
            {
                <emodal
                    (closeModal)="displayEditAccessModal(false)">
                    <app-generic-form 
                        (submitClicked)="putNewAccessRule($event)"
                        [formConfig]="accesFormConfig" />
                </emodal>
            }
            <div>
                <api-key-list 
                    [apiKeys]="apikeys" 
                    (editNameFormOpen)="displayEditNameModal(true, $event)"
                    (editAccessFormOpen)="displayEditAccessModal(true, $event)"
                    (revokeApiKey)="revoquer($event)"></api-key-list>
            </div>
        </div>
    `,
    imports: [
        ApiKeyListComponent,
        EButtonComponent,
        EModalComponent,
        ApiKeyEditNameComponent,
        GenericFormComponent
    ]
})
export class AdminAPIKeysComponent implements OnInit {
    isDisplayAddModal: boolean = false;
    isDisplayEditNameModal: boolean = false;
    isDisplayAccessModal: boolean = false;
    editAccessApiKey?: ApiKey;
    editNameApiKey?: ApiKey;
    newApiKeyField: FormFieldConfig[] = [
        {
            key: "Nom",
            label: "Nom",
            type: "text"
        },
        {
            key: "customerId",
            label: "Utilisateur",
            type: "text"
        }
    ];
    accesFormConfig: FormFieldConfig[] = [
        {
            key: "autoriseUris",
            label: "Uris autorisés",
            type: "text"
        },
        {
            key: "autoriseVerbs",
            label: "Méthode autorisé",
            type: "text"
        }
    ]

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

    displayEditNameModal(arg0: boolean, apiKey?: ApiKey) {
        this.editNameApiKey = apiKey;
        this.isDisplayEditNameModal = arg0;
    }

    displayEditAccessModal(display: boolean, apiKey?: ApiKey) {
        this.editAccessApiKey = apiKey;
        if (this.editAccessApiKey != null) {
            const autUri = this.accesFormConfig.find(v => v.key == "autoriseUris");
            const autMathod = this.accesFormConfig.find(v => v.key == "autoriseVerbs");
            if (autUri != null) {
                autUri.initialValue = this.editAccessApiKey.authorizeUris;
            }
            if (autMathod != null) {
                autMathod.initialValue = this.editAccessApiKey.authorizeVerbs;
            }
        }
        this.isDisplayAccessModal = display;
    }

    postNewApiKey(formValue: any) {
        console.log(formValue);
        this._api.postApiKey(formValue).then(() => {
            this.ngOnInit();
        });
    }

    revoquer($event: string | undefined) {
        this._api.revokeApiKey($event).then(() => {
            this.ngOnInit();
        })
    }

    putNewAccessRule(formValue: any) {
        console.log(formValue);
        const r = new PutApiKeyRestriction();
        r.authorizeUris = formValue.autoriseUris;
        r.authorizeVerbs = formValue.autoriseVerbs;
        this._api.putApiKeyRestriction(this.editAccessApiKey?.id, r).then(() => {
            this.ngOnInit();
        });
    }
}