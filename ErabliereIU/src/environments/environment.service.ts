import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { OAuthConfig } from "src/model/oauthConfig";
import { UrlModel } from "../model/urlModel";
import { firstValueFrom } from "rxjs";

@Injectable({ providedIn: 'root' })
export class EnvironmentService {
  apiUrl?: string
  appRoot?: string
  clientId?: string
  tenantId?: string
  scopes?: string
  stsAuthority?: string
  authEnable?: boolean
  additionnalUrls?: UrlModel[]
  checkoutEnabled: boolean | undefined;

  constructor(private readonly _httpClient: HttpClient) {

  }

  loadConfig() {
    this.getAdditionnalUrls();

    return firstValueFrom(this._httpClient.get<OAuthConfig>("/assets/config/oauth-oidc.json")).then(c => {
      if (c == null) {
        return;
      }

      this.apiUrl = c.apiUrl;
      this.appRoot = c.appRoot;
      this.clientId = c.clientId;
      this.tenantId = c.tenantId;
      this.scopes = c.scopes;
      this.stsAuthority = c.stsAuthority;
      this.authEnable = c.authEnable;
    })
    .catch((err: any) => {
      console.error(err);
    });
  }

  getAdditionnalUrls() {
    return firstValueFrom(this._httpClient.get<UrlModel[]>("/assets/config/additionnalUrls.json")).then(c => {
      this.additionnalUrls = c;
    })
      .catch((err: any) => {
        console.error(err);
      });
  }
}
