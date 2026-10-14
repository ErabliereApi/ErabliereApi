import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { EpisodeAlerteCapteur, RaisonFinEpisode } from 'src/model/episodeAlerteCapteur';

type Periode = '30j' | '12m' | 'tout';

@Component({
    selector: 'app-episodes-alerte-capteur',
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [FormsModule],
    template: `
        <div class="d-flex flex-wrap align-items-center gap-2 mb-2">
            <h5 class="mb-0 me-auto">Historique des épisodes : {{nomCapteur}}</h5>
            <label for="periode-episodes" class="form-label mb-0">Période</label>
            <select id="periode-episodes" class="form-select form-select-sm w-auto"
                [(ngModel)]="periode" (ngModelChange)="charger()">
                <option value="30j">30 derniers jours</option>
                <option value="12m">12 derniers mois</option>
                <option value="tout">Tout</option>
            </select>
            <button class="btn btn-sm btn-outline-secondary" (click)="charger()">Actualiser</button>
        </div>

        @if (chargement) {
        <p><span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Chargement des épisodes...</p>
        }
        @if (erreur) {
        <p class="text-danger">{{erreur}}</p>
        }
        @if (!chargement && !erreur && episodes.length === 0) {
        <p>Aucun épisode d'alerte pour cette période.</p>
        }
        @if (episodes.length > 0) {
        <div class="table-responsive">
            <table class="table table-sm">
                <thead>
                    <tr>
                        <th>Début</th>
                        <th>Fin</th>
                        <th>Durée</th>
                        <th>Alerte</th>
                        <th>Valeur au début</th>
                        <th>Valeur à la fin</th>
                    </tr>
                </thead>
                <tbody>
                    @for (episode of episodes; track episode.id) {
                    <tr [class.table-danger]="!episode.dateFin">
                        <td>{{formaterDate(episode.dateDebut)}}</td>
                        <td>
                            @if (episode.dateFin) {
                            {{formaterDate(episode.dateFin)}}
                            @if (episode.raisonFin && episode.raisonFin !== 'RetourNormale') {
                            <div><small class="text-body-secondary">Fin non mesurée : {{libelleRaison(episode.raisonFin)}}</small></div>
                            }
                            } @else {
                            <span class="badge text-bg-danger">En cours</span>
                            }
                        </td>
                        <td>
                            @if (!episode.dateFin) {
                            <span class="fw-semibold">depuis </span>
                            }
                            {{formaterDuree(episode)}}
                        </td>
                        <td>{{episode.nomAlerte}}</td>
                        <td>{{formaterValeur(episode.valeurDebut)}}</td>
                        <td>{{formaterValeur(episode.valeurFin)}}</td>
                    </tr>
                    }
                </tbody>
            </table>
        </div>
        }
    `
})
export class EpisodesAlerteCapteurComponent implements OnChanges {
    @Input() idCapteur?: any;
    @Input() nomCapteur?: string;
    @Input() symbole?: string;

    episodes: EpisodeAlerteCapteur[] = [];
    chargement: boolean = false;
    erreur: string | null = null;
    periode: Periode = '12m';

    /** L'heure de référence de la durée d'un épisode en cours, fixée au chargement. */
    private maintenant: Date = new Date();

    private static readonly formatDate = new Intl.DateTimeFormat('fr-CA', { dateStyle: 'long', timeStyle: 'short' });

    constructor(private readonly _api: ErabliereApi) { }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['idCapteur']) {
            this.charger();
        }
    }

    charger() {
        if (this.idCapteur == null) {
            return;
        }

        this.chargement = true;
        this.erreur = null;
        this.maintenant = new Date();

        this._api.getEpisodesAlerteCapteur(this.idCapteur, this.debutPeriode())
            .then(episodes => {
                this.episodes = episodes;
            })
            .catch(err => {
                this.episodes = [];
                this.erreur = "Erreur lors du chargement des épisodes : " + err.message;
            })
            .finally(() => {
                this.chargement = false;
            });
    }

    formaterDate(date?: string) {
        return date ? EpisodesAlerteCapteurComponent.formatDate.format(new Date(date)) : '';
    }

    formaterValeur(valeur?: number) {
        return valeur == null ? '' : valeur + (this.symbole ? ' ' + this.symbole : '');
    }

    /** Même format que les messages d'alerte : « 8 min », « 2 h 5 min », « 1 j 3 h 15 min ». */
    formaterDuree(episode: EpisodeAlerteCapteur) {
        if (!episode.dateDebut) {
            return '';
        }

        const fin = episode.dateFin ? new Date(episode.dateFin) : this.maintenant;
        const totalMinutes = Math.floor((fin.getTime() - new Date(episode.dateDebut).getTime()) / 60000);

        if (totalMinutes < 1) {
            return "moins d'une minute";
        }

        const jours = Math.floor(totalMinutes / 1440);
        const heures = Math.floor((totalMinutes % 1440) / 60);
        const minutes = totalMinutes % 60;

        const parties: string[] = [];
        if (jours > 0) parties.push(jours + ' j');
        if (heures > 0) parties.push(heures + ' h');
        if (minutes > 0) parties.push(minutes + ' min');

        return parties.join(' ');
    }

    libelleRaison(raison: RaisonFinEpisode) {
        switch (raison) {
            case 'AlerteModifiee': return 'alerte modifiée';
            case 'AlerteDesactivee': return 'alerte désactivée';
            case 'AlerteSupprimee': return 'alerte supprimée';
            default: return '';
        }
    }

    private debutPeriode(): Date | undefined {
        const debut = new Date(this.maintenant);

        switch (this.periode) {
            case '30j':
                debut.setDate(debut.getDate() - 30);
                return debut;
            case '12m':
                debut.setFullYear(debut.getFullYear() - 1);
                return debut;
            default:
                return undefined;
        }
    }
}
