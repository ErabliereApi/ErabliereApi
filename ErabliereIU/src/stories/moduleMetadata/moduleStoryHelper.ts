import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { provideRouter } from "@angular/router";
import { MSAL_INSTANCE, MsalService } from "@azure/msal-angular";
import { applicationConfig } from "@storybook/angular";
import { provideNgxMask } from "ngx-mask";
import { MSALInstanceFactory } from "src/app/app.config";
import { EnvironmentService } from "src/environments/environment.service";
import { StorybookMockErabliereApiFn } from "../mock/StorybookMockErabliereApi";
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

export class ModuleStoryHelper{
    static getErabliereApiStoriesApplicationConfig() {
        return applicationConfig({
            providers: [
                provideRouter([]),
                provideHttpClient(withInterceptors([StorybookMockErabliereApiFn])), 
                provideNgxMask(),
                provideCharts(withDefaultRegisterables()),
                MsalService,
                {
                  provide: MSAL_INSTANCE,
                  useFactory: MSALInstanceFactory,
                  deps: [EnvironmentService]
                }
            ]
        });
    }
}
