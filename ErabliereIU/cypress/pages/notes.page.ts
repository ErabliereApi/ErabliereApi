import { FormUtil } from "cypress/util/formUtil";

export class NotesPage {
    
    getPageTitle(): Cypress.Chainable<JQuery<HTMLElement>> {
        return cy.get('notes > h3');
    }

    getAddButton(): Cypress.Chainable<JQuery<HTMLElement>> {
        let addButton = cy.get('#addNoteButton');
        return addButton;
    }

    getCancelButton(): Cypress.Chainable<JQuery<HTMLElement>> {
        let cancelButton = cy.get("#annulerCreerNote");
        return cancelButton;
    }

    enterNoteTitle(value: string): void {
        FormUtil.typeTextBaseOnFormControlName(value, "notes", "title");
    }

    enterNoteDescription(value: string): void {
        FormUtil.typeTextBaseOnFormControlName(value, "notes", "text");
    }

    enterNoteDate(date: string) {
        FormUtil.typeTextBaseOnFormControlName(date, "notes", "noteDate");
    }

    sendNote(): void {
        FormUtil.clickButton("notes", "creerNote");
    }

    addNote(title: string, content: string, date: string | undefined = undefined): this {
        this.getAddButton()
            .click();

        this.enterNoteTitle(title);
        this.enterNoteDescription(content);

        if (date != undefined) {
            this.enterNoteDate(date);
        }

        this.sendNote();

        return this;
    }

    getNoteTitle(): Cypress.Chainable<JQuery<HTMLElement>> {
        // get the first note of the page and check its description
        return cy.get("note").first().then(noteComponent => {
            cy.wrap(noteComponent)
                .find('h4[class="card-header"]')
                .then(text => {
                    return text;
                }
            );
        });
    }

    getNoteDescription(): Cypress.Chainable<JQuery<HTMLElement>> {
        // get the first note of the page and check its description
        return cy.get("note").first().then(noteComponent => {
            cy.wrap(noteComponent)
                .find('p[class="noteDescription card-text"]')
                .then(text => {
                    return text;
                }
            );
        });
    }

    getNoteDate(): Cypress.Chainable<JQuery<HTMLElement>> {
        // get the first note of the page and check its date
        return cy.get("note").first().then(noteComponent => {
            cy.wrap(noteComponent)
                .find('p[class="noteDate card-text"]')
                .then(text => {
                    return text;
                }
            );
        });
    }
}