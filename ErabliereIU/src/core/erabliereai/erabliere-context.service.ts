import { Injectable } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { ErabliereApi } from 'src/core/erabliereapi.service';

/** L'érablière que l'utilisateur consulte, telle que déduite de l'url. */
export interface ErabliereContext {
    id: string;
    nom?: string;
}

/**
 * Suit l'érablière affichée à l'écran, pour que la conversation puisse la nommer
 * au modèle plutôt que de le laisser deviner de quelle érablière on parle.
 *
 * Rien ici n'est une autorisation : l'identifiant n'est qu'un indice écrit dans la
 * phrase système. Les outils appelés ensuite sont authentifiés comme l'utilisateur
 * et l'API refuse ce qu'il ne possède pas.
 */
@Injectable({ providedIn: 'root' })
export class ErabliereContextService {
    /** Les routes du client sont de la forme /e/:idErabliereSelectionee/... */
    private static readonly erabliereRoutePattern = /\/e\/([0-9a-fA-F-]{36})(\/|$|\?)/;

    private current?: ErabliereContext;

    constructor(private readonly router: Router, private readonly api: ErabliereApi) {
        this.updateFromUrl(this.router.url);

        this.router.events
            .pipe(filter(event => event instanceof NavigationEnd))
            .subscribe(() => this.updateFromUrl(this.router.url));
    }

    /** L'érablière consultée, ou undefined lorsque la page n'en concerne aucune. */
    getContext(): ErabliereContext | undefined {
        return this.current;
    }

    private updateFromUrl(url: string): void {
        const id = ErabliereContextService.erabliereRoutePattern.exec(url)?.[1];

        if (!id) {
            this.current = undefined;
            return;
        }

        if (this.current?.id === id) {
            return;
        }

        this.current = { id: id };

        // Le nom n'est que du confort : il évite au modèle un appel d'outil pour
        // apprendre comment s'appelle l'érablière. Un échec ne coûte donc rien.
        this.api.getErabliere(id, true, true)
            .then(erabliere => {
                if (this.current?.id === id) {
                    this.current.nom = erabliere?.nom;
                }
            })
            .catch(() => { /* l'identifiant suffit */ });
    }
}
