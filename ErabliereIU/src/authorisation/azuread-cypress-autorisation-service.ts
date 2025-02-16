import { Subject } from 'rxjs';
import { AppUser } from 'src/model/appuser';
import { AuthResponse } from 'src/model/authresponse';
import { IAuthorisationSerivce } from './iauthorisation-service';

export class AzureADCypressAuthorisationService implements IAuthorisationSerivce {
  private _isLoggingIn = false;
  private readonly _loginChangedSubject = new Subject<boolean>();
  type: string = "AzureADCypress";
  loginChanged = this._loginChangedSubject.asObservable();

  async login() {
    const appUser = await this.completeLogin();
    console.log(appUser);
  }

  isLoggedIn(): Promise<boolean> {
    return new Promise<boolean>((resolve, reject) => {
      if (this.getAccessToken() != null && !this._isLoggingIn) {
        this._isLoggingIn = true;
        this._loginChangedSubject.next(true);
      }

      return resolve(this._isLoggingIn);
    });
  }

  completeLogin() {
    return new Promise<AppUser>((resolve, reject) => {
      if (!this._isLoggingIn) {
        this._isLoggingIn = true;
        this._loginChangedSubject.next(true);
      }
      return resolve(new AppUser());
    });
  }

  logout() {
    this.completeLogout();
  }

  completeLogout() {
    return new Promise<AuthResponse>((resolve, reject) => {
      this._isLoggingIn = false;
      this._loginChangedSubject.next(false);
      return resolve(new AuthResponse());
    });
  }

  getAccessToken(): Promise<string | null> {
    return new Promise<string | null>((resolve, reject) => {
        const token = localStorage.getItem("adal.idtoken");

        console.log(token);

        if (token != null && !this._isLoggingIn) {
          this._isLoggingIn = true;
          this._loginChangedSubject.next(true);
        }

        return resolve(token);
    });
  }

  userIsInRole(role: string): Promise<boolean> {
    return Promise.resolve(true);
  }

  init(): Promise<void> {
    return Promise.resolve();
  }
}
