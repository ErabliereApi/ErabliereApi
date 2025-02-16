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
    console.log("login");
    if (!this.initialize) {
      await this.init();
    }
    else {
      console.log("MSAL already initialize at login");
    }
    const popupParam: PopupRequest = {
      scopes: this._environmentService.scopes?.split(' ') ?? [],
      prompt: "select_account"
    }
    const appUser = await firstValueFrom(this._msalInstance.loginPopup(popupParam)).then(response => {
      return this.completeLogin();
    });
    console.log("AppUser", appUser);
  }

  async isLoggedIn(): Promise<boolean> {
    console.log("isLoggedIn");
    if (!this.initialize) {
      await this.init();
    }
    else {
      console.log("MSAL already initialize at isLoggedIn");
    }

    let user = this.getUser();

    this._isLoggedIn = user != null;

    return this._isLoggedIn;
  }

  async completeLogin(): Promise<AppUser> {
    console.log("completeLogin")
    if (!this.initialize) {
      await this.init();
    }
    else {
      console.log("MSAL already initialize at completeLogin");
    }

    const user = this.getUser();
    if (user != null) {
      this._isLoggedIn = true;
      this._loginChangedSubject.next(true);
      this._msalInstance.instance.setActiveAccount(user);
      return new AppUser();
    }

    this._isLoggedIn = false;
    this._loginChangedSubject.next(false);
    this._msalInstance.instance.setActiveAccount(null);
    return new AppUser();
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
    console.log("getAccessToken");
    if (!this.initialize) {
      await this.init();
    }
    else {
      console.log("MSAL already initialize at getAccessToken");
    }

    const user = this.getUser();

    if (user == null) {
      console.log("No user found when getting access token, exiting the function");
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
      console.log("Initilize MSAL Instance");
      await firstValueFrom(this._msalInstance.initialize());
      this.initialize = true;
    }
  }
}
