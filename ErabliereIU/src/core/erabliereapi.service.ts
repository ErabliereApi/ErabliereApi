import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/core/authorisation/iauthorisation-service';
import { EnvironmentService } from 'src/environments/environment.service';
import { Alerte } from 'src/model/alerte';
import { AlerteCapteur } from 'src/model/alerteCapteur';
import { Baril } from 'src/model/baril';
import { Capteur } from 'src/model/capteur';
import { Conversation, Message } from 'src/model/conversation';
import { Customer } from 'src/model/customer';
import { CustomerAccess } from 'src/model/customerAccess';
import { Documentation } from 'src/model/documentation';
import { Dompeux } from 'src/model/dompeux';
import { Donnee } from 'src/model/donnee';
import { DonneeCapteur, PostDonneeCapteur } from 'src/model/donneeCapteur';
import { Erabliere } from 'src/model/erabliere';
import { ErabliereApiDocument } from 'src/model/erabliereApiDocument';
import { GetImageInfo } from 'src/model/imageInfo';
import { Note } from 'src/model/note';
import { PutCapteur } from 'src/model/putCapteur';
import { PostCapteurImage } from "../model/postCapteurImage";
import { CapteurImage } from "../model/capteurImage";
import { PutCapteurImage } from "../model/putCapteurImage";
import { WeatherForecast } from 'src/model/weatherForecast';
import { HourlyWeatherForecast } from 'src/model/hourlyweatherforecast';
import { PostDegresJoursRepportRequest, ResponseRapportDegreeJours } from 'src/model/postDegresJoursRepportRequest';
import { firstValueFrom } from 'rxjs';
import { GetMapAccessToken } from 'src/model/getMapAccessToken';
import { ApiKey } from 'src/model/apikey';
import { Rapport } from 'src/model/rapport';
import { PostImageGenerationResponse } from 'src/model/postImageGenerationResponse';
import { Horaire } from 'src/model/horaire';
import { Appareil } from 'src/model/appareil';
import { IpInfo } from 'src/model/ipinfo';

@Injectable({ providedIn: 'root' })
export class ErabliereApi {
    private readonly _authService: IAuthorisationSerivce

    constructor(private readonly _httpClient: HttpClient,
        authFactoryService: AuthorisationFactoryService,
        private readonly _environmentService: EnvironmentService) {
        this._authService = authFactoryService.getAuthorisationService();
    }

    async getErabliere(idErabliereSelectionee: any, withoutCapteur: boolean = false, withoutHoraire: boolean = false): Promise<Erabliere> {
        const headers = await this.getHeaders();

        let url = this._environmentService.apiUrl + '/erablieres?$filter=id eq ' + idErabliereSelectionee

        if (!withoutCapteur) {
            url += '&$expand=Capteurs($filter=afficherCapteurDashboard eq true;$orderby=indiceOrdre)';

            if (!withoutHoraire) {
                url += ',Horaires';
            }
        }

        if (withoutCapteur && !withoutHoraire) {
            url += '&$expand=Horaires';
        }

        const req = this._httpClient.get<Erabliere[]>(url, { headers: headers });

        const rtn = await firstValueFrom(req);

        if (rtn != null && rtn.length > 0) {
            return rtn[0];
        }
        return new Erabliere();
    }

    async getErablieres(top?: number, search?: string): Promise<Erabliere[]> {
        const headers = await this.getHeaders();
        top ??= 10;
        let url = this._environmentService.apiUrl + '/erablieres?$top=' + top + '&$expand=Horaires';
        if (search) {
            url += "&$filter=contains(nom, '" + search + "') or contains(codePostal, '" + search + "')";
        }
        const rtn = await firstValueFrom(this._httpClient.get<Erabliere[]>(url, { headers: headers }));
        return rtn ?? [];
    }

    async getErablieresAdmin(): Promise<Erabliere[]> {
        const headers = await this.getHeaders();
        const rtn = await firstValueFrom(this._httpClient.get<Erabliere[]>(this._environmentService.apiUrl + '/admin/erablieres', { headers: headers }));
        return rtn ?? [];
    }

    async getErablieresAdminExpandAccess(search?: string, top?: number, skip?: number) {
        const headers = await this.getHeaders();
        let url = this._environmentService.apiUrl + '/admin/erablieres?$count=true&$expand=customerErablieres';
        if (top) {
            url += "&$top=" + top;
        }
        if (skip) {
            url += "&$skip=" + skip;
        }
        if (search) {
            url += "&$filter=contains(nom, '" + search + "') or contains(codePostal, '" + search + "')";
        }
        const rtn = await firstValueFrom(this._httpClient.get<Erabliere[]>(url, { headers: headers, observe: 'response' }));
        return {
            items: rtn.body ?? [],
            count: Number.parseInt(rtn.headers.get('x-odatacount') ?? '0')
        }
    }

    async getErablieresExpandCapteurs(my: boolean): Promise<Erabliere[]> {
        let headers = await this.getHeaders();
        headers = headers.set('Accept', 'application/json');
        const rtn = await firstValueFrom(this._httpClient.get<Erabliere[]>(
            this._environmentService.apiUrl + '/erablieres?my=' + my + '&$expand=Capteurs($filter=afficherCapteurDashboard eq true)', { headers: headers }));
        return rtn ?? [];
    }

    async putErabliere(erabliere: Erabliere): Promise<void> {
        const headers = await this.getHeaders();
        await firstValueFrom(this._httpClient.put<void>(this._environmentService.apiUrl + '/erablieres/' + erabliere.id, erabliere, { headers: headers }));
    }

    async putErabliereAdmin(erabliere: Erabliere): Promise<void> {
        const headers = await this.getHeaders();
        await firstValueFrom(this._httpClient.put<void>(this._environmentService.apiUrl + '/Admin/Erablieres/' + erabliere.id, erabliere, { headers: headers }));
    }

    async getAlertes(idErabliereSelectionnee: any): Promise<Alerte[]> {
        const headers = await this.getHeaders();
        const rtn = await firstValueFrom(this._httpClient.get<Alerte[]>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/alertes?additionalProperties=true", { headers: headers }));
        return rtn ?? [];
    }

    async getAlertesCapteur(idErabliereSelectionnee: any): Promise<AlerteCapteur[]> {
        const headers = await this.getHeaders();
        const rtn = await firstValueFrom(this._httpClient.get<AlerteCapteur[]>(
            this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/alertesCapteur?additionnalProperties=true&include=Capteur",
            { headers: headers }));
        return rtn ?? [];
    }

    async getCapteurs(idErabliereSelectionnee: any): Promise<Capteur[]> {
        const headers = await this.getHeaders();
        const rtn = await firstValueFrom(this._httpClient.get<Capteur[]>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/capteurs?$orderby=indiceOrdre", { headers: headers }));
        return rtn ?? [];
    }

    async postCapteur(idErabliereSelectionnee: any, capteur: PutCapteur) {
        const headers = await this.getHeaders();
        return await firstValueFrom(this._httpClient.post<Capteur>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/capteurs", capteur, { headers: headers }));
    }

    async putCapteur(capteur: Capteur) {
        const headers = await this.getHeaders();
        return await firstValueFrom(this._httpClient.put<Capteur>(
            this._environmentService.apiUrl + `/erablieres/${capteur.idErabliere}/capteurs/${capteur.id}`, capteur,
            { headers: headers }));
    }

    async putCapteurs(idErabliereSelectionnee: string, capteurs: Capteur[]) {
        const headers = await this.getHeaders();
        return await firstValueFrom(this._httpClient.put<Capteur>(
            this._environmentService.apiUrl + `/erablieres/${idErabliereSelectionnee}/capteurs/`, capteurs,
            { headers: headers }));
    }

    async deleteCapteur(idErabliereSelectionnee: any, capteur: Capteur) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete<any>(
            this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/capteurs/" + capteur.id,
            {
                headers: headers
            }));
    }

    async postAlerte(idErabliereSelectionnee: any, alerte: Alerte): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.post<Alerte>(
            this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/alertes",
            alerte,
            { headers: headers }));
    }

    async putAlerte(idErabliereSelectionnee: any, alerte: Alerte): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<Alerte>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/alertes?additionalProperties=true", alerte, { headers: headers }));
    }

    async putAlerteCapteur(idCapteur: any, alerte: AlerteCapteur): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<AlerteCapteur>(this._environmentService.apiUrl + '/Capteurs/' + idCapteur + "/alerteCapteurs?additionalProperties=true", alerte, { headers: headers }));
    }

    async deleteAlerte(idErabliereSelectionnee: any, alerteId: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/alertes/" + alerteId, { headers: headers }));
    }

    async deleteAlerteCapteur(idCapteur: any, alerteId: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + '/Capteurs/' + idCapteur + "/AlerteCapteurs/" + alerteId, { headers: headers }));
    }

    async getBarils(idErabliereSelectionnee: any): Promise<Baril[]> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.get<Baril[]>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/baril", { headers: headers }));
        return rtn ?? [];
    }

    async getDonnees(idErabliereSelectionnee: any, debutFiltre: string, finFiltre: string, xddr?: any): Promise<HttpResponse<Donnee[]>> {
        let headers = await this.getHeaders();
        if (xddr != null) {
            headers = headers.set('x-ddr', xddr);
        }
        let httpCall = this._httpClient.get<Donnee[]>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/donnees?dd=" + debutFiltre + "&df=" + finFiltre, { headers: headers, observe: 'response' });
        const rtn = await firstValueFrom(httpCall);
        return rtn ?? new HttpResponse();
    }

    async getDonneesCapteur(idCapteur: any, debutFiltre: string, finFiltre: string, xddr?: any): Promise<HttpResponse<DonneeCapteur[]>> {
        let headers = await this.getHeaders();
        if (xddr != null) {
            headers = headers.set('x-ddr', xddr);
        }
        let httpCall = this._httpClient.get<DonneeCapteur[]>(this._environmentService.apiUrl + '/capteurs/' + idCapteur + "/DonneesCapteurV2?dd=" + debutFiltre + "&df=" + finFiltre, { headers: headers, observe: 'response' });
        const rtn = await firstValueFrom(httpCall);
        return rtn ?? new HttpResponse();
    }

    async getDonneesCapteurTop(idCapteur: any, top: number): Promise<DonneeCapteur[]> {
        let headers = await this.getHeaders();
        let httpCall = this._httpClient.get<DonneeCapteur[]>(this._environmentService.apiUrl + '/capteurs/' + idCapteur + "/DonneesCapteurV2?order=desc&top=" + top, { headers: headers, observe: 'response' });
        return firstValueFrom(httpCall).then(rtn => rtn.body ?? []);
    }

    async getDompeux(idErabliereSelectionnee: any, debutFiltre: string, finFiltre: string, xddr?: any): Promise<HttpResponse<Dompeux[]>> {
        let headers = await this.getHeaders();
        if (xddr != null) {
            headers = headers.set('x-ddr', xddr);
        }
        let httpCall = this._httpClient.get<Dompeux[]>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/dompeux?dd=" + debutFiltre + "&df=" + finFiltre, { headers: headers, observe: 'response' });
        const rtn = await firstValueFrom(httpCall);
        return rtn ?? new HttpResponse();
    }

    async getDocumentations(idErabliereSelectionnee: any, skip: number = 0, top?: number) {
        const headers = await this.getHeaders();
        let odataOptions = "?$count=true&$select=id,idErabliere,created,title,text,fileExtension";
        odataOptions += "&$skip=" + skip;
        odataOptions += top ? "&$top=" + top : "";
        const rtn = await firstValueFrom(this._httpClient.get<Documentation[]>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/documentation" + odataOptions, { headers: headers, observe: 'response' }));
        return { items: rtn.body ?? [], count: rtn.headers.get('x-odatacount') };
    }

    async getDocumentationBase64(idErabliereSelectionnee: any, idDocumentation: any): Promise<Documentation[]> {
        let headers = await this.getHeaders();
        headers = headers.set('Accept', 'application/json');
        const rtn = firstValueFrom(this._httpClient.get<Documentation[]>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/documentation?$select=file&$filter=id eq " + idDocumentation, { headers: headers }));
        return rtn ?? [];
    }

    async deleteDocumentation(idErabliereSelectionnee: any, idDocumentation: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/documentation/" + idDocumentation, { headers: headers }));
    }

    async getNotes(idErabliereSelectionnee: any, search?: string, skip: number = 0, top?: number) {
        const headers = await this.getHeaders();
        let odataOptions = "?$orderby=NoteDate desc&$count=true";
        odataOptions += "&$skip=" + skip;
        odataOptions += top ? "&$top=" + top : "";
        odataOptions += "&$select=id,idErabliere,noteDate,created,text,title,fileExtension,notificationFilter";
        odataOptions += "&$expand=rappel";
        if (search) {
            odataOptions += "&$filter=contains(text, '" + search + "') or contains(title, '" + search + "')";
        }
        const rtn = await firstValueFrom(this._httpClient.get<Note[]>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/notes" + odataOptions, { headers: headers, observe: 'response' }));
        return { items: rtn.body ?? [], count: rtn.headers.get('x-odatacount') };
    }

    async getActiveRappelNotes(idErabliereSelectionee: any): Promise<Note[]> {
        let headers = await this.getHeaders();
        headers = headers.set('Accept', 'application/json');
        const rtn = firstValueFrom(this._httpClient.get<Note[]>(
            this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionee + '/Notes' + "/ActiveRappelsNotes",
            { headers: headers }));
        return rtn ?? [];
    }

    async getNoteImage(idErabliere: any, id: any, noCache: boolean = false): Promise<ArrayBuffer> {
        let headers = await this.getHeaders();
        if (noCache) {
            headers = headers.set('Cache-Control', 'no-cache');
        }
        return firstValueFrom(this._httpClient.get(
            this._environmentService.apiUrl +
            '/erablieres/' + idErabliere + "/notes/" + id + "/image",
            { headers: headers, responseType: 'arraybuffer' }));
    }

    async postNote(idErabliereSelectionnee: any, note: Note): Promise<Note> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.post<Note>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/notes", note, { headers: headers }));
        return rtn ?? new Note();
    }

    async deleteNote(idErabliereSelectionnee: any, noteId: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/notes/" + noteId, { headers: headers }));
    }

    async postErabliere(erabliere: Erabliere): Promise<Erabliere> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.post<Erabliere>(this._environmentService.apiUrl + '/erablieres', erabliere, { headers: headers }));
        return rtn ?? new Erabliere();
    }

    async postDonneeCapteur(idCapteur: any, donneeCapteur: PostDonneeCapteur): Promise<DonneeCapteur> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.post<DonneeCapteur>(this._environmentService.apiUrl + '/Capteurs/' + idCapteur + "/DonneesCapteurV2", donneeCapteur, { headers: headers }));
        return rtn ?? new DonneeCapteur();
    }

    async postCapteurImage(idErabliereSelectionee: any, capteurImage: PostCapteurImage): Promise<CapteurImage | undefined> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.post<CapteurImage>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionee + '/CapteurImage', capteurImage, { headers: headers }));
    }

    async putCapteurImage(idErabliereSelectionee: any, idCapteur: string, capteurImage: PutCapteurImage): Promise<CapteurImage | undefined> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<CapteurImage>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionee + '/CapteurImage/' + idCapteur, capteurImage, { headers: headers }));
    }

    async deleteCapteurImage(idErabliereSelectionee: any, idCapteur: string) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete<CapteurImage>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionee + '/CapteurImage/' + idCapteur, { headers: headers }));
    }

    async getCapteursImage(idErabliereSelectionee: any) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.get<CapteurImage[]>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionee + '/CapteurImage', { headers: headers })) ?? [];
    }

    async postDocument(idErabliereSelectionee: any, document: ErabliereApiDocument): Promise<any> {
        let headers = await this.getHeaders();
        headers = headers.set('Accept', 'application/json');
        return firstValueFrom(this._httpClient.post<any>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionee + "/documentation", document, { headers: headers }));
    }

    async postAlerteCapteur(idCapteur: any, alerteCapteur: AlerteCapteur): Promise<AlerteCapteur> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.post<AlerteCapteur>(this._environmentService.apiUrl + '/Capteurs/' + idCapteur + "/AlerteCapteurs", alerteCapteur, { headers: headers }));
        return rtn ?? new AlerteCapteur();
    }

    async desactiverAlerteCapteur(idCapteur: any, idAlerte: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<AlerteCapteur>(this._environmentService.apiUrl + '/Capteurs/' + idCapteur + "/AlerteCapteurs/" + idAlerte + "/Desactiver", { idCapteur: idCapteur, id: idAlerte }, { headers: headers }));
    }

    async activerAlerteCapteur(idCapteur: any, idAlerte: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<AlerteCapteur>(this._environmentService.apiUrl + '/Capteurs/' + idCapteur + "/AlerteCapteurs/" + idAlerte + "/Activer", { idCapteur: idCapteur, id: idAlerte }, { headers: headers }));
    }

    async desactiverAlerte(idErabliere: any, idAlerte: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<AlerteCapteur>(this._environmentService.apiUrl + '/Erablieres/' + idErabliere + "/Alertes/" + idAlerte + "/Desactiver", { idErabliere: idErabliere, id: idAlerte }, { headers: headers }));
    }

    async activerAlerte(idErabliere: any, idAlerte: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<AlerteCapteur>(this._environmentService.apiUrl + '/Erablieres/' + idErabliere + "/Alertes/" + idAlerte + "/Activer", { idErabliere: idErabliere, id: idAlerte }, { headers: headers }));
    }

    async putNote(idErabliereSelectionnee: any, note: Note): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<Note>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/notes/" + note.id, note, { headers: headers }));
    }
    async putNotePeriodiciteDue(idErabliereSelectionnee: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<Note>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/notes/" + "PeriodiciteNotes", {}, { headers: headers }));
    }

    async putDocumentation(idErabliereSelectionnee: any, documentation: Documentation): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<Documentation>(this._environmentService.apiUrl + '/erablieres/' + idErabliereSelectionnee + "/documentation/" + documentation.id, documentation, { headers: headers }));
    }

    async getCustomers(): Promise<Customer[]> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.get<Customer[]>(this._environmentService.apiUrl + '/Customers', { headers: headers }));
        return rtn ?? [];
    }

    async getCustomersAdminExpandAccess(page: number, pageSize: number) {
        const headers = await this.getHeaders();
        const query = `?$skip=${(page - 1) * pageSize}&$top=${pageSize}&$expand=customerErablieres&$count=true`;
        const rtn = await firstValueFrom(this._httpClient.get<Customer[]>(this._environmentService.apiUrl + '/admin/customers' + query, { headers: headers, observe: 'response' }));
        return {
            items: rtn.body ?? [],
            count: Number.parseInt(rtn.headers.get('x-odatacount') ?? '0')
        }
    }

    async putCustomer(idCustomer: string, customer: Customer): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<Customer>(this._environmentService.apiUrl + '/admin/customers/' + idCustomer, customer, { headers: headers }));
    }

    async deleteCustomer(idCustomer: string): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + '/admin/customers/' + idCustomer, { headers: headers }));
    }

    async getCustomersAccess(idErabliere: string): Promise<CustomerAccess[]> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.get<CustomerAccess[]>(this._environmentService.apiUrl + '/Erablieres/' + idErabliere + "/Access", { headers: headers }));
        return rtn ?? [];
    }

    async getAdminErabliereAccess(idErabliere: string): Promise<CustomerAccess[]> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.get<CustomerAccess[]>(this._environmentService.apiUrl + '/Admin/Erablieres/' + idErabliere + "/Access", { headers: headers }));
        return rtn ?? [];
    }

    async getAdminCustomerAccess(idCustomer: string): Promise<CustomerAccess[]> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.get<CustomerAccess[]>(this._environmentService.apiUrl + '/Admin/Customers/' + idCustomer + "/Access", { headers: headers }));
        return rtn ?? [];
    }

    async postCustomerAccess(customerAccess: CustomerAccess): Promise<CustomerAccess> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.post<CustomerAccess>(
            `${this._environmentService.apiUrl}/Erablieres/${customerAccess.idErabliere}/Customer/${customerAccess.idCustomer}/Access`,
            { access: customerAccess.access },
            { headers: headers }));
        return rtn ?? new CustomerAccess();
    }

    async postAdminCustomerAccess(customerAccess: CustomerAccess): Promise<CustomerAccess> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.post<CustomerAccess>(
            `${this._environmentService.apiUrl}/Admin/Erablieres/${customerAccess.idErabliere}/Customer/${customerAccess.idCustomer}/Access`,
            { access: customerAccess.access },
            { headers: headers }));
        return rtn ?? new CustomerAccess();
    }

    async putCustomerAccess(customerAccess: CustomerAccess): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<any>(
            `${this._environmentService.apiUrl}/Erablieres/${customerAccess.idErabliere}/Customer/${customerAccess.idCustomer}/Access`,
            { access: customerAccess.access },
            { headers: headers }));
    }

    async putAdminCustomerAccess(customerAccess: CustomerAccess): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<any>(
            `${this._environmentService.apiUrl}/Admin/Erablieres/${customerAccess.idErabliere}/Customer/${customerAccess.idCustomer}/Access`,
            { access: customerAccess.access },
            { headers: headers }));
    }

    async deleteCustomerAccess(idErabliere: any, idCustomer: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + `/Erablieres/${idErabliere}/Customer/${idCustomer}/Access`, { headers: headers }));
    }

    async deleteAdminCustomerAccess(idErabliere: any, idCustomer: any): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + `/Admin/Erablieres/${idErabliere}/Customer/${idCustomer}/Access`, { headers: headers }));
    }

    async deleteErabliere(idErabliere: any, erabliere: Erabliere): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + '/Erablieres/' + idErabliere, { headers: headers, body: erabliere }));
    }

    async deleteErabliereAdmin(idErabliere: string): Promise<any> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + '/Admin/Erablieres/' + idErabliere, { headers: headers }));
    }

    async getWeatherForecast(idErabliere: any): Promise<WeatherForecast> {
        const headers = await this.getHeaders();
        const rtn = firstValueFrom(this._httpClient.get<WeatherForecast>(
            this._environmentService.apiUrl + '/Erablieres/' + idErabliere + "/WeatherForecast", { headers: headers }));
        return rtn ?? new WeatherForecast();
    }

    async geHourlyWeatherForecast(id: any): Promise<HourlyWeatherForecast[]> {
        const headers = await this.getHeaders();
        const rtn = await firstValueFrom(this._httpClient.get<HourlyWeatherForecast[]>(
            this._environmentService.apiUrl + '/Erablieres/' + id + "/WeatherForecast/Hourly", { headers: headers }));
        return rtn ?? [];
    }

    async startCheckoutSession(): Promise<any> {
        return await firstValueFrom(this._httpClient.post<any>(this._environmentService.apiUrl + "/Checkout", {}, {}));
    }

    async postPrompt(prompt: { Prompt: string; ConversationId: any; }) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.post<any>(this._environmentService.apiUrl + "/ErabliereAI/Prompt", prompt, { headers: headers }));
    }

    async getConversations(search?: string, top?: number, skip?: number): Promise<Conversation[]> {
        const headers = await this.getHeaders();
        let url = this._environmentService.apiUrl + "/ErabliereAI/Conversations?$orderby=lastMessageDate desc";
        if (isNotNullOrWhitespace(search)) {
            url += "&$filter=messages/any(m: contains(m/content, '" + search + "'))";
        }
        if (top) {
            url += "&$top=" + top;
        }
        if (skip) {
            url += "&$skip=" + skip;
        }
        const arr = await firstValueFrom(this._httpClient.get<Conversation[]>(url, { headers: headers }));
        if (arr) {
            return arr;
        }
        else {
            return [];
        }
    }

    async getMessages(conversationId: any): Promise<Message[]> {
        const headers = await this.getHeaders();
        const url = this._environmentService.apiUrl + "/ErabliereAI/Conversations/" + conversationId + "/Messages";
        const arr = await firstValueFrom(this._httpClient.get<Message[]>(url, { headers: headers }));
        if (arr) {
            return arr;
        }
        else {
            return [];
        }
    }

    async deleteConversation(id: any) {
        const headers = await this.getHeaders();
        const url = this._environmentService.apiUrl + "/ErabliereAI/Conversations/" + id;
        return firstValueFrom(this._httpClient.delete<any>(url, { headers: headers }));
    }

    async patchConversation(id: any, arg1: { isPublic: boolean; }) {
        const headers = await this.getHeaders();
        const url = this._environmentService.apiUrl + "/ErabliereAI/Conversations/" + id;
        return firstValueFrom(this._httpClient.patch<any>(url, arg1, { headers: headers }));
    }

    async getPublicConversation(conversationId: any) {
        const url = this._environmentService.apiUrl + "/ErabliereAI/Conversations/Public/" + conversationId;
        return firstValueFrom(this._httpClient.get<Conversation>(url));
    }

    private openApiSpecCache: any = null;
    private openApiSpecRequest: Promise<any> | null = null;

    async getOpenApiSpec(): Promise<any> {
        if (this.openApiSpecCache) {
            return this.openApiSpecCache;
        }

        if (this.openApiSpecRequest) {
            return this.openApiSpecRequest; // Return the ongoing request
        }

        this.openApiSpecRequest = firstValueFrom(
            this._httpClient.get<any>(this._environmentService.apiUrl + "/api/v1/swagger.json", {})
        );

        try {
            this.openApiSpecCache = await this.openApiSpecRequest;
        } finally {
            this.openApiSpecRequest = null; // Reset the request tracker
        }

        return this.openApiSpecCache;
    }

    async getImages(idErabliereSelectionnee: any, take: number, skip: number = 0, search?: string): Promise<GetImageInfo[]> {
        const headers = await this.getHeaders();
        let url = this._environmentService.apiUrl +
            '/erablieres/' + idErabliereSelectionnee +
            "/ImagesCapteur?take=" + take +
            "&skip=" + skip;

        if (isNotNullOrWhitespace(search)) {
            url += "&search=" + search;
        }

        return await firstValueFrom(this._httpClient.get<any>(
            url,
            { headers: headers }));
    }

    async traduire(message: string) {
        const headers = await this.getHeaders();
        return await firstValueFrom(this._httpClient.post<any>(
            this._environmentService.apiUrl + '/ErabliereAI/Traduction?from=en&to=fr',
            { text: message },
            { headers: headers }));
    }

    async getRapports(idErabliereSelectionee: string, filter?: string, expand?: string): Promise<Rapport[]> {
        const headers = await this.getHeaders();
        let url = this._environmentService.apiUrl + '/Erablieres/' + idErabliereSelectionee + '/Rapports';
        if (filter) {
            url += '?$filter=' + filter;
        }
        if (expand) {
            url += (filter ? '&' : '?') + '$expand=' + expand;
        }
        return await firstValueFrom(this._httpClient.get<Rapport[]>(url, { headers: headers }));
    }

    async getRapport(erabliereId: any, rapId: any) {
        const headers = await this.getHeaders();
        const url = this._environmentService.apiUrl + '/Erablieres/' + erabliereId + '/Rapports/' + rapId;
        return await firstValueFrom(this._httpClient.get<Rapport>(url, { headers: headers }));
    }

    async postDegresJours(idErabliere: any, form: PostDegresJoursRepportRequest, save?: boolean) {
        const headers = await this.getHeaders();
        let url = this._environmentService.apiUrl + '/Erablieres/' + idErabliere + '/Rapports/RapportDegreeJour';
        if (save) {
            url += '?sauvegarder=true';
        }
        return await firstValueFrom(this._httpClient.post<ResponseRapportDegreeJours>(url, form, { headers: headers }));
    }

    async refreshRapport(idErabliereSelectionee: string | null | undefined, id: string | undefined) {
        const headers = await this.getHeaders();
        const url = this._environmentService.apiUrl + '/Erablieres/' + idErabliereSelectionee + '/Rapports/Refresh/' + id + '/Refresh';
        return await firstValueFrom(this._httpClient.patch<any>(url, {}, { headers: headers }));
    }

    async deleteRapport(idErabliereSelectionee?: any, id?: any) {
        const headers = await this.getHeaders();
        const url = this._environmentService.apiUrl + '/Erablieres/' + idErabliereSelectionee + '/Rapports/' + id;
        return firstValueFrom(this._httpClient.delete<any>(url, { headers: headers }));
    }

    async getHeaders(): Promise<HttpHeaders> {
        const token = await this._authService.getAccessToken();
        if (token) {
            return new HttpHeaders().set('Authorization', `Bearer ${token}`);
        }
        return new HttpHeaders();
    }

    async getMapAccessToken(provider: string): Promise<GetMapAccessToken> {
        return await firstValueFrom(this._httpClient.get<GetMapAccessToken>(this._environmentService.apiUrl + '/api/Map/Access-Token/' + provider));
    }

    async getErablieresGeoJson(isPublic?: boolean, my?: boolean, capteur?: string, maxCapteur?: number | null): Promise<any> {
        const header = await this.getHeaders();

        let url = this._environmentService.apiUrl + '/Erablieres/GeoJson?';

        if (isPublic != null) {
            url += 'isPublic=' + isPublic;
        }

        if (my != null) {
            url += '&my=' + my;
        }

        if (capteur) {
            url += '&capteur=' + capteur;
        }

        if (maxCapteur) {
            url += '&topCapteur=' + maxCapteur;
        }

        return await firstValueFrom(this._httpClient.get<any>(url, { headers: header }));
    }

    async getApiKeys(): Promise<ApiKey[]> {
        const header = await this.getHeaders();

        return await firstValueFrom(
            this._httpClient.get<ApiKey[]>(
                this._environmentService.apiUrl + '/access/ApiKey?$expand=customer', { headers: header }));
    }

    async getTunnelKeys() {
        const header = await this.getHeaders();

        return await firstValueFrom(
            this._httpClient.get<any[]>(
                this._environmentService.apiUrl + '/api/Hologram/GetTunnelKeys', { headers: header }));
    }

    async disableTunnelKeys(deviceid: any) {
        const header = await this.getHeaders();

        return await firstValueFrom(
            this._httpClient.put<any>(
                this._environmentService.apiUrl + '/api/Hologram/' + deviceid + '/Disable', {}, { headers: header }));
    }

    async putNoteImage(idErabliere: any, id: any, file: File): Promise<void> {
        const headers = await this.getHeaders();
        headers.append('Content-Type', 'multipart/form-data');
        const formData = new FormData();
        formData.append('File', file, file.name);
        const fileExtension = file.name.split('.').pop();
        const url = this._environmentService.apiUrl + '/Erablieres/' + idErabliere + '/Notes/' + id + '/Image?FileExtension=' + fileExtension;
        await firstValueFrom(this._httpClient.put(url, formData, { headers: headers, responseType: 'arraybuffer' }));
    }

    async ErabliereIAImage(arg0: { imageCount: number; prompt: string; size: string; }): Promise<PostImageGenerationResponse> {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.post<PostImageGenerationResponse>(this._environmentService.apiUrl + '/ErabliereAI/Images', arg0, { headers: headers }));
    }

    async reportRappelProchainePeriode(idErabliere: any, id: any) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<any>(this._environmentService.apiUrl +
            '/Erablieres/' + idErabliere + '/Notes/' + id + '/RappelProchainePeriode', {}, { headers: headers }));
    }

    async getCurrentCustomer(expand?: string) {
        const headers = await this.getHeaders();
        let query = '';
        if (expand) {
            query = '?$expand=' + expand;
        }
        return firstValueFrom(this._httpClient.get<Customer>(this._environmentService.apiUrl +
            '/Customers/me' + query, { headers: headers }));
    }

    async deleteApiKey(arg0: string | undefined): Promise<void> {
        const headers = await this.getHeaders();
        await firstValueFrom(this._httpClient.delete<void>(this._environmentService.apiUrl + '/access/ApiKey/' + arg0, { headers: headers }));
    }

    async getCustomerAcceptTerms() {
      const headers = await this.getHeaders();
      return firstValueFrom(this._httpClient.get<any>(this._environmentService.apiUrl + '/Customers/me/has-accepted-terms', { headers: headers }));
    }

    async acceptTerms() {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.post<void>(this._environmentService.apiUrl + '/Customers/me/accept-terms', {}, { headers: headers }));
    }

    async adminAcceptTermsForDevices(deviceUniqueName: any) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.post<void>(this._environmentService.apiUrl + '/Admin/Customer/ConsentForDevice/' + deviceUniqueName, {}, { headers: headers }));
    }

    async putHoraire(erabliereId: any, value: Horaire) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<void>(this._environmentService.apiUrl + '/Erablieres/' + erabliereId + '/Horaire', value, { headers: headers }));
    }

    async getHoraires(id: any) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.get<Horaire[]>(this._environmentService.apiUrl + '/Erablieres/' + id + '/Horaire', { headers: headers }));
    }

    async getAppareils(id: any) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.get<Appareil[]>(this._environmentService.apiUrl + '/Erablieres/' + id + '/Appareil?$expand=Adresses,Ports($expand=Etat,PortService),NomsHost,Statut', { headers: headers }));
    }

    async supprimerTousLesAppareils(erabliereId: any) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + '/erablieres/' + erabliereId + '/appareil', { headers: headers }));
    }

    async importerScanNmap(erabliereId: any, file: File) {
        let headers = await this.getHeaders();
        headers = headers.set('Content-Type', 'text/xml');
        const textXml = await file.text();
        return firstValueFrom(this._httpClient.put(this._environmentService.apiUrl + '/erablieres/' + erabliereId + '/appareil/nmapscan', textXml, { headers: headers }));
    }

    async getIpInfos({ skip = 0, top = 50, search = '' }: { skip?: number; top?: number; search?: string; }) {
        let filter = '';
        if (isNotNullOrWhitespace(search)) {
            filter = `&$filter=contains(ipAddress, '${search}')`;
        }
        const headers = await this.getHeaders();
        const response = await firstValueFrom(this._httpClient.get<IpInfo[]>(this._environmentService.apiUrl + '/ipinfo?$count=true&$orderby=dm desc' + filter + `&$skip=${skip}&$top=${top}`, { headers: headers, observe: 'response' }));
        console.log(response.headers);
        return { items: response.body ?? [], count: response.headers.get('x-odatacount') ?? null };
    }

    async deleteIpInfo(id: any) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.delete(this._environmentService.apiUrl + '/ipinfo/' + id, { headers: headers }));
    }

    async getAuthorizedCountries() {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.get<string[]>(this._environmentService.apiUrl + '/ipinfo/authorized-countries', { headers: headers }));
    }

    async getIpInfosGroupedByCountry() {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.get<any[]>(this._environmentService.apiUrl + '/ipinfo/group-by-country', { headers: headers }));
    }

    async updateApiKeyName(id: string | undefined, name: string | undefined) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.put<void>(this._environmentService.apiUrl + '/access/ApiKey/' + id + '/name', { name: name }, { headers: headers }));
    }

    async getCustomerSubscriptions() {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.get<any[]>(this._environmentService.apiUrl + '/Checkout/subscriptions', { headers: headers }));
    }

    async getUpcomingInvoice(subscriptionId: string) {
        const headers = await this.getHeaders();
        return firstValueFrom(this._httpClient.get<any>(this._environmentService.apiUrl + '/Checkout/UpcomingInvoice?subscriptionId=' + subscriptionId, { headers: headers }));
    }  
}

function isNotNullOrWhitespace(search: string | undefined) {
    return search != null && search.trim() !== '';
}
