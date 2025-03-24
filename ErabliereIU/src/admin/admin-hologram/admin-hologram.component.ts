import { Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { TunnelKeysTableComponent } from './tunnel-keys-table/tunnel-keys-table.component';

@Component({
    selector: 'admin-hologram',
    templateUrl: './admin-hologram.component.html',
    imports: [TunnelKeysTableComponent]
})
export class AdminHologramComponent implements OnInit {
    keys: any[] = [];

    constructor(private readonly api: ErabliereApi) { }

    ngOnInit(): void {
        this.getKeys();
    }

    getKeys(): void {
        this.api.getTunnelKeys().then((data: any) => {
            this.keys = data.data;
        });
    }

    disabletunnelkeys($event: any) {
        confirm('Are you sure you want to disable this key ? Id: ' + $event.id) &&
        this.api.disableTunnelKeys($event.id).then(() => {
            this.getKeys();
        });
    }

    enabletunnelkeys($event: any) {
        throw new Error('Method not implemented.');
    }
}