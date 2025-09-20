import { Component, OnInit } from "@angular/core";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { IpInfo } from "src/model/ipinfo";
import { IpinfoListComponent } from "./ipinfo-list/ipinfo-list.component";
import { IpinfoMapComponent } from "./ipinfo-map/ipinfo-map.component";
import { PaginationComponent } from "src/generic/pagination/pagination.component";

@Component({
    selector: 'app-admin-ipinfos',
    templateUrl: './admin-ipinfos.component.html',
    imports: [
        IpinfoListComponent,
        IpinfoMapComponent,
        PaginationComponent
    ]
})
export class AdminIpinfosComponent implements OnInit {
    ipInfos: IpInfo[] = [];
    totalCount: number = 0;
    generalError: string | null = null;
    authorizeCountries: string[] = [];
    top: number = 25;
    skip: number = 0;

    constructor(private readonly api: ErabliereApi) {

    }

    ngOnInit(): void {
        this.loadIpInfos();

        this.api.getAuthorizedCountries().then(countries => {
            this.authorizeCountries = countries;
        }).catch(err => {
            this.generalError = `Erreur lors de la récupération des pays autorisés: ${err.message}`
            console.error("Erreur lors de la récupération des pays autorisés", err);
            this.authorizeCountries = [];
        });
    }

    nextPage($event: number) {
        this.skip = $event * this.top - this.top;
        this.loadIpInfos();
    }

    private loadIpInfos() {
        this.api.getIpInfos({ top: this.top, skip: this.skip }).then(infos => {
            this.ipInfos = infos.items;
            this.totalCount = parseInt(infos.count ?? "0");
        }).catch(err => {
            this.totalCount = 0;
            this.ipInfos = [];
            this.generalError = `Erreur lors de la récupération des informations: ${err.message}`;
            console.error("Erreur lors de la récupération des informations", err);
        });
    }
}