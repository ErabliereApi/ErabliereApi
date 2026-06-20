
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { ChirpstackSrvConfig } from 'src/model/chripstacksrvconfig';

@Component({
    selector: 'chirpstacksrvconfig-table',
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: './chirpstacksrvconfig-table.component.html',
    imports: []
})
export class ChirpstackSrvConfigTableComponent {
    @Input() configs: ChirpstackSrvConfig[] = [];
    @Output() deleteConfig: EventEmitter<any> = new EventEmitter();

    constructor() { }

    copyPublicKey(_t13: any) {
        navigator.clipboard.writeText(_t13.public_key);
    }
}