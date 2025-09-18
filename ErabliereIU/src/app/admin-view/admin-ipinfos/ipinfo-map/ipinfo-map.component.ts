import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { IpInfo } from 'src/model/ipinfo';


@Component({
    selector: 'app-ipinfo-map',
    templateUrl: './ipinfo-map.component.html',
    styleUrls: ['./ipinfo-map.component.css']
})
export class IpinfoMapComponent implements OnInit, OnDestroy {
    @Input() ipInfos: IpInfo[] = [];
    @Input() authorizeCountries: string[] = [];
    map?: mapboxgl.Map;
    style = 'mapbox://styles/mapbox/light-v10';
    token?: string = 'YOUR_MAPBOX_ACCESS_TOKEN';
    missingCountries: string[] = [];

    constructor(private readonly api: ErabliereApi) { }

    ngOnInit(): void {
        this.api.getMapAccessToken("mapbox").then(t => {
            this.token = t.accessToken;
            console.log("Mapbox token loaded");
            if (!this.token) {
                console.error("Mapbox token is empty");
                return;
            }
            this.initMap();
        }).catch(() => {
            this.token = '';
        });
    }

    ngOnDestroy(): void {
        if (this.map) this.map.remove();
    }

    initMap() {
        if (this.map != null) {
            this.map.remove();
        }

        this.map = new mapboxgl.Map({
            accessToken: this.token,
            container: 'mapipinfo',
            style: this.style,
            zoom: 1.5,
            center: [0, 20],
        });

        this.map.on('load', async () => {
            if (this.map == null) return;

            // 1) agréger les ipInfos par pays
            const agg = this.aggregateByCountry(this.ipInfos);

            // 2) charger un GeoJSON world-countries (Topology/GeoJSON)
            const geoJsonUrl = '/assets/countries.geo.json';
            const res = await fetch(geoJsonUrl);
            const world = await res.json();
            const worldFiltered: any = {
                type: 'FeatureCollection',
                features: []
            };

            // 2.5 vérifier les pays manquants
            this.missingCountries = [];
            for (const code of Object.keys(agg)) {
                const found = world.features.find((f: any) => f.properties.name === code);
                if (!found) {
                    this.missingCountries.push(code);
                }
            }
            if (this.missingCountries.length) {
                console.warn("Missing countries in GeoJSON:", this.missingCountries);
            }

            // 3) injecter count et color dans les propriétés des features
            const counts = Object.values(agg).map(v => v.ipCount);
            const min = counts.length ? Math.min(...counts) : 0;
            const max = counts.length ? Math.max(...counts) : 1;

            for (const f of world.features) {
                const code = f.properties.name;

                if (agg.hasOwnProperty(code)) {
                    f.properties.count = agg[code];
                    f.properties.color = this.countToColor(agg[code], min, max);
                    worldFiltered.features.push(f);
                }
            }

            // 4) ajouter la source et la couche
            this.map.addSource('countries', {
                type: 'geojson',
                data: worldFiltered
            });

            // couche fill (pays colorés)
            this.map.addLayer({
                id: 'country-fills',
                type: 'fill',
                source: 'countries',
                paint: {
                    'fill-color': ['get', 'color'],
                    'fill-opacity': 0.8
                }
            });

            // bordures
            this.map.addLayer({
                id: 'country-borders',
                type: 'line',
                source: 'countries',
                paint: {
                    'line-color': '#ffffff',
                    'line-width': 0.5
                }
            });

            let popup: mapboxgl.Popup | undefined = undefined;

            // zoom/hover tooltip
            this.map.on('mousemove', 'country-fills', (e) => {
                if (popup) {
                    popup.remove();
                    popup = undefined;
                }
                const props = e.features?.[0]?.properties;
                if (!props) return;
                const coordinates = (e.lngLat).toArray();
                const name = props.name;
                const count = agg[name]?.ipCount || 0;
                const html = `<strong>${name}</strong><br/>Nombre d'adresses IP: ${count}<br/>Long/Lat: ${coordinates[0].toFixed(4)}, ${coordinates[1].toFixed(4)}`;
                popup = new mapboxgl.Popup({
                        closeButton: false,
                        closeOnClick: false
                    })
                    .setLngLat(coordinates)
                    .setHTML(html)
                    .addTo(this.map!);
            });
        });
    }

    aggregateByCountry(list: IpInfo[]) {
        const agg: { [k: string]: { ipCount: number, countryCode: string } } = {};
        for (const item of list) {
            let code = item.country; // nom complet
            if (!code) continue;
            if (agg.hasOwnProperty(code)) {
                agg[code].ipCount += 1;
            }
            else {
                agg[code] = {
                    ipCount: 1,
                    countryCode: item.countryCode
                }
            }
        }
        return agg;
    }

    // Si le code de pays est dans les pays autorisés, on colore en vert, sinon en rouge
    countToColor(value: { ipCount: number, countryCode: string }, min: number, max: number): string {
        if (value.ipCount <= 0) return '#eeeeee';
        let h = 120; // vert
        let s = 90; // 90% saturation
        let l = 50; // 50% lightness
        if (this.authorizeCountries.length > 0 && !this.authorizeCountries.includes(value.countryCode)) {
            h = 0; // rouge
        }
        if (max === min) {
            // tout le monde a la même valeur
            return this.hslToHex(h, s, l);
        }
        const t = (value.ipCount - min) / (max - min);
        s = Math.min(s * t + 20, 100); // entre 20% et 100%
        l = Math.min(l * t + 50, 50); // entre 50% et 90%
        return this.hslToHex(h, s, l);
    }

    hslToHex(h: number, s: number, l: number) {
        s /= 100;
        l /= 100;
        const c = (1 - Math.abs(2 * l - 1)) * s;
        const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
        const m = l - c / 2;
        let r = 0, g = 0, b = 0;
        if (0 <= h && h < 60) { r = c; g = x; }
        else if (60 <= h && h < 120) { r = x; g = c; }
        else if (120 <= h && h < 180) { g = c; b = x; }
        else if (180 <= h && h < 240) { g = x; b = c; }
        else if (240 <= h && h < 300) { r = x; b = c; }
        else { r = c; b = x; }
        const R = Math.round((r + m) * 255);
        const G = Math.round((g + m) * 255);
        const B = Math.round((b + m) * 255);
        return '#' + ((1 << 24) + (R << 16) + (G << 8) + B).toString(16).slice(1);
    }
}