import { ChangeDetectionStrategy, Component, OnInit } from "@angular/core";
import { DatePipe } from "@angular/common";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Abonnement } from "src/model/abonnement";
import { EButtonComponent } from "src/generic/ebutton.component";

interface ForfaitOption {
    plan: string;
    titre: string;
    description: string;
}

@Component({
    selector: 'app-gestion-abonnement',
    templateUrl: './gestion-abonnement.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [DatePipe, EButtonComponent],
})
export class GestionAbonnementComponent implements OnInit {
    abonnements: Abonnement[] = [];
    abonnementCourant: Abonnement | null = null;
    error: string | null = null;
    chargementEnCours = false;
    actionEnCours = false;

    forfaits: ForfaitOption[] = [
        {
            plan: 'gratuit',
            titre: 'Gratuit',
            description: 'Les fonctionnalités de base pour le suivi de votre érablière.'
        },
        {
            plan: 'base',
            titre: 'Base',
            description: 'Les fonctionnalités complètes, incluant ErabliereAI. Facturé via Stripe.'
        },
    ];

    constructor(private readonly api: ErabliereApi) { }

    ngOnInit(): void {
        this.chargerAbonnements();
    }

    chargerAbonnements(): void {
        this.chargementEnCours = true;
        this.api.getAbonnements().then(abonnements => {
            this.abonnements = abonnements;
            this.abonnementCourant = abonnements.find(a => a.statut === 'Actif' || a.statut === 'EnAttente') ?? null;
            this.error = null;
        }).catch(error => {
            console.error("Error loading abonnements:", error);
            this.error = "Erreur lors du chargement des abonnements.";
            this.abonnements = [];
            this.abonnementCourant = null;
        }).finally(() => {
            this.chargementEnCours = false;
        });
    }

    choisirForfait(plan: string): void {
        this.actionEnCours = true;
        this.error = null;
        this.api.postAbonnement({ plan: plan }).then(response => {
            if (response.checkoutUrl) {
                globalThis.location.href = response.checkoutUrl;
            } else {
                this.chargerAbonnements();
            }
        }).catch(error => {
            console.error("Error creating abonnement:", error);
            this.error = typeof error?.error === 'string' ? error.error : "Erreur lors de la création de l'abonnement.";
        }).finally(() => {
            this.actionEnCours = false;
        });
    }

    annulerAbonnement(): void {
        if (!this.abonnementCourant?.id) {
            return;
        }
        if (!confirm("Voulez-vous vraiment annuler votre abonnement ?")) {
            return;
        }
        this.actionEnCours = true;
        this.error = null;
        this.api.annulerAbonnement(this.abonnementCourant.id).then(() => {
            this.chargerAbonnements();
        }).catch(error => {
            console.error("Error canceling abonnement:", error);
            this.error = typeof error?.error === 'string' ? error.error : "Erreur lors de l'annulation de l'abonnement.";
        }).finally(() => {
            this.actionEnCours = false;
        });
    }

    libelleStatut(statut?: string): string {
        switch (statut) {
            case 'EnAttente': return 'En attente de paiement';
            case 'Actif': return 'Actif';
            case 'Annule': return 'Annulé';
            case 'Expire': return 'Expiré';
            default: return statut ?? '';
        }
    }

    libelleForfait(plan?: string): string {
        const forfait = this.forfaits.find(f => f.plan === plan?.toLowerCase());
        return forfait?.titre ?? plan ?? '';
    }
}
