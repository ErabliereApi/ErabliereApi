import { Component, OnInit } from "@angular/core";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { IpInfo } from "src/model/ipinfo";
import { IpinfoListComponent } from "./ipinfo-list/ipinfo-list.component";

@Component({
    selector: 'app-admin-ipinfos',
    templateUrl: './admin-ipinfos.component.html',
    imports: [
        IpinfoListComponent
    ]
})
export class AdminIpinfosComponent implements OnInit {
    ipInfos: IpInfo[] = [];

    constructor(private readonly api: ErabliereApi) {

    }

    ngOnInit(): void {
        this.api.getIpInfos().then(infos => {
            this.ipInfos = infos;
        });
    }
}