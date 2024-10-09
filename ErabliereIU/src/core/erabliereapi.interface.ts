import { HttpResponse } from "@angular/common/http";
import { DonneeCapteur } from "src/model/donneeCapteur";

export interface IErabliereApi {
    getDonneesCapteur(idCapteur: any, debutFiltre: string, finFiltre: string, xddr?: any): Promise<HttpResponse<DonneeCapteur[]>>;
}