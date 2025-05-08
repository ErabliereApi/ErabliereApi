import { provideAppInitializer, ApplicationConfig, provideZoneChangeDetection, importProvidersFrom, inject } from '@angular/core';
import { routes } from './app.routes';
import { provideHttpClient, withFetch, withInterceptorsFromDi } from '@angular/common/http';
import { EnvironmentService } from 'src/environments/environment.service';
import { MsalService, MSAL_INSTANCE, MsalGuard, MsalBroadcastService, MsalInterceptorConfiguration, MsalGuardConfiguration, MSAL_INTERCEPTOR_CONFIG } from '@azure/msal-angular';
import { BrowserCacheLocation, Configuration, InteractionType, IPublicClientApplication, LogLevel, PublicClientApplication } from '@azure/msal-browser';
import { provideNgxMask } from 'ngx-mask';
import { BrowserModule } from '@angular/platform-browser';
import 'chartjs-adapter-date-fns';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { env } from 'process';

export function MSALInstanceFactory(envSvc: EnvironmentService): IPublicClientApplication {
    if (!envSvc.authEnable) {
        return new PublicClientApplication({
            auth: {
                clientId: "null"
            }
        });
    }

    if (envSvc.clientId == undefined) {
        throw new Error("/assets/config/oauth-oidc.json/clientId cannot be null when using AzureAD authentication mode");
    }

    const msalConfig: Configuration = {
        auth: {
            clientId: envSvc.clientId,
            authority: "https://login.microsoftonline.com/" + envSvc.tenantId,
            redirectUri: "/signin-callback",
            postLogoutRedirectUri: "/signout-callback",
            navigateToLoginRequestUrl: true
        },
        cache: {
            cacheLocation: BrowserCacheLocation.LocalStorage,
        },
        system: {
            loggerOptions: {
                loggerCallback: (level: LogLevel, message: string, containsPii: boolean): void => {
                    if (containsPii) {
                        return;
                    }
                    switch (level) {
                        case LogLevel.Error:
                            console.error(message);
                            return;
                        case LogLevel.Info:
                            console.info(message);
                            return;
                        case LogLevel.Verbose:
                            console.debug(message);
                            return;
                        case LogLevel.Warning:
                            console.warn(message);
                            return;
                    }
                },
                piiLoggingEnabled: false
            },
            windowHashTimeout: 60000,
            iframeHashTimeout: 10000,
            loadFrameTimeout: 0,
            asyncPopups: false
        }
    };

    const pca = new PublicClientApplication(msalConfig);

    return pca;
}

export function MSALInterceptorConfigFactory(envSvc: EnvironmentService): MsalInterceptorConfiguration {
    const protectedResourceMap = new Map<string, Array<string>>();
    protectedResourceMap.set(
        envSvc.apiUrl ?? "",
        [envSvc.scopes ?? ""]
    );

    return {
        interactionType: InteractionType.Redirect,
        protectedResourceMap,
    };
}

export function MSALGuardConfigFactory(): MsalGuardConfiguration {
    return {
        interactionType: InteractionType.Redirect,
        authRequest: {
            scopes: [env.scopes ?? ""],
        },
        loginFailedRoute: '/login-failed',
    };
}

export const appConfig: ApplicationConfig = {
    providers: [
        provideAppInitializer(() => {
            const envSvc = inject(EnvironmentService);
            return envSvc.loadConfig();
        }),
        provideZoneChangeDetection({ eventCoalescing: true }),
        provideRouter(routes, withComponentInputBinding()),
        provideCharts(withDefaultRegisterables()),
        provideNgxMask(),
        importProvidersFrom(
            BrowserModule
        ),
        provideHttpClient(withInterceptorsFromDi(), withFetch()),
        {
            provide: MSAL_INSTANCE,
            useFactory: MSALInstanceFactory,
            deps: [EnvironmentService],
        },
        {
            provide: MSAL_INTERCEPTOR_CONFIG,
            useFactory: MSALInterceptorConfigFactory,
            deps: [EnvironmentService],
        },
        MsalService,
        MsalGuard,
        MsalBroadcastService,
    ],
};