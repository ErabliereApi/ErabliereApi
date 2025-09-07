import { DatePipe } from "@angular/common";
import { Component, Input } from "@angular/core";
import { IpInfo } from "src/model/ipinfo";
import { EButtonComponent } from "src/generic/ebutton.component";
import { ErabliereApi } from "src/core/erabliereapi.service";

@Component({
    selector: 'app-ipinfo-list',
    templateUrl: './ipinfo-list.component.html',
    imports: [
    DatePipe,
    EButtonComponent
],
})
export class IpinfoListComponent {
    @Input() ipInfos: IpInfo[] = [];
    @Input() authorizeCountryCode: Array<string> = [];

    constructor(private readonly api: ErabliereApi) { }

    deleteIpInfo(id: any) {
        this.api.deleteIpInfo(id).then(() => {
            this.ipInfos = this.ipInfos.filter(info => info.id !== id);
        }).catch(err => {
            console.error("Erreur lors de la suppression de l'information IP", err);
        });
    }
}