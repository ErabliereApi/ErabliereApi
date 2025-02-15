import { UserManager, User, UserManagerSettings } from 'oidc-client'
import { Subject } from 'rxjs';
import { EnvironmentService } from 'src/environments/environment.service';
import { AppUser } from 'src/model/appuser';
import { AuthResponse } from 'src/model/authresponse';
import { IAuthorisationSerivce } from './iauthorisation-service';

export class AuthorisationService implements IAuthorisationSerivce {
    private readonly _userManager: UserManager;
    private _user?: User | null;
    private readonly _loginChangedSubject = new Subject<boolean>();
    type: string = "IdentityServer";
    loginChanged = this._loginChangedSubject.asObservable();

    constructor(_environmentService: EnvironmentService) {
        const stsSettings:UserManagerSettings = {
            authority: _environmentService.stsAuthority,
            client_id: _environmentService.clientId,
            client_secret: "secret",
            redirect_uri: `${_environmentService.appRoot}/signin-callback`,
            scope: "openid profile erabliereapi",
            response_type: 'code',
            post_logout_redirect_uri: `${_environmentService.stsAuthority}/signout-callback`
        };
        this._userManager = new UserManager(stsSettings);
     }
    
     login(): Promise<void> {
         return this._userManager.signinRedirect();
     }

     async isLoggedIn(): Promise<boolean> {
         const user = await this._userManager.getUser();
         const isLoggedIn = !!user && !user.expired;
         if (this._user !== user) {
             this._loginChangedSubject.next(isLoggedIn);
         }
         this._user = user;
         return isLoggedIn;
     }

    async completeLogin(): Promise<AppUser> {
        const user = await this._userManager.signinRedirectCallback();
        this._user = user;
        this._loginChangedSubject.next(!!user && !user.expired);
        return user;
    }

    logout() {
        this._userManager.signoutRedirect();
    }

    completeLogout(): Promise<AuthResponse> {
        return new Promise<AuthResponse>(() => {
            this._user = null;
            this._userManager.signoutRedirectCallback();
            return new AuthResponse();
        });
    }

    async getAccessToken() : Promise<string | null> {
        const user = await this._userManager.getUser();
        if (!!user && !user.expired) {
            return user.access_token;
        }
        else {
            return null;
        }
    }

    async userIsInRole(role: string): Promise<boolean> {
        const user = await this._userManager.getUser();
        if (!!user && !user.expired) {
            return user.profile.role === role;
        }
        else {
            return false;
        }
    }

    async init(): Promise<void> {
        return Promise.resolve();
    }
}