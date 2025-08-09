import { AlertesPage } from "./alertes.page";
import { NotesPage } from "./notes.page";
import { GraphPannelCompoenent } from "./component/graphpannel.component";
import { AProposPage } from "./apropos.page";

export class HomePage {
    visit(): this {
        cy.login();
        cy.visit('/');
        return this;
    }

    clickOnAlerteButtonNavMenu(): AlertesPage {
        cy.get('#nav-menu-alerte-button').focus().click()
        return new AlertesPage();
    }

    clickOnNotesButtonNavMenu(): NotesPage {
        cy.get('#nav-menu-notes-button').focus().click()
        return new NotesPage();
    }

    clickOnAProposPage(): AProposPage {
        cy.get('.container-fluid > .navbar-collapse > .navbar-nav > .nav-item:nth-child(5) > .nav-link').click();
        return new AProposPage();
    }

    getGraphPannel(guidId: string): GraphPannelCompoenent {
        return new GraphPannelCompoenent('#graph-pannel-' + guidId);
    }

    searchErabliere(name: string): this {
        cy.get('input[placeholder="Rechercher une erablière"').type(name);
        cy.wait(100);
        return this;
    }
}