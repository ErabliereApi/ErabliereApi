import { Injectable } from "@angular/core";
import { OAuthConfig } from "src/model/oauthConfig";

@Injectable({ providedIn: 'root' })
export class EnvironmentService {
  apiUrl?: string
  appRoot?: string
  clientId?: string
  tenantId?: string
  scopes?: string
  stsAuthority?: string
  authEnable?: boolean
  checkoutEnabled: boolean | undefined;

  constructor() { }

  async loadConfig() {
    const r = await fetch("/assets/config/oauth-oidc.json");
    const c = await r.json() as OAuthConfig;

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
  }
}
