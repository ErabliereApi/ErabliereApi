import { Observable } from "rxjs";
import { AppUser } from "src/model/appuser";
import { AuthResponse } from "src/model/authresponse";
import { IAuthorisationSerivce } from "./iauthorisation-service";

export class AuthorisationBypassService implements IAuthorisationSerivce {
    loginChanged: Observable<boolean> = new Observable<boolean>();
    type: string = "AuthDisabled";

    login(): Promise<void> {
        return new Promise<void>((resolve, reject) => { return resolve(); });
    }
    isLoggedIn(): Promise<boolean> {
        return new Promise<boolean>((resolve, reject) => resolve(true));
    }
    completeLogin(): Promise<AppUser> {
        return new Promise<AppUser>((resolve, reject) => resolve(new AppUser()));
    }
    logout(): void {
        
    }
    completeLogout(): Promise<AuthResponse> {
        return new Promise<AuthResponse>((resolve, reject) => resolve(new AuthResponse()));
    }
    getAccessToken(): Promise<String | null> {
        return new Promise((resolve, reject) => {
            return resolve(null);
        });
    }
}