import { Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { DonneesComponent } from 'src/app/client-view/donnees/donnees.component';
import { BarilsComponent } from 'src/app/client-view/barils/barils.component';
import { ActivatedRoute } from '@angular/router';
import { Erabliere } from 'src/model/erabliere';
import { Subject } from 'rxjs';
import { ImagePanelComponent } from 'src/app/client-view/donnees/sub-panel/image-panel.component';
import { RappelsComponent } from 'src/app/client-view/rappel/rappels.component';
import { WeatherForecastComponent } from 'src/app/client-view/donnees/weather-forecast.component';
import { HourlyWeatherForecastComponent } from 'src/app/client-view/donnees/hourly-weather-forecast';
import { CapteurPanelsComponent } from 'src/app/client-view/donnees/sub-panel/capteur-panels.component';
import { AuthorisationFactoryService } from 'src/core/authorisation/authorisation-factory-service';
import { IAuthorisationSerivce } from 'src/core/authorisation/iauthorisation-service';

@Component({
  selector: 'erablieres',
  templateUrl: 'erabliere.component.html',
  imports: [
    DonneesComponent,
    CapteurPanelsComponent,
    BarilsComponent,
    ImagePanelComponent,
    RappelsComponent,
    WeatherForecastComponent,
    HourlyWeatherForecastComponent
  ]
})
export class ErabliereComponent implements OnInit {
  idErabliereSelectionee?: any;
  erabliere?: Erabliere;
  resetErabliere: Subject<Erabliere> = new Subject<Erabliere>();

  displayCapteurs: boolean = false;
  displayImages: boolean = false;

  private readonly authSvc: IAuthorisationSerivce;

  constructor(
    private readonly _api: ErabliereApi,
    private readonly route: ActivatedRoute,
    private readonly authSvcFactory: AuthorisationFactoryService) {
    this.authSvc = this.authSvcFactory.getAuthorisationService();
    this.route.paramMap.subscribe(params => {
      this.idErabliereSelectionee = params.get('idErabliereSelectionee');
      this.getErabliere();
    });
    this.resetErabliere.subscribe((erabliere) => {
      this.erabliere = erabliere;
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.idErabliereSelectionee = params.get('idErabliereSelectionee');
      if (this.idErabliereSelectionee) {
        this._api.getImages(this.idErabliereSelectionee, 1).then((images) => {
          this.displayImages = images.length > 0;
        });
      }
    });

    this.authSvc.loginChanged.subscribe(isLoggedIn => {
      this._api.getErablieres(1).then((erablieres) => {
        if (erablieres.length == 0) {
          this.idErabliereSelectionee = undefined;
          this.erabliere = new Erabliere();
          this.resetErabliere.next(this.erabliere);
          this.displayCapteurs = false;
          this.displayImages = false;
        }
        else {
          this.resetErabliere.next(this.erabliere ?? new Erabliere());
        }
      }).catch((error) => {
        console.error('Error fetching erablieres:', error)
        this.idErabliereSelectionee = undefined;
        this.erabliere = new Erabliere();
        this.resetErabliere.next(this.erabliere);
        this.displayCapteurs = false;
        this.displayImages = false;
      });
    });
  }

  getErabliere() {
    this._api.getErabliere(this.idErabliereSelectionee).then((erabliere) => {
        this.erabliere = erabliere;
        this.resetErabliere.next(erabliere);
        this.displayCapteurs = !!(this.erabliere.capteurs?.find(capteur => capteur.afficherCapteurDashboard));
      }).catch((error) => {
        console.error('Error fetching erabliere:', error)
        this.erabliere = undefined;
        this.resetErabliere.next(new Erabliere());
        this.displayCapteurs = false;
        this.displayImages = false;
      });
  }

  getClassPanneauImage(arg0: Erabliere | undefined) {
    if (arg0?.dimensionPanneauImage) {
      return 'col-md-' + arg0.dimensionPanneauImage;
    }

    return 'col-md-12';
  }
}
