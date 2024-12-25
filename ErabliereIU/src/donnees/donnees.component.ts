import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { ChartDataset, ChartType } from 'chart.js';
import { Subject } from 'rxjs';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { Erabliere } from 'src/model/erabliere';
import { BarPanelComponent } from './sub-panel/bar-panel.component';
import { VacuumGraphPanelComponent } from './sub-panel/vacuum-graph-panel.component';
import { calculerMoyenne } from './util';
import { GraphPanelComponent } from './sub-panel/graph-panel.component';

@Component({
    selector: 'donnees-panel',
    templateUrl: './donnees.component.html',
    imports: [
        GraphPanelComponent,
        VacuumGraphPanelComponent,
        BarPanelComponent,
    ]
})
export class DonneesComponent implements OnInit {
  @ViewChild('temperatureGraphPannel') temperatureGraphPannel?: GraphPanelComponent
  @ViewChild('vacciumGraphPannel') vacciumGraphPannel?: GraphPanelComponent
  @ViewChild('niveaubassinGraphPannel') niveaubassinGraphPannel?: GraphPanelComponent
  @ViewChild('dompeuxGraphPannel') dompeuxGraphPannel?: GraphPanelComponent

  intervalRequetes?: any

  @Input() initialErabliere?: Erabliere
  @Input() erabliereSubject: Subject<Erabliere> = new Subject<Erabliere>()
  @Input() dureeDonneesRequete: any

  timeaxes: string[] = [];
  timeaxes_dompeux: string[] = []

  derniereDonneeRecu?: string = undefined;
  ddr?: string = undefined;
  dernierDompeuxRecu?: string = undefined;
  ddrDompeux?: string = undefined;

  ids: Array<number> = []
  idsDompeux: Array<number> = []

  titre_temperature = "Temperature"
  temperature: ChartDataset[] = []
  temperatureValueActuel?: string | null
  temperatureSymbole: string = "°C"
  meanTemperature?: string

  titre_vaccium = "Vacuum"
  vaccium: ChartDataset[] = []
  vacciumValueActuel?: string | null
  vacciumSymbole: string = "HG"
  meanVaccium?: string

  titre_niveaubassin = "Niveau Bassin"
  niveaubassin: ChartDataset[] = []
  niveauBassinValueActuel?: string | null
  niveauBassinSymbole: string = "%"
  meanNiveauBassin?: string

  titre_dompeux = "Dompeux"
  dompeux: ChartDataset[] = []
  dompeux_line_type: ChartType = "bar"

  erabliereAfficherTrioDonnees: boolean | undefined;
  erabliereAfficherSectionDompeux: boolean | undefined;
  erabliereId: any;

  constructor(private readonly _erabliereApi: ErabliereApi) { }

  ngOnInit() {
    this.erabliereSubject.subscribe(response => {
      this.ngOnDestroy();
      this.ddr = undefined;
      this.ddrDompeux = undefined;
      this.dernierDompeuxRecu = undefined;
      this.derniereDonneeRecu = undefined;

      if (response != undefined) {
        this.erabliereAfficherTrioDonnees = response.afficherTrioDonnees;
        this.erabliereAfficherSectionDompeux = response.afficherSectionDompeux;
        this.erabliereId = response.id;
      }
      else {
        this.erabliereAfficherTrioDonnees = undefined;
        this.erabliereAfficherSectionDompeux = undefined;
        this.erabliereId = undefined;
      }

      this.fetchDataAndBuildGraph();
    });

    this.erabliereAfficherTrioDonnees = this.initialErabliere?.afficherTrioDonnees;
    this.erabliereAfficherSectionDompeux = this.initialErabliere?.afficherSectionDompeux;
    this.erabliereId = this.initialErabliere?.id;

    this.fetchDataAndBuildGraph();
  }

  fetchDataAndBuildGraph() {
    if (this.erabliereAfficherTrioDonnees) {
      this.doHttpCall();
    }
    if (this.erabliereAfficherSectionDompeux) {
      this.doHttpCallDompeux();
    }

    this.intervalRequetes = setInterval(() => {
      if (!this.fixRange) {
        if (this.erabliereAfficherTrioDonnees) {
          this.doHttpCall();
        }
      }
      if (this.erabliereAfficherSectionDompeux) {
        this.doHttpCallDompeux();
      }
    }, 1000 * 60);
  }

  ngOnDestroy() {
    clearInterval(this.intervalRequetes);
  }

  doHttpCallDompeux() {
    let debutFiltre = this.obtenirDebutFiltre().toISOString();
    let finFiltre = new Date().toISOString();

    let xddr = null;
    if (this.dernierDompeuxRecu != undefined) {
      xddr = this.dernierDompeuxRecu.toString();
    }

    this._erabliereApi.getDompeux(this.erabliereId, debutFiltre, finFiltre, xddr)
      .then(resp => {
        const h = resp.headers;

        this.dernierDompeuxRecu = h.get("x-dde")?.valueOf();
        this.ddrDompeux = h.get("x-ddr")?.valueOf();

        let e = resp.body;

        if (e == null) {
          return;
        }

        let idsDompeux = e.map(ee => ee.id);

        let dompeux = [
          { data: e.map(ee => (new Date(ee.df).getTime() - new Date(ee.dd).getTime()) / 1000), label: 'Durée en seconde' }
        ];

        let timeaxes_dompeux = e.map(ee => new Date(ee.t).toLocaleTimeString());

        if (h.has("x-ddr") && this.ddrDompeux != undefined && h.get("x-ddr")?.valueOf() == this.ddrDompeux) {

          if (idsDompeux.length > 0 && this.idsDompeux[this.idsDompeux.length - 1] === idsDompeux[0]) {
            dompeux[0].data.shift();
            timeaxes_dompeux.shift();
            idsDompeux.shift();
          }

          dompeux[0].data.forEach((d: number) => this.dompeux[0].data?.push(d));
          timeaxes_dompeux.forEach((t: string) => this.timeaxes_dompeux?.push(t));
          idsDompeux.forEach((n: number) => this.idsDompeux.push(n));

          while (this.timeaxes_dompeux.length > 0 &&
            new Date(this.timeaxes_dompeux[0].toString()) < new Date(debutFiltre)) {
            this.timeaxes_dompeux.shift();
            this.dompeux[0].data?.shift();
            this.idsDompeux.shift();
          }
        }
        else {
          this.dompeux = dompeux;
          this.timeaxes_dompeux = timeaxes_dompeux;
          this.idsDompeux = idsDompeux;
        }

        this.dompeuxGraphPannel?.chart?.update();
      });
  }

  doHttpCall() {
    let debutFiltre = this.obtenirDebutFiltre().toISOString();
    let finFiltre = new Date().toISOString();

    if (this.fixRange) {
      if (this.dateDebutFixRange != undefined) {
        debutFiltre = this.dateDebutFixRange;
      }
      if (this.dateFinFixRange != undefined) {
        finFiltre = this.dateFinFixRange;
      }
    }

    let xddr = null;
    if (this.derniereDonneeRecu != undefined) {
      xddr = this.derniereDonneeRecu.toString();
    }

    this._erabliereApi.getDonnees(this.erabliereId, debutFiltre, finFiltre, xddr)
      .then(reponse => {
        let h = reponse.headers;

        this.derniereDonneeRecu = h.get("x-dde")?.valueOf();
        this.ddr = h.get("x-ddr")?.valueOf();

        let e = reponse.body;

        if (e == null) {
          return;
        }

        let ids = e.map(ee => ee.id);

        let temperature: Array<ChartDataset> = [
          {
            data: e.map(ee => ee.t != null ? ee.t / 10 : null),
            label: 'Temperature',
            fill: true,
            pointBackgroundColor: 'rgba(255,255,0,0.8)',
            pointBorderColor: 'black',
            tension: 0.5
          }
        ];
        let vaccium: Array<ChartDataset> = [
          {
            data: e.map(ee => ee.v != null ? ee.v / 10 : null),
            label: 'Vaccium',
            fill: true,
            pointBackgroundColor: 'rgba(255,255,0,0.8)',
            pointBorderColor: 'black',
            tension: 0.5
          }
        ];
        let niveaubassin: Array<ChartDataset> = [
          {
            data: e.map(ee => ee.nb ?? null),
            label: 'Niveau bassin',
            fill: true,
            pointBackgroundColor: 'rgba(255,255,0,0.8)',
            pointBorderColor: 'black',
            tension: 0.5
          }
        ];

        let timeaxes = e.map(ee => ee.d);

        if (e.length > 0) {
          let tva = e[e.length - 1].t;
          let vva = e[e.length - 1].v;
          this.temperatureValueActuel = tva != null ? (tva / 10).toFixed(1) : null;
          this.vacciumValueActuel = vva != null ? (vva / 10).toFixed(1) : null;
          this.niveauBassinValueActuel = e[e.length - 1].nb?.toString();
        }
        else if (e.length == 0) {
          this.temperatureValueActuel = null;
          this.vacciumValueActuel = null;
          this.niveauBassinValueActuel = null;
        }

        if (h.has("x-ddr") && this.ddr != undefined && h.get("x-ddr")?.valueOf() == this.ddr) {

          if (ids.length > 0 && this.ids[this.ids.length - 1] === ids[0]) {
            this.temperature[0].data?.pop();
            this.vaccium[0].data?.pop();
            this.niveaubassin[0].data?.pop();
            this.timeaxes.pop();

            this.temperature[0].data?.push(temperature[0].data.shift() as any);
            this.vaccium[0].data?.push(vaccium[0].data.shift() as any);
            this.niveaubassin[0].data?.push(niveaubassin[0].data.shift() as any);
            this.timeaxes.push(timeaxes.shift() as any);
          }

          temperature[0].data.forEach(t => this.temperature[0].data?.push(t as any));
          vaccium[0].data.forEach(v => this.vaccium[0].data?.push(v as any));
          niveaubassin[0].data.forEach(nb => this.niveaubassin[0].data?.push(nb as any));
          timeaxes.forEach(t => this.timeaxes.push(t as any));
          ids.forEach((n: number) => this.ids.push(n));

          while (this.timeaxes.length > 0 &&
            new Date(this.timeaxes[0].toString()) < new Date(debutFiltre)) {
            this.timeaxes.shift();
            this.temperature[0].data?.shift();
            this.vaccium[0].data?.shift();
            this.niveaubassin[0].data?.shift();
            this.ids.shift();
          }
        }
        else {
          this.temperature = temperature;
          this.vaccium = vaccium;
          this.niveaubassin = niveaubassin;
          this.timeaxes = timeaxes as any[];
          this.ids = ids;
        }

        this.temperatureGraphPannel?.chart?.update();
        this.vacciumGraphPannel?.chart?.update();
        this.niveaubassinGraphPannel?.chart?.update();

        if (this.fixRange) {
          this.meanTemperature = "Moyenne: " + calculerMoyenne(this.temperature[0]) + " " + this.temperatureSymbole;
          this.meanVaccium = "Moyenne: " + calculerMoyenne(this.vaccium[0]) + " " + this.vacciumSymbole;
          this.meanNiveauBassin = "Moyenne: " + calculerMoyenne(this.niveaubassin[0]) + " " + this.niveauBassinSymbole;
        }
        else {
          this.meanNiveauBassin = undefined;
          this.meanTemperature = undefined;
          this.meanVaccium = undefined;
        }
      })
      .catch(reason => {
        console.log(reason);
        this.derniereDonneeRecu = undefined;
        this.ddr = undefined;
      });
  }

  duree: string = "12h"
  debutEnHeure: number = 12;

  obtenirDebutFiltre(): Date {
    let twelve_hour = 1000 * 60 * 60 * this.debutEnHeure;

    return new Date(Date.now() - twelve_hour);
  }

  updateGraph($event: any): void {
    this.fixRange = false;
    this.duree = "";

    if ($event.days != 0) {
      this.duree = $event.days + " jours";
    }

    if ($event.hours != 0) {
      this.duree = this.duree + " " + $event.hours + "h";
    }

    this.updateDuree(this.duree);

    this.debutEnHeure = $event.hours + (24 * $event.days);

    this.cleanGraphComponentCache();

    this.doHttpCall();
  }

  updateDuree(duree: string) {
    this.duree = duree;
    this.temperatureGraphPannel?.updateDuree(this.duree);
    this.vacciumGraphPannel?.updateDuree(this.duree);
    this.niveaubassinGraphPannel?.updateDuree(this.duree);
  }

  private cleanGraphComponentCache() {
    this.derniereDonneeRecu = undefined;
    this.ddr = undefined;
    this.ids = [];
  }

  fixRange: boolean = false;
  dateDebutFixRange?: string = undefined
  dateFinFixRange?: string = undefined

  updateGraphUsingFixRangeCallback($event: any): void {
    this.fixRange = true;
    this.dateDebutFixRange = $event.dateDebutFixRange;
    this.dateFinFixRange = $event.dateFinFixRange;
    this.cleanGraphComponentCache();
    this.updateDuree(this.dateDebutFixRange + " - " + this.dateFinFixRange);
    this.doHttpCall();
  }
}
