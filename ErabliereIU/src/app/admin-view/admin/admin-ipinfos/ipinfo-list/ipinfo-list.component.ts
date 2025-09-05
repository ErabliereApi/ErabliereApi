import { DatePipe } from "@angular/common";
import { Component, Input } from "@angular/core";
import { IpInfo } from "src/model/ipinfo";

@Component({
    selector: 'app-ipinfo-list',
    templateUrl: './ipinfo-list.component.html',
    imports: [
        DatePipe
    ],
})
export class IpinfoListComponent {
    @Input() ipInfos: IpInfo[] = [];
}