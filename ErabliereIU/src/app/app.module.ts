import { NgModule, DoBootstrap, ApplicationRef, inject, provideAppInitializer } from '@angular/core';
import { AppComponent } from './app.component';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { EnvironmentService } from 'src/environments/environment.service';
import { MsalService, MSAL_INSTANCE } from '@azure/msal-angular';
import { BrowserCacheLocation, Configuration, IPublicClientApplication, LogLevel, PublicClientApplication } from '@azure/msal-browser';
import { environment } from 'src/environments/environment';
import { AuthorisationFactoryService } from 'src/authorisation/authorisation-factory-service';
import { provideNgxMask } from 'ngx-mask';
import { BrowserModule } from '@angular/platform-browser';
import 'chartjs-adapter-date-fns';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

declare global {
  interface Window {
    appRef: ApplicationRef,
    Cypress: any,
    authorisationFactoryService: AuthorisationFactoryService
  }
}

export function initConfig(appConfig: EnvironmentService) {
  return () => appConfig.loadConfig();
}

export function MSALInstanceFactory(appConfig: EnvironmentService): IPublicClientApplication {
  if (!appConfig.authEnable) {
    return new PublicClientApplication({
      auth: {
        clientId: "null"
      }
    });
  }

  if (appConfig.clientId == undefined) {
    throw new Error("/assets/config/oauth-oidc.json/clientId cannot be null when using AzureAD authentication mode");
  }

  const msalConfig: Configuration = {
    auth: {
      clientId: appConfig.clientId,
      authority: "https://login.microsoftonline.com/" + appConfig.tenantId,
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

@NgModule({
  imports: [BrowserModule], providers: [
    provideAppInitializer(() => {
      const initializerFn = (initConfig)(inject(EnvironmentService));
      return initializerFn();
    }),
    {
      provide: MSAL_INSTANCE,
      useFactory: MSALInstanceFactory,
      deps: [EnvironmentService]
    },
    MsalService,
    provideNgxMask(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptorsFromDi()),
    provideCharts(withDefaultRegisterables()),
    {
      provide: 'IErabliereApi',
      useClass: ErabliereApi
    }
  ]
})
export class AppModule implements DoBootstrap {
  constructor() { }

  ngDoBootstrap(appRef: ApplicationRef): void {
    appRef.bootstrap(AppComponent);

    if (!environment.production) {
      if (window.Cypress) {
        window.appRef = appRef;
      }
    }
  }
}
