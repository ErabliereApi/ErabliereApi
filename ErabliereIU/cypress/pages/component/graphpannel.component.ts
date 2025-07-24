export class GraphPannelCompoenent {
    
    constructor(private readonly pannelId: string) {

    }

    getAddButton(): Cypress.Chainable<JQuery<HTMLElement>> {
        let addButton = cy.get(this.pannelId).then($pannel => {
            let buttons = cy.wrap($pannel).find('button');
            let addButton = buttons.filter(function(index, element) {
                return element.innerText === "Ajouter";
            });
            return addButton;
        });
        return addButton;
    }

    getCancelButton(): Cypress.Chainable<JQuery<HTMLElement>> {
        let cancelButton = cy.get(this.pannelId).then($pannel => {
            let buttons = cy.wrap($pannel).find('button');
            let cancelButton = buttons.filter(function(index, element) {
                return element.innerText === "Annuler";
            });
            return cancelButton;
        });
        return cancelButton;
    }

    find(selector: string): Cypress.Chainable<JQuery<HTMLElement>> {
        let elem = cy.get(this.pannelId).then($pannel => {
            let element = cy.wrap($pannel).find(selector);
            return element;
        });
        return elem;
    }

    enterValue(value: string): void {
        cy.get(this.pannelId).then($pannel => {
            let valeurField = cy.wrap($pannel).find('input[name="valeur"]');
            valeurField.type(value);
        });
    }

    enterDate(): void {
        cy.get(this.pannelId).then($pannel => {
            const format = 'ca-FR'
            let dateField = cy.wrap($pannel).find('input[name="date"]');
            let localDate = new Date()
            let dateInfo = localDate.toLocaleDateString(format).split('/');
            let timeInfo = localDate.toLocaleTimeString(format).split(':');
            dateField.type(dateInfo[2].padStart(2, '0') + '-' + dateInfo[1].padStart(2, '0') + '-' + dateInfo[0].padStart(2, '0') + 'T' + 
                           timeInfo[0].padStart(2, '0') + ':' + timeInfo[1].padStart(2, '0') + ':' + timeInfo[2].padStart(2, '0'));
        });
    }
}