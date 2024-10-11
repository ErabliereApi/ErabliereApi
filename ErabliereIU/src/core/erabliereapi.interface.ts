import { HttpResponse } from "@angular/common/http";
import { ApiKey } from "src/model/apikey";
import { DonneeCapteur } from "src/model/donneeCapteur";

export interface IErabliereApi {
    getDonneesCapteur(idCapteur: any, debutFiltre: string, finFiltre: string, xddr?: any): Promise<HttpResponse<DonneeCapteur[]>>;
    getApiKeys(): Promise<ApiKey[]>;
}