import { MsalService } from '@azure/msal-angular';
import { AccountInfo, PopupRequest, SilentRequest } from '@azure/msal-browser';
import { firstValueFrom, Subject } from 'rxjs';
import { EnvironmentService } from 'src/environments/environment.service';
import { AppUser } from 'src/model/appuser';
import { AuthResponse } from 'src/model/authresponse';
import { IAuthorisationSerivce } from './iauthorisation-service';

export class AzureADAuthorisationService implements IAuthorisationSerivce {
  private _isLoggedIn: boolean = false;
  private readonly _loginChangedSubject = new Subject<boolean>();
  loginChanged = this._loginChangedSubject.asObservable();
  type: string = "AzureAD";
  initialize: boolean = false;

  constructor(
    private readonly _msalInstance: MsalService, 
    private readonly _environmentService: EnvironmentService) {

  }

  async login() {
    if (!this.initialize) {
      await this.init();
    }
    const popupParam: PopupRequest = {
      scopes: this._environmentService.scopes?.split(' ') ?? [],
      prompt: "select_account"
    }
    await firstValueFrom(this._msalInstance.loginPopup(popupParam)).then(response => {
      return this.completeLogin();
    });
  }

  async isLoggedIn(): Promise<boolean> {
    if (!this.initialize) {
      await this.init();
    }

    let user = this.getUser();

    this._isLoggedIn = user != null;

    return this._isLoggedIn;
  }

  async completeLogin(): Promise<AppUser> {
    if (!this.initialize) {
      await this.init();
    }

    const user = this.getUser();
    if (user != null) {
      this._isLoggedIn = true;
      this._loginChangedSubject.next(true);
      this._msalInstance.instance.setActiveAccount(user);
      return {
        id: user.localAccountId ?? null,
        name: user.name ?? null,
        email: user.idTokenClaims?.email ?? null,
        roles: user.idTokenClaims?.roles ?? []
      } as AppUser;
    }

    this._isLoggedIn = false;
    this._loginChangedSubject.next(false);
    this._msalInstance.instance.setActiveAccount(null);
    return {
      id: null,
      name: null,
      email: null,
      roles: []
    } as AppUser;
  }

  logout() {
    this._msalInstance.logoutPopup().subscribe(async response => {
      await this.completeLogout();
    });
  }

  completeLogout(): Promise<AuthResponse> {
    return new Promise<AuthResponse>((resolve, reject) => {
      this._isLoggedIn = false;
      this._msalInstance.instance.setActiveAccount(null);
      this._loginChangedSubject.next(false);
      return resolve(new AuthResponse());
    })
  }

  async getAccessToken(): Promise<string | null> {
    if (!this.initialize) {
      await this.init();
    }

    const user = this.getUser();

    if (user == null) {
      return null
    }

    this._msalInstance.instance.setActiveAccount(user);

    const requestObj: SilentRequest = {
      scopes: this._environmentService.scopes?.split(' ') ?? [],
      authority: this._environmentService.stsAuthority,
      account: this.getUser() ?? undefined,
      forceRefresh: false
    };

    return firstValueFrom(this._msalInstance.acquireTokenSilent(requestObj)).then(authResult => {
      if (!this._isLoggedIn) {
        this.completeLogin();
      }
      return authResult?.accessToken ?? null;
    }).catch(reason => {
      console.log(reason);
      this._isLoggedIn = false;
      this._msalInstance.instance.setActiveAccount(null);
      this._loginChangedSubject.next(false);
      return null;
    });
  }

  getUser(): AccountInfo | null {
    if (!this.initialize) {
      return null;
    }
    let user = null;
    const accounts = this._msalInstance.instance.getAllAccounts();
    if (accounts.length > 0) {
      user = accounts[0];
    }
    return user;
  }

  async userIsInRole(role: string): Promise<boolean> {
    if (this._isLoggedIn) {
      const user = this.getUser();
      if (user != null) {
        const roles = user.idTokenClaims?.roles;
        console.log(roles);
        if (roles?.includes(role)) {
          return true;
        }
      }
    }

    return false;
  }

  async init(): Promise<void> {
    if (!this.initialize) {
      await firstValueFrom(this._msalInstance.initialize());
      this.initialize = true;
    }
  }

  getUserInfo(): Promise<AppUser> {
    const user = this.getUser();

    if (user != null) {
      return Promise.resolve({
        id: user.localAccountId ?? null,
        name: user.name ?? null,
        email: user.username ?? null,
        roles: user.idTokenClaims?.roles ?? []
      } as AppUser);
    } else {
      return Promise.reject(new Error("User not logged in"));
    }
  }
}
