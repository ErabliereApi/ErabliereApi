import { Component, OnInit } from "@angular/core";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { IpInfo } from "src/model/ipinfo";
import { IpinfoListComponent } from "./ipinfo-list/ipinfo-list.component";
import { IpinfoMapComponent } from "./ipinfo-map/ipinfo-map.component";

@Component({
    selector: 'app-admin-ipinfos',
    templateUrl: './admin-ipinfos.component.html',
    imports: [
    IpinfoListComponent,
    IpinfoMapComponent
]
})
export class AdminIpinfosComponent implements OnInit {
    ipInfos: IpInfo[] = [];
    totalCount: string| null = null;
    generalError: string | null = null;
    authorizeCountries: string[] = [];

    constructor(private readonly api: ErabliereApi) {

    }

    ngOnInit(): void {
        this.api.getIpInfos().then(infos => {
            this.ipInfos = infos.items;
            this.totalCount = infos.count;
        }).catch(err => {
            this.totalCount = null;
            this.ipInfos = [];
            this.generalError = `Erreur lors de la récupération des informations: ${err.message}`
            console.error("Erreur lors de la récupération des informations", err);
        });

        this.api.getAuthorizedCountries().then(countries => {
            this.authorizeCountries = countries;
        }).catch(err => {
            this.generalError = `Erreur lors de la récupération des pays autorisés: ${err.message}`
            console.error("Erreur lors de la récupération des pays autorisés", err);
            this.authorizeCountries = [];
        });
    }
}