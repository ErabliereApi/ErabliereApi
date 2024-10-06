import { Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import * as mapboxgl from 'mapbox-gl';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';

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

    constructor(private readonly _api: ErabliereApi) { }

    map: mapboxgl.Map | undefined;
    style = 'mapbox://styles/mapbox/streets-v11';
    lat: number = 46.829853;
    lng: number = -71.254028;

    async ngOnInit(): Promise<void> {
        const accessToken = await this._api.getMapAccessToken("mapbox");

        if (this.map != null) {
            this.map.remove();
        }

        this.map = new mapboxgl.Map({
            accessToken: accessToken.accessToken,
            container: 'map',
            style: this.style,
            zoom: 7,
            center: [this.lng, this.lat]
        });

        const erabliereGeoJson = await this._api.getErablieresGeoJson(this.publicFilter == "yes", this.myErablieresFilter == "yes", this.sensorFilter, this.maxSensors);

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

                    console.log(props);

                    console.log(props?.capteur);
                    
                    let caps = JSON.parse(props?.capteur);

                    console.log(caps);

                    let captText = caps != null && caps.length > 0 ?
                    `<p>${caps[0].nom}: ${caps[0].valeur} ${caps[0].symbole}</p>` : '';

                    description = `<div>
                        <h3>${props?.name}</h3>
                        ${captText}
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
    sensorFilter: string = 'Essences';
    maxSensors: number | null = null;

    selectSensorOption(option: string) {
        this.sensorFilter = option;
    }

    async applyFilters() {
        console.log("Applying filters", this.publicFilter, this.myErablieresFilter, this.sensorFilter, this.maxSensors);
        await this.ngOnInit();
    }

    updatePublicFilter(event: any) {
        let element = event.target as HTMLSelectElement;

        this.publicFilter = element.value;
    }

    updateMyFilter(event: any) {
        let element = event.target as HTMLSelectElement;

        this.myErablieresFilter = element.value;
    }
}