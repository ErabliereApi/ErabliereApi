export class FormUtil {
    static selectValueFromDropdown(selector: string, value: string) {
        cy.get(selector).select(value);
    }
    static typeTextBaseOnName(text: string, selector: string, name: string, timeout: number = 10000): void {
        cy.get(selector, { timeout: timeout }).then(element => {
            cy.wrap(element)
                .find('input[name="' + name + '"]')
                .type(text);
        });
    }
    static typeTextAreaBaseOnName(text: string, selector: string, name: string, timeout: number = 10000): void {
        cy.get(selector, { timeout: timeout }).then(element => {
            cy.wrap(element)
                .find('textarea[name="' + name + '"]')
                .type(text);
        });
    }
    static typeTextBaseOnFormControlName(text: string, selector: string, formControl: string, timeout: number = 10000): void {
        cy.get(selector, { timeout: timeout }).then(element => {
            cy.wrap(element)
                .find('input[formcontrolname="' + formControl + '"]')
                .type(text);
        });
    }
    static clickButton(selector: string, buttonId: string, timeout: number = 10000): void {
        cy.get(selector, { timeout: timeout }).then(noteComponent => {
            cy.wrap(noteComponent)
                .find('button[id="' + buttonId + '"]')
                .click();
        });
    }
}