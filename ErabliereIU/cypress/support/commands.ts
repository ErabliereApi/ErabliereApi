declare namespace Cypress {
    interface Chainable {
        login(): void;
        checkoutEnabled(): Cypress.Chainable<boolean>;
        forceVisit(url: string): void;
    }
}

Cypress.Commands.add('login', () => {
    function isAzureAD(config: any) {
        return config.tenantId != undefined && config.tenantId.length > 1;
    }

    cy.request({
        method: "GET",
        url: "/assets/config/oauth-oidc.json"
    }).then(body => {
        const config = body.body;

        if (config.authEnable) {
            if (isAzureAD(config)) {
                // Check if there is already a token in local storage
                // var token = localStorage.getItem("adal.idtoken");

                // if (token) {
                //     // Check if the token is still valid
                //     var expiresOn = localStorage.getItem(`adal.expiration.key${Cypress.env("clientId")}`) ?? "";
                //     var now = new Date();
                //     var expiresOnDate = new Date(parseInt(expiresOn));
                //     if (now < expiresOnDate) {
                //         cy.log("Token is still valid, expire at " + expiresOnDate);
                //         return;
                //     }
                // }

                cy.request({
                    method: "POST",
                    url: `https://login.microsoftonline.com/${config.tenantId}/oauth2/token`,
                    form: true,
                    body: {
                        grant_type: "client_credentials",
                        client_id: Cypress.env("clientId"),
                        client_secret: Cypress.env("clientSecret"),
                        scope: config.scopes
                    },
                }).then(response => {
                    const ADALToken = response.body.access_token;
                    const expiresOn = response.body.expires_on;

                    localStorage.setItem("adal.token.keys", `${Cypress.env("clientId")}|`);
                    localStorage.setItem(`adal.access.token.key${Cypress.env("clientId")}`, ADALToken);
                    localStorage.setItem(`adal.expiration.key${Cypress.env("clientId")}`, expiresOn);
                    localStorage.setItem("adal.idtoken", ADALToken);
                });
            }
            else {
                cy.request({
                    method: "POST",
                    url: `${config.stsAuthority}/connect/token`,
                    headers: {
                        "Content-Type": "application/x-www-form-urlencoded",
                        "Authorization": "Basic cmFzcGJlcnJ5bG9jYWw6c2VjcmV0"
                    },
                    form: true,
                    body: "grant_type=client_credentials&scope=erabliereapi"
                }).then(res => {
                    sessionStorage.setItem(`oidc.user:${config.stsAuthority}:erabliereiu`, JSON.stringify(res.body))
                })
            }
        }
    })
})

Cypress.Commands.add('checkoutEnabled', () => {
    return cy.request({
        method: "GET",
        url: "/assets/config/oauth-oidc.json"
    }).then(body => {
        return body.body;
    }).then(urlInfo => {
        return cy.request({
            method: "GET",
            url: urlInfo.apiUrl + "/api/v1/swagger.json"
        }).then(response => {
            var checkoutEnabled = response.body.paths['/Checkout'] !== undefined;
            return cy.wrap(checkoutEnabled);
        })
    })
})

Cypress.Commands.add('forceVisit', url => {
    cy.window().then(win => {
        return win.open(url, '_self');
    })
})