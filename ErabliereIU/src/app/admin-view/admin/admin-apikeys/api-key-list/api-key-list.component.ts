import { Component, Input } from '@angular/core';
import { ApiKey } from 'src/model/apikey';

@Component({
    selector: 'api-key-list',
    templateUrl: './api-key-list.component.html',
    standalone: true
})
export class ApiKeyListComponent {

    @Input() apiKeys: ApiKey[] = [];

    constructor() { }
}