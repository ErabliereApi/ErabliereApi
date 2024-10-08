import { Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import * as mapboxgl from 'mapbox-gl';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IAuthorisationSerivce } from 'src/authorisation/iauthorisation-service';
import { AuthorisationFactoryService } from 'src/authorisation/authorisation-factory-service';

@Component({
    selector: 'app-erablieres-map',
    templateUrl: './erablieres-map.component.html',
    standalone: true,
    imports: [
        NgIf,
        FormsModule,
        NgFor
    ],
    styles: [
        `.match-parent {
            width: 100%;
            height: 700px;
        }
        #map {
            position: relative;
        }
        .mapboxgl-popup {
            max-width: 400px;
            font: 12px/20px 'Helvetica Neue', Arial, Helvetica, sans-serif;
        }
        `
    ]
})
export class ErablieresMapComponent implements OnInit {

    private readonly _authService: IAuthorisationSerivce

    constructor(authFactoryService: AuthorisationFactoryService, private readonly _api: ErabliereApi) { 
        this._authService = authFactoryService.getAuthorisationService();

        this._authService.loginChanged.subscribe(isLoggedIn => {
            this.isAuthenticated = isLoggedIn;
            this.myErablieresFilter = isLoggedIn ? 'yes' : 'no';
        });
    }

    map: mapboxgl.Map | undefined;
    style = 'mapbox://styles/mapbox/streets-v11';
    lat: number = 46.829853;
    lng: number = -71.254028;

    loadingInProgress = true;
    nombreElements = 0;
    duration = 0;
    error?: string;

    isAuthenticated = false;

    async ngOnInit(): Promise<void> {
        this._authService.isLoggedIn().then(isLoggedIn => {
            this.isAuthenticated = isLoggedIn;
            this.myErablieresFilter = isLoggedIn ? 'yes' : 'no';
        });

        await this.reInitMap();
    }

    async reInitMap() {
        this.loadingInProgress = true;
        this.nombreElements = 0;
        this.duration = 0;
        let dur = Date.now();
        const accessToken = await this._api.getMapAccessToken("mapbox");

        if (this.map != null) {
            this.map.remove();
        }

        let erabliereGeoJson: any = null;

        try {
            console.log(this.publicFilter);
            erabliereGeoJson = await this._api.getErablieresGeoJson(
                this.publicFilter == "yes", 
                this.myErablieresFilter == "yes", 
                this.sensorFilter, 
                this.maxSensors);

            this.error = undefined;
            this.nombreElements = erabliereGeoJson.features.length;
        }
        catch (e) {
            this.error = "Erreur lors de la récupération des érablières";
            console.error("Error while fetching erablieres", e);
            this.nombreElements = 0;
        }
        
        this.loadingInProgress = false;
        
        this.duration = Date.now() - dur;

        this.duration = this.duration / 1000;

        this.map = new mapboxgl.Map({
            accessToken: accessToken.accessToken,
            container: 'map',
            style: this.style,
            zoom: 7,
            center: [this.lng, this.lat]
        });

        this.map.on('load', () => {
            if (this.map == null) {
                return;
            }

            this.map.addSource('erablieres', {
                type: 'geojson',
                data: erabliereGeoJson
            });

            this.map.addLayer({
                id: 'erablieres',
                type: 'circle',
                source: 'erablieres',
                paint: {
                    'circle-radius': 6,
                    'circle-color': '#B42222'
                }
            });

            // Change the cursor to a pointer when the mouse is over the erabliere layer.
            this.map.on('mouseenter', 'erablieres', () => {
                if (this.map == null) {
                    return;
                }

                this.map.getCanvas().style.cursor = 'pointer';
            });

            // Change it back to a pointer when it leaves.
            this.map.on('mouseleave', 'erablieres', () => {
                if (this.map == null) {
                    return;
                }

                this.map.getCanvas().style.cursor = '';
            });

            // add popup for each erabliere
            this.map.on('click', 'erablieres', (e) => {
                if (this.map == null) {
                    return;
                }

                const coordinates = e.lngLat;
                let description = '';
                if (e.features != null && e.features.length > 0) {
                    let props = e.features[0]?.properties;
                    
                    let caps = JSON.parse(props?.capteur);

                    let captsText = '';

                    for (let cap of caps) {
                        captsText += `<p>${cap.nom}: ${cap.valeur} ${cap.symbole}</p>`;
                    }

                    description = `<div>
                        <h3>${props?.name}</h3>
                        ${captsText}
                        </div>
                    `;
                }

                new mapboxgl.Popup()
                    .setLngLat(coordinates)
                    .setHTML(description)
                    .addTo(this.map);
            });
        });
    }

    publicFilter: string = 'yes';
    myErablieresFilter: string = 'yes';
    sensorFilter: string = '';
    maxSensors: number | null = null;

    selectSensorOption(option: string) {
        this.sensorFilter = option;
    }

    async applyFilters() {
        await this.reInitMap();
    }

    updatePublicFilter(event: any) {
        let element = event.target as HTMLSelectElement;

        this.publicFilter = element.value;

        console.log(this.publicFilter);
    }

    updateMyFilter(event: any) {
        let element = event.target as HTMLSelectElement;

        this.myErablieresFilter = element.value;
    }
}