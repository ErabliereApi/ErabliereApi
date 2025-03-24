import { NgFor, NgIf } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'tunnel-keys-table',
    templateUrl: './tunnel-keys-table.component.html',
    imports: [
        NgFor,
        NgIf
    ]
})
export class TunnelKeysTableComponent {
    @Input() tunnelKeys: any[] = [];
    @Output() disabletunnelkeys: EventEmitter<any> = new EventEmitter();
    @Output() enabletunnelkeys: EventEmitter<any> = new EventEmitter();

    constructor() { }

    copyPublicKey(_t13: any) {
        navigator.clipboard.writeText(_t13.public_key);
    }
}