import { Observable } from "rxjs";
import { AppUser } from "src/model/appuser";
import { AuthResponse } from "src/model/authresponse";
import { IAuthorisationSerivce } from "./iauthorisation-service";

export class AuthorisationBypassService implements IAuthorisationSerivce {
    loginChanged: Observable<boolean> = new Observable<boolean>();
    type: string = "AuthDisabled";

    login(): Promise<void> {
        return Promise.resolve();
    }
    isLoggedIn(): Promise<boolean> {
        return Promise.resolve(true);
    }
    completeLogin(): Promise<AppUser> {
        return Promise.resolve(new AppUser());
    }
    logout(): void {
        // Do nothing
    }
    completeLogout(): Promise<AuthResponse> {
        return Promise.resolve(new AuthResponse());
    }
    getAccessToken(): Promise<string | null> {
        return Promise.resolve(null);
    }
    userIsInRole(role: string): Promise<boolean> {
        return Promise.resolve(false);
    }
    init(): Promise<void> {
        return Promise.resolve();
    }
}