let mapboxglPromise: Promise<typeof import('mapbox-gl/esm')> | undefined;

/**
 * Charge Mapbox GL JS (build ESM) à la demande.
 *
 * Le build ESM démarre son worker avec `new URL("worker.js", import.meta.url)`,
 * un fichier que le build Angular n'émet pas. Les fichiers de `mapbox-gl/dist/esm`
 * sont donc copiés dans `mapbox-gl/` (voir `assets` dans angular.json) et le worker
 * est chargé à partir de là.
 */
export function chargerMapboxgl(): Promise<typeof import('mapbox-gl/esm')> {
    mapboxglPromise ??= import('mapbox-gl/esm').then(mapboxgl => {
        mapboxgl.setWorkerUrl(new URL('mapbox-gl/worker.js', document.baseURI).href);
        return mapboxgl;
    });

    return mapboxglPromise;
}
