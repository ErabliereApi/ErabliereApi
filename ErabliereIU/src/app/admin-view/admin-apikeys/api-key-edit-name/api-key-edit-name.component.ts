import { Component, EventEmitter, Output } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { GenericFormComponent } from 'src/generic/forms/generic-form.component';
import { FormFieldConfig } from 'src/model/form-field-config';

@Component({
    selector: 'api-key-edit-name-compoent',
    standalone: true,
    templateUrl: './api-key-edit-name.component.html',
    imports: [GenericFormComponent]
})
export class ApiKeyEditNameComponent {
    editApiKeyNameField: FormFieldConfig[] = [
        {
            key: "Nom",
            label: "Nom",
            type: "text"
        }
    ]

    @Output() needToUpdate = new EventEmitter();

    constructor(private readonly api: ErabliereApi) {

    }

    editName(formValue: any) {
        console.log(formValue);
        this.api.updateApiKeyName(formValue.id, formValue.name).then(() => {
            this.needToUpdate.emit();
        })
    }
}