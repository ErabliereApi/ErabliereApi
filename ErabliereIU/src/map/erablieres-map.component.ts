import { Component, OnInit } from '@angular/core';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import * as mapboxgl from 'mapbox-gl';

@Component({
    selector: 'app-erablieres-map',
    template: `
    <div class="container-fluid" style="height: max-content;">
        <div id="map" class="match-parent"></div>
    </div>
    `,
    standalone: true,
    imports: [],
    styles: [`.match-parent {
        width: 100%;
        height: 700px;
        }`]
})
export class ErablieresMapComponent implements OnInit {

    constructor(private readonly _api: ErabliereApi) { }

    map: mapboxgl.Map | undefined;
    style = 'mapbox://styles/mapbox/streets-v11';
    lat: number = 46.829853;
    lng: number = -71.254028;

    async ngOnInit(): Promise<void> {
        const accessToken = await this._api.getMapAccessToken("mapbox");

        this.map = new mapboxgl.Map({
            accessToken: accessToken.accessToken,
            container: 'map',
            style: this.style,
            zoom: 7,
            center: [this.lng, this.lat]
        });

        const erabliereGeoJson = await this._api.getErablieresGeoJson();

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
        });
    }
}