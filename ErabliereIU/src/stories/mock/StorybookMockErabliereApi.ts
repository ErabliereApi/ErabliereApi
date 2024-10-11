import { DonneeCapteur } from "src/model/donneeCapteur";
import { IErabliereApi } from "src/core/erabliereapi.interface";
import { HttpHeaders, HttpResponse } from "@angular/common/http";
import { ApiKey } from "src/model/apikey";

export class StorybookMockErabliereApi implements IErabliereApi {
    getApiKeys(): Promise<ApiKey[]> {
        throw new Error("Method not implemented.");
    }
    async getDonneesCapteur(idCapteur: any, debutFiltre: string, finFiltre: string, xddr?: any): Promise<HttpResponse<DonneeCapteur[]>> {
        await new Promise(resolve => setTimeout(resolve, 1));
        return {
            body: [
                {
                    d: "2021-09-01T00:00:00",
                    valeur: 10
                },
                {
                    d: "2021-09-01T00:01:00",
                    valeur: 11
                },
                {
                    d: "2021-09-01T00:02:00",
                    valeur: 12
                },
                {
                    d: "2021-09-01T00:03:00",
                    valeur: 13
                },
            ],
            status: 200,
            statusText: "OK",
            headers: new HttpHeaders(),
            url: "mockStorybook"
        } as HttpResponse<DonneeCapteur[]>;
    }
}