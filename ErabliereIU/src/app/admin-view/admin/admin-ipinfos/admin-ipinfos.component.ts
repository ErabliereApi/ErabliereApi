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
    generalError: string | null = null;
    authorizeCountries: string[] = [];

    constructor(private readonly api: ErabliereApi) {

    }

    ngOnInit(): void {
        this.api.getIpInfos().then(infos => {
            this.ipInfos = infos;
        }).catch(err => {
            this.generalError = `Erreur lors de la récupération des informations: ${err.message}`
            console.error("Erreur lors de la récupération des informations", err);
        });

        this.api.getAuthorizedCountries().then(countries => {
            this.authorizeCountries = countries;
        }).catch(err => {
            this.generalError = `Erreur lors de la récupération des pays autorisés: ${err.message}`
            console.error("Erreur lors de la récupération des pays autorisés", err);
        });
    }
}