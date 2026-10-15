import { ChangeDetectionStrategy, Component, Input, OnChanges } from "@angular/core";
import { DatePipe } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { ErabliereApi } from "src/core/erabliereapi.service";
import { Kit, PostKitPanne, PostKitPanneResponse } from "src/model/kit";
import { CopyTextButtonComponent } from "src/generic/copy-text-button.component";
import { EButtonComponent } from "src/generic/ebutton.component";

@Component({
    selector: 'app-kits-panne',
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: './kits-panne.component.html',
    imports: [FormsModule, DatePipe, CopyTextButtonComponent, EButtonComponent]
})
export class KitsPanneComponent implements OnChanges {
    @Input() idErabliere?: string;

    kits: Kit[] = [];
    nouveauKit: PostKitPanne = { nom: 'Kit panne', delaiSilenceMinutes: 5 };
    configuration?: PostKitPanneResponse;
    commandeInstallation = '';
    ajoutEnCours = false;
    revocationEnCours?: string;
    erreur?: string;

    constructor(private readonly api: ErabliereApi) {

    }

    ngOnChanges(): void {
        this.configuration = undefined;
        this.erreur = undefined;
        this.chargerKits();
    }

    private chargerKits() {
        if (!this.idErabliere) {
            return;
        }
        this.api.getKits(this.idErabliere).then(kits => {
            this.kits = kits;
        }).catch(() => {
            this.kits = [];
        });
    }

    ajouterKit() {
        this.ajoutEnCours = true;
        this.erreur = undefined;
        this.api.postKitPanne(this.idErabliere, { ...this.nouveauKit }).then(configuration => {
            this.configuration = configuration;
            this.commandeInstallation = this.construireCommande(configuration);
            this.chargerKits();
        }).catch(err => {
            this.erreur = typeof err?.error === 'string' ? err.error : "Le kit n'a pas pu être ajouté.";
        }).finally(() => {
            this.ajoutEnCours = false;
        });
    }

    revoquerKit(kit: Kit) {
        if (!confirm(`Révoquer le kit « ${kit.nom} »? Il ne pourra plus envoyer de données. Les capteurs et leur historique sont conservés.`)) {
            return;
        }
        this.revocationEnCours = kit.id;
        this.api.revoquerKit(this.idErabliere, kit.id).then(() => {
            this.chargerKits();
        }).catch(err => {
            console.error(err);
        }).finally(() => {
            this.revocationEnCours = undefined;
        });
    }

    fermerConfiguration() {
        this.configuration = undefined;
        this.commandeInstallation = '';
    }

    // Une seule commande à coller dans le terminal du kit, qui écrit les deux fichiers lus par monitorUpsV2.py
    private construireCommande(configuration: PostKitPanneResponse) {
        const auth = configuration.nomFichierAuthentification;
        return [
            'mkdir -p ~/.erabliereapi',
            `cat > ~/.erabliereapi/${configuration.nomFichierConfiguration} <<'EOF'`,
            configuration.fichierConfiguration,
            'EOF',
            `cat > ~/.erabliereapi/${auth} <<'EOF'`,
            configuration.fichierAuthentification,
            'EOF',
            `chmod 600 ~/.erabliereapi/${auth}`
        ].join('\n');
    }
}
