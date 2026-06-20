import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { ApiKeyListComponent } from './api-key-list/api-key-list.component';
import { ApiKey, PostApiKey, PutApiKeyRestriction } from 'src/model/apikey';
import { EButtonComponent } from 'src/generic/ebutton.component';
import { EModalComponent } from 'src/generic/modal/emodal.component';
import { ApiKeyEditNameComponent } from "./api-key-edit-name/api-key-edit-name.component";
import { GenericFormComponent } from 'src/generic/forms/generic-form.component';
import { FormFieldConfig } from 'src/model/form-field-config';
import { Customer } from 'src/model/customer';
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";

@Component({
    selector: 'app-admin-apikeys',
    changeDetection: ChangeDetectionStrategy.Eager,
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
                        (submitClicked)="postNewApiKey($event)" /> 
                        
                    @if (newlyCreatedApiKey != null) {
                        <div class="api-key-container">
                            <h3 class="api-key-title">Votre nouvelle clé API ({{newlyCreatedApiKey.name}})</h3>

                            <div class="api-key-box">
                                <span class="api-key-value">{{ newlyCreatedApiKey.key }}</span>
                                <copy-id-button [text]="newlyCreatedApiKey.key" />
                            </div>

                            <p>
                                Utiliser la clé d'API avec l'entête 'X-ErabliereApi-ApiKey' vos requêtes HTTP.
                            </p>

                            <p class="api-key-warning">
                                ⚠️ Cette clé ne sera plus affichée.  
                                Copiez-la et conservez-la dans un endroit sécurisé.
                            </p>
                        </div>
                    }
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
    styleUrls: [
        './admin-apikeys.component.css'
    ],
    imports: [
        ApiKeyListComponent,
        EButtonComponent,
        EModalComponent,
        ApiKeyEditNameComponent,
        GenericFormComponent,
        CopyTextButtonComponent
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
            key: "name",
            label: "Nom",
            type: "text"
        },
        {
            key: "customerId",
            label: "Utilisateur",
            type: "select"
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
    ];
    cusotmersList: Customer[] = [];
    newlyCreatedApiKey?: ApiKey;

    constructor(private readonly _api: ErabliereApi) { }

    ngOnInit(): void {
        this._api.getApiKeys().then(apikeys => {
            this.apikeys = apikeys;
        });
        this._api.getCustomers().then(clist => {
            this.cusotmersList = clist;
            const custIdField = this.newApiKeyField.find(f => f.key == "customerId");
            if (custIdField != null) {
                custIdField.options = clist.map(c => ({
                    key: c.id as string,
                    value: c.email ?? ""
                }));
            }
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
        const r = new PostApiKey();
        r.name = formValue.name;
        r.customerId = formValue.customerId;
        this._api.postApiKey(formValue).then((r) => {
            this.newlyCreatedApiKey = r;
            this.ngOnInit();
        });
    }

    revoquer($event: string | undefined) {
        this._api.revokeApiKey($event).then(() => {
            this.ngOnInit();
        })
    }

    putNewAccessRule(formValue: any) {
        const r = new PutApiKeyRestriction();
        r.authorizeUris = formValue.autoriseUris;
        r.authorizeVerbs = formValue.autoriseVerbs;
        this._api.putApiKeyRestriction(this.editAccessApiKey?.id, r).then(() => {
            this.ngOnInit();
        });
    }
}