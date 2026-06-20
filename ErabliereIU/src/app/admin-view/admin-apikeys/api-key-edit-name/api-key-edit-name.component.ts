import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { GenericFormComponent } from 'src/generic/forms/generic-form.component';
import { ApiKey } from 'src/model/apikey';
import { FormFieldConfig } from 'src/model/form-field-config';

@Component({
    selector: 'api-key-edit-name-compoent',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: true,
    templateUrl: './api-key-edit-name.component.html',
    imports: [GenericFormComponent]
})
export class ApiKeyEditNameComponent implements OnInit, OnChanges {
    editApiKeyNameField: FormFieldConfig[] = [
        {
            key: "name",
            label: "Nom",
            type: "text"
        }
    ]

    @Input() apiKey?: ApiKey;
    @Output() needToUpdate = new EventEmitter();

    constructor(private readonly api: ErabliereApi) {

    }

    ngOnInit(): void {
        if (this.apiKey != null) {
            const name = this.editApiKeyNameField.find(v => v.key == "name");
            if (name != null) {
                name.initialValue = this.apiKey.name;
            }
        }
        console.log("NgOnInit ended", this.apiKey);
    }

    ngOnChanges(changes: SimpleChanges): void {
        const newInput = changes['apiKey'];
        if (newInput != null) {
            this.apiKey = newInput.currentValue as ApiKey;
        }
        this.ngOnInit();
    }

    editName(formValue: any) {
        console.log(this.apiKey);
        console.log(formValue);
        this.api.updateApiKeyName(this.apiKey?.id, formValue.name).then(() => {
            this.needToUpdate.emit();
        })
    }
}