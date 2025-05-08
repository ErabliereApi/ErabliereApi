import { DonneeCapteur } from "src/model/donneeCapteur";
import { HttpEvent, HttpHandlerFn, HttpHeaders, HttpInterceptorFn, HttpRequest, HttpResponse } from "@angular/common/http";
import { ApiKey } from "src/model/apikey";
import { Observable } from "rxjs";

export const StorybookMockErabliereApiFn: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn) => {
    console.log("Intercept: [" + req.method + "] " + req.url)
    if (req.url.includes("capteurs/capteur-guid/DonneesCapteurV2")) {
        const donneesCapteur: DonneeCapteur[] = [
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
        ];
        return mockResponse(req, donneesCapteur);
    } else if (req.url.includes("erabliere/api/apikeys")) {
        const apiKeys: ApiKey[] = [
            { id: "1", key: "12345" },
            { id: "2", key: "67890" }
        ];
        return mockResponse(req, apiKeys);
    }
    return next(req);
}

const mockResponse = (req: HttpRequest<any>, body: any): Observable<HttpEvent<any>> => {
    const headers = new HttpHeaders({ "Content-Type": "application/json" });
    const response = new HttpResponse({ status: 200, body, headers });
    return new Observable((observer) => {
        observer.next(response);
        observer.complete();
    })
}