import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, OnChanges, OnDestroy, OnInit, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { EModalComponent } from 'src/generic/modal/emodal.component';
import { LigneTubelure, TYPES_LIGNE } from 'src/model/ligneTubelure';
import { Arbre } from 'src/model/arbre';
import { Entaille } from 'src/model/entaille';

type ModeTubelure = 'idle' | 'ligne' | 'arbre' | 'entaille';

const COULEURS_LIGNE: Record<string, string> = {
    principale: '#d62728',
    secondaire: '#ff7f0e',
    laterale: '#1f77b4'
};

const LIBELLES_LIGNE: Record<string, string> = {
    principale: 'Ligne principale',
    secondaire: 'Ligne secondaire',
    laterale: 'Ligne latérale'
};

@Component({
    selector: 'app-tubelure',
    templateUrl: './tubelure.component.html',
    styleUrls: ['./tubelure.component.css'],
    imports: [
        DecimalPipe,
        FormsModule,
        EModalComponent
    ],
    changeDetection: ChangeDetectionStrategy.Eager
})
export class TubelureComponent implements OnInit, OnChanges, OnDestroy {

    @Input() idErabliereSelectionee?: any;

    constructor(private readonly _api: ErabliereApi) { }

    map?: mapboxgl.Map;
    private _mapboxgl?: any;
    private _mapChargee = false;
    private _geoJsonReseau: any = { type: 'FeatureCollection', features: [] };
    private _popup?: any;

    typesLigne = TYPES_LIGNE;
    couleursLigne = COULEURS_LIGNE;
    libellesLigne = LIBELLES_LIGNE;

    loadingInProgress = true;
    error?: string;

    // Machine à états de saisie
    mode: ModeTubelure = 'idle';
    afficherChoixType = false;
    typeLigneEnCours: string = 'laterale';
    nomLigneEnCours: string = '';
    pointsEnCours: [number, number][] = [];
    ligneEnEditionId?: string;
    enregistrementEnCours = false;

    // GPS
    gpsFix?: { lng: number, lat: number, accuracy: number };
    erreurGps?: string;

    // Modales
    modal: 'arbre' | 'entaille' | 'suppression' | null = null;
    formNom: string = '';
    formEspece: string = '';
    formLng?: number;
    formLat?: number;
    formIdArbre: string = '';
    formIdLigne: string = '';
    arbres: Arbre[] = [];
    lignes: LigneTubelure[] = [];
    featureASupprimer?: { type: string, id: string, nom?: string };

    ngOnInit(): void {
        this.initMap();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.idErabliereSelectionee && !changes.idErabliereSelectionee.firstChange) {
            this.annulerSaisie();
            this.chargerReseau(true);
        }
    }

    ngOnDestroy(): void {
        this.map?.remove();
    }

    async initMap() {
        try {
            const accessToken = await this._api.getMapAccessToken('mapbox');

            const mapboxgl = (await import('mapbox-gl')).default;
            this._mapboxgl = mapboxgl;

            this.map = new mapboxgl.Map({
                accessToken: accessToken.accessToken,
                container: 'map-tubelure',
                style: 'mapbox://styles/mapbox/outdoors-v12',
                zoom: 15,
                center: [-71.254028, 46.829853]
            });

            const geolocate = new mapboxgl.GeolocateControl({
                positionOptions: { enableHighAccuracy: true },
                trackUserLocation: true,
                showUserHeading: true,
                showAccuracyCircle: true
            });

            this.map.addControl(new mapboxgl.NavigationControl(), 'top-right');
            this.map.addControl(geolocate, 'top-right');

            geolocate.on('geolocate', (e: any) => {
                this.gpsFix = {
                    lng: e.coords.longitude,
                    lat: e.coords.latitude,
                    accuracy: e.coords.accuracy
                };
                this.erreurGps = undefined;
            });

            geolocate.on('error', (e: any) => {
                if (e?.code === 1) {
                    this.erreurGps = "Accès à la position refusé. Autorisez la géolocalisation dans votre navigateur pour utiliser le bouton GPS.";
                }
                else {
                    this.erreurGps = "Position GPS indisponible. Vous pouvez quand même placer des points en touchant la carte.";
                }
            });

            this.map.on('load', () => {
                if (this.map == null) {
                    return;
                }

                this.map.addSource('tubelure', {
                    type: 'geojson',
                    data: this._geoJsonReseau
                });

                this.map.addSource('edition', {
                    type: 'geojson',
                    data: { type: 'FeatureCollection', features: [] }
                });

                this.map.addLayer({
                    id: 'lignes',
                    type: 'line',
                    source: 'tubelure',
                    filter: ['==', ['get', 'feature'], 'ligne'],
                    layout: { 'line-cap': 'round', 'line-join': 'round' },
                    paint: {
                        'line-color': ['match', ['get', 'typeLigne'],
                            'principale', COULEURS_LIGNE['principale'],
                            'secondaire', COULEURS_LIGNE['secondaire'],
                            'laterale', COULEURS_LIGNE['laterale'],
                            '#888888'],
                        'line-width': ['match', ['get', 'typeLigne'],
                            'principale', 6,
                            'secondaire', 4,
                            3]
                    }
                });

                this.map.addLayer({
                    id: 'arbres',
                    type: 'circle',
                    source: 'tubelure',
                    filter: ['==', ['get', 'feature'], 'arbre'],
                    paint: {
                        'circle-radius': 8,
                        'circle-color': '#2ca02c',
                        'circle-stroke-width': 2,
                        'circle-stroke-color': '#ffffff'
                    }
                });

                this.map.addLayer({
                    id: 'entailles',
                    type: 'circle',
                    source: 'tubelure',
                    filter: ['==', ['get', 'feature'], 'entaille'],
                    paint: {
                        'circle-radius': 6,
                        'circle-color': '#8c564b',
                        'circle-stroke-width': 2,
                        'circle-stroke-color': '#ffffff'
                    }
                });

                this.map.addLayer({
                    id: 'edition-ligne',
                    type: 'line',
                    source: 'edition',
                    filter: ['==', ['geometry-type'], 'LineString'],
                    layout: { 'line-cap': 'round', 'line-join': 'round' },
                    paint: {
                        'line-color': ['get', 'couleur'],
                        'line-width': 4,
                        'line-dasharray': [2, 1.5]
                    }
                });

                this.map.addLayer({
                    id: 'edition-points',
                    type: 'circle',
                    source: 'edition',
                    filter: ['==', ['geometry-type'], 'Point'],
                    paint: {
                        'circle-radius': 10,
                        'circle-color': ['get', 'couleur'],
                        'circle-stroke-width': 2,
                        'circle-stroke-color': '#ffffff'
                    }
                });

                this.map.on('click', (e) => this.clicCarte(e));

                this.map.on('mousedown', 'edition-points', (e) => this.demarrerDragSommet(e));
                this.map.on('touchstart', 'edition-points', (e) => this.demarrerDragSommet(e));

                for (const layer of ['lignes', 'arbres', 'entailles']) {
                    this.map.on('mouseenter', layer, () => {
                        if (this.map != null && this.mode === 'idle') {
                            this.map.getCanvas().style.cursor = 'pointer';
                        }
                    });
                    this.map.on('mouseleave', layer, () => {
                        if (this.map != null) {
                            this.map.getCanvas().style.cursor = '';
                        }
                    });
                }

                this._mapChargee = true;
                this.chargerReseau(true);
                geolocate.trigger();
            });
        }
        catch (e) {
            console.error('Erreur lors de l\'initialisation de la carte', e);
            this.error = 'Erreur lors de l\'initialisation de la carte.';
            this.loadingInProgress = false;
        }
    }

    async chargerReseau(recentrer: boolean = false) {
        if (this.idErabliereSelectionee == null) {
            return;
        }

        this.loadingInProgress = true;

        try {
            this._geoJsonReseau = await this._api.getTubelureGeoJson(this.idErabliereSelectionee);
            this.error = undefined;
        }
        catch (e) {
            console.error('Erreur lors de la récupération du réseau de tubelure', e);
            this.error = 'Erreur lors de la récupération du réseau de tubelure.';
            this.loadingInProgress = false;
            return;
        }

        this.majSourceReseau();

        if (recentrer) {
            await this.recentrerCarte();
        }

        this.loadingInProgress = false;
    }

    private majSourceReseau() {
        if (!this._mapChargee || this.map == null) {
            return;
        }

        // La ligne en cours de modification est masquée du réseau affiché
        const data = this.ligneEnEditionId == null
            ? this._geoJsonReseau
            : {
                type: 'FeatureCollection',
                features: this._geoJsonReseau.features.filter((f: any) => f.properties?.id !== this.ligneEnEditionId)
            };

        (this.map.getSource('tubelure') as mapboxgl.GeoJSONSource)?.setData(data);
    }

    private async recentrerCarte() {
        if (this.map == null || this._mapboxgl == null) {
            return;
        }

        const bounds = new this._mapboxgl.LngLatBounds();
        let nbCoords = 0;

        for (const feature of this._geoJsonReseau.features ?? []) {
            if (feature.geometry.type === 'Point') {
                bounds.extend(feature.geometry.coordinates);
                nbCoords++;
            }
            else if (feature.geometry.type === 'LineString') {
                for (const coord of feature.geometry.coordinates) {
                    bounds.extend(coord);
                    nbCoords++;
                }
            }
        }

        if (nbCoords > 0) {
            this.map.fitBounds(bounds, { padding: 80, maxZoom: 17 });
            return;
        }

        try {
            const erabliere = await this._api.getErabliere(this.idErabliereSelectionee, true, true);

            if (erabliere?.latitude && erabliere?.longitude) {
                this.map.setCenter([erabliere.longitude, erabliere.latitude]);
                this.map.setZoom(15);
            }
        }
        catch (e) {
            console.error('Erreur lors de la récupération de l\'érablière pour centrer la carte', e);
        }
    }

    // ---- Interactions carte ----

    private clicCarte(e: any) {
        if (this.map == null) {
            return;
        }

        if (this.mode === 'ligne') {
            // Un tap sur un sommet existant ne doit pas ajouter de point (c'est un drag raté)
            const sommets = this.map.queryRenderedFeatures(e.point, { layers: ['edition-points'] });
            if (sommets.length > 0) {
                return;
            }

            this.ajouterPoint([e.lngLat.lng, e.lngLat.lat]);
        }
        else if (this.mode === 'arbre' || this.mode === 'entaille') {
            this.ouvrirModalPoint(e.lngLat.lng, e.lngLat.lat);
        }
        else {
            const features = this.map.queryRenderedFeatures(e.point, { layers: ['entailles', 'arbres', 'lignes'] });
            if (features.length > 0) {
                this.ouvrirPopupFeature(features[0], e.lngLat);
            }
        }
    }

    private ouvrirPopupFeature(feature: any, lngLat: any) {
        if (this.map == null || this._mapboxgl == null) {
            return;
        }

        const props = feature.properties ?? {};
        const type = props.feature;

        const div = document.createElement('div');

        const titre = document.createElement('h6');
        if (type === 'ligne') {
            titre.textContent = `${LIBELLES_LIGNE[props.typeLigne] ?? 'Ligne'}${props.nom ? ' « ' + props.nom + ' »' : ''}`;
        }
        else if (type === 'arbre') {
            titre.textContent = `Arbre${props.nom ? ' « ' + props.nom + ' »' : ''}${props.espece ? ' (' + props.espece + ')' : ''}`;
        }
        else {
            titre.textContent = `Entaille${props.nom ? ' « ' + props.nom + ' »' : ''}`;
        }
        div.appendChild(titre);

        const boutons = document.createElement('div');
        boutons.className = 'd-flex gap-2';

        if (type === 'ligne') {
            const btnModifier = document.createElement('button');
            btnModifier.className = 'btn btn-sm btn-outline-primary';
            btnModifier.textContent = 'Modifier';
            btnModifier.addEventListener('click', () => {
                this.fermerPopup();
                this.modifierLigne(feature);
            });
            boutons.appendChild(btnModifier);
        }

        const btnSupprimer = document.createElement('button');
        btnSupprimer.className = 'btn btn-sm btn-outline-danger';
        btnSupprimer.textContent = 'Supprimer';
        btnSupprimer.addEventListener('click', () => {
            this.fermerPopup();
            this.featureASupprimer = { type: type, id: props.id, nom: props.nom };
            this.modal = 'suppression';
        });
        boutons.appendChild(btnSupprimer);

        div.appendChild(boutons);

        this._popup = new this._mapboxgl.Popup()
            .setLngLat(lngLat)
            .setDOMContent(div)
            .addTo(this.map);
    }

    private fermerPopup() {
        this._popup?.remove();
        this._popup = undefined;
    }

    private demarrerDragSommet(e: any) {
        if (this.map == null || this.mode !== 'ligne' || e.features == null || e.features.length === 0) {
            return;
        }

        e.preventDefault();

        const index = e.features[0].properties.index;
        const map = this.map;

        const onMove = (ev: any) => {
            this.pointsEnCours[index] = [ev.lngLat.lng, ev.lngLat.lat];
            this.majSourceEdition();
        };

        const onUp = () => {
            map.off('mousemove', onMove);
            map.off('touchmove', onMove);
            map.dragPan.enable();
        };

        map.dragPan.disable();
        map.on('mousemove', onMove);
        map.on('touchmove', onMove);
        map.once('mouseup', onUp);
        map.once('touchend', onUp);
    }

    private majSourceEdition() {
        if (!this._mapChargee || this.map == null) {
            return;
        }

        const couleur = COULEURS_LIGNE[this.typeLigneEnCours] ?? '#888888';
        const features: any[] = [];

        if (this.pointsEnCours.length >= 2) {
            features.push({
                type: 'Feature',
                geometry: { type: 'LineString', coordinates: this.pointsEnCours },
                properties: { couleur: couleur }
            });
        }

        this.pointsEnCours.forEach((point, index) => {
            features.push({
                type: 'Feature',
                geometry: { type: 'Point', coordinates: point },
                properties: { couleur: couleur, index: index }
            });
        });

        (this.map.getSource('edition') as mapboxgl.GeoJSONSource)?.setData({
            type: 'FeatureCollection',
            features: features
        } as any);
    }

    // ---- Saisie d'une ligne ----

    commencerLigne(type: string) {
        this.fermerPopup();
        this.afficherChoixType = false;
        this.typeLigneEnCours = type;
        this.nomLigneEnCours = '';
        this.pointsEnCours = [];
        this.ligneEnEditionId = undefined;
        this.mode = 'ligne';
        this.majSourceEdition();
    }

    modifierLigne(feature: any) {
        this.typeLigneEnCours = feature.properties?.typeLigne ?? 'laterale';
        this.nomLigneEnCours = feature.properties?.nom ?? '';
        this.ligneEnEditionId = feature.properties?.id;

        // La géométrie retournée par queryRenderedFeatures peut être tronquée aux tuiles
        // visibles : on relit la ligne complète depuis le GeoJson chargé.
        const ligneComplete = this._geoJsonReseau.features
            .find((f: any) => f.properties?.id === this.ligneEnEditionId);
        this.pointsEnCours = (ligneComplete ?? feature).geometry.coordinates
            .map((c: number[]) => [c[0], c[1]] as [number, number]);

        this.mode = 'ligne';
        this.majSourceReseau();
        this.majSourceEdition();
    }

    ajouterPointGps() {
        if (this.gpsFix == null) {
            return;
        }

        if (this.mode === 'ligne') {
            this.ajouterPoint([this.gpsFix.lng, this.gpsFix.lat]);
        }
        else if (this.mode === 'arbre' || this.mode === 'entaille') {
            this.ouvrirModalPoint(this.gpsFix.lng, this.gpsFix.lat);
        }
    }

    private ajouterPoint(point: [number, number]) {
        this.pointsEnCours.push(point);
        this.majSourceEdition();
    }

    annulerDernierPoint() {
        this.pointsEnCours.pop();
        this.majSourceEdition();
    }

    async enregistrerLigne() {
        if (this.idErabliereSelectionee == null || this.pointsEnCours.length < 2) {
            return;
        }

        this.enregistrementEnCours = true;

        const ligne: LigneTubelure = {
            id: this.ligneEnEditionId,
            idErabliere: this.idErabliereSelectionee,
            nom: this.nomLigneEnCours || undefined,
            typeLigne: this.typeLigneEnCours,
            coordonneesJson: JSON.stringify(this.pointsEnCours)
        };

        try {
            if (this.ligneEnEditionId != null) {
                await this._api.putLigneTubelure(this.idErabliereSelectionee, ligne);
            }
            else {
                await this._api.postLigneTubelure(this.idErabliereSelectionee, ligne);
            }

            this.error = undefined;
            this.annulerSaisie();
            await this.chargerReseau();
        }
        catch (e) {
            console.error('Erreur lors de l\'enregistrement de la ligne', e);
            this.error = 'Erreur lors de l\'enregistrement de la ligne.';
        }

        this.enregistrementEnCours = false;
    }

    annulerSaisie() {
        this.mode = 'idle';
        this.afficherChoixType = false;
        this.pointsEnCours = [];
        this.nomLigneEnCours = '';
        this.ligneEnEditionId = undefined;
        this.majSourceEdition();
        this.majSourceReseau();
    }

    // ---- Saisie d'un arbre ou d'une entaille ----

    commencerPoint(mode: 'arbre' | 'entaille') {
        this.fermerPopup();
        this.afficherChoixType = false;
        this.mode = mode;
    }

    async ouvrirModalPoint(lng: number, lat: number) {
        this.formNom = '';
        this.formEspece = '';
        this.formLng = lng;
        this.formLat = lat;
        this.formIdArbre = '';
        this.formIdLigne = '';

        if (this.mode === 'entaille') {
            try {
                [this.arbres, this.lignes] = await Promise.all([
                    this._api.getArbres(this.idErabliereSelectionee),
                    this._api.getLignesTubelure(this.idErabliereSelectionee)
                ]);
            }
            catch (e) {
                console.error('Erreur lors de la récupération des arbres et des lignes', e);
                this.arbres = [];
                this.lignes = [];
            }
        }

        this.modal = this.mode === 'arbre' ? 'arbre' : 'entaille';
    }

    utiliserPositionGpsModal() {
        if (this.gpsFix != null) {
            this.formLng = this.gpsFix.lng;
            this.formLat = this.gpsFix.lat;
        }
    }

    async enregistrerArbre() {
        if (this.idErabliereSelectionee == null || this.formLng == null || this.formLat == null) {
            return;
        }

        this.enregistrementEnCours = true;

        const arbre: Arbre = {
            idErabliere: this.idErabliereSelectionee,
            nom: this.formNom || undefined,
            espece: this.formEspece || undefined,
            longitude: this.formLng,
            latitude: this.formLat
        };

        try {
            await this._api.postArbre(this.idErabliereSelectionee, arbre);
            this.error = undefined;
            this.fermerModal();
            this.mode = 'idle';
            await this.chargerReseau();
        }
        catch (e) {
            console.error('Erreur lors de l\'enregistrement de l\'arbre', e);
            this.error = 'Erreur lors de l\'enregistrement de l\'arbre.';
        }

        this.enregistrementEnCours = false;
    }

    async enregistrerEntaille() {
        if (this.idErabliereSelectionee == null || this.formLng == null || this.formLat == null) {
            return;
        }

        this.enregistrementEnCours = true;

        const entaille: Entaille = {
            idErabliere: this.idErabliereSelectionee,
            nom: this.formNom || undefined,
            longitude: this.formLng,
            latitude: this.formLat,
            idArbre: this.formIdArbre || null,
            idLigneTubelure: this.formIdLigne || null
        };

        try {
            await this._api.postEntaille(this.idErabliereSelectionee, entaille);
            this.error = undefined;
            this.fermerModal();
            this.mode = 'idle';
            await this.chargerReseau();
        }
        catch (e) {
            console.error('Erreur lors de l\'enregistrement de l\'entaille', e);
            this.error = 'Erreur lors de l\'enregistrement de l\'entaille.';
        }

        this.enregistrementEnCours = false;
    }

    // ---- Suppression ----

    async confirmerSuppression() {
        if (this.idErabliereSelectionee == null || this.featureASupprimer == null) {
            return;
        }

        this.enregistrementEnCours = true;

        try {
            const { type, id } = this.featureASupprimer;

            if (type === 'ligne') {
                await this._api.deleteLigneTubelure(this.idErabliereSelectionee, id);
            }
            else if (type === 'arbre') {
                await this._api.deleteArbre(this.idErabliereSelectionee, id);
            }
            else {
                await this._api.deleteEntaille(this.idErabliereSelectionee, id);
            }

            this.error = undefined;
            this.fermerModal();
            await this.chargerReseau();
        }
        catch (e) {
            console.error('Erreur lors de la suppression', e);
            this.error = 'Erreur lors de la suppression.';
        }

        this.enregistrementEnCours = false;
    }

    libelleSuppression(): string {
        if (this.featureASupprimer == null) {
            return '';
        }

        const nom = this.featureASupprimer.nom ? ` « ${this.featureASupprimer.nom} »` : '';

        switch (this.featureASupprimer.type) {
            case 'ligne':
                return `la ligne${nom} et son tracé ? Les entailles raccordées seront conservées`;
            case 'arbre':
                return `l'arbre${nom} ? Ses entailles seront conservées`;
            default:
                return `l'entaille${nom} ?`;
        }
    }

    fermerModal() {
        this.modal = null;
        this.featureASupprimer = undefined;
    }
}
