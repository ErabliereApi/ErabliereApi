import { Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { DonneesComponent } from 'src/donnees/donnees.component';
import { BarilsComponent } from 'src/barils/barils.component';
import { ActivatedRoute } from '@angular/router';
import { Erabliere } from 'src/model/erabliere';
import { Subject } from 'rxjs';
import { ImagePanelComponent } from 'src/donnees/sub-panel/image-panel.component';
import { RappelsComponent } from 'src/rappel/rappels.component';
import { WeatherForecastComponent } from 'src/donnees/weather-forecast.component';
import { HourlyWeatherForecastComponent } from 'src/donnees/hourly-weather-forecast';
import { CapteurPanelsComponent } from 'src/donnees/sub-panel/capteur-panels.component';

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

  constructor(private readonly _api: ErabliereApi, private readonly route: ActivatedRoute) {
    this.route.paramMap.subscribe(params => {
      this.idErabliereSelectionee = params.get('idErabliereSelectionee');
      if (this.idErabliereSelectionee) {
        this._api.getErabliere(this.idErabliereSelectionee).then((erabliere) => {
          this.erabliere = erabliere;
          this.resetErabliere.next(erabliere);
          this.displayCapteurs = !!(this.erabliere.capteurs?.find(capteur => capteur.afficherCapteurDashboard));
        });
      }
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
  }

  getClassPanneauImage(arg0: Erabliere|undefined) {
    if (arg0?.dimensionPanneauImage) {
      return 'col-md-' + arg0.dimensionPanneauImage;
    }

    return 'col-md-12';
  }
}
