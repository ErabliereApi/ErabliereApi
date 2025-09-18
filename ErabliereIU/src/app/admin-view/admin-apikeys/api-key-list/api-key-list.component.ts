import { Component, Input } from '@angular/core';
import { ApiKey } from 'src/model/apikey';
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";
import { DatePipe } from '@angular/common';

@Component({
    selector: 'api-key-list',
    templateUrl: './api-key-list.component.html',
    standalone: true,
    imports: [CopyTextButtonComponent, DatePipe]
})
export class ApiKeyListComponent {

    @Input() apiKeys: ApiKey[] = [];

    constructor() { }
}