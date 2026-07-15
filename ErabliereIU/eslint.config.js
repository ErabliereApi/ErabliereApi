// @ts-check
// Migration flat config (ESLint 9) de l'ancien .eslintrc.json.
// Les règles sont volontairement identiques à l'ancienne configuration :
// @angular-eslint recommended + sélecteurs préfixés « app ».
const tseslint = require("typescript-eslint");
const angular = require("angular-eslint");

module.exports = tseslint.config(
  {
    ignores: ["projects/**/*", "dist/**/*", ".angular/**/*"],
  },
  {
    files: ["**/*.ts"],
    languageOptions: {
      parser: tseslint.parser,
    },
    extends: [...angular.configs.tsRecommended],
    processor: angular.processInlineTemplates,
    rules: {
      // Le projet utilise l'injection par constructeur partout.
      "@angular-eslint/prefer-inject": "off",
      // Les composants alimentés par les événements MSAL doivent utiliser
      // ChangeDetectionStrategy.Eager, sinon la vue ne se met jamais à jour
      // (voir ErabliereIU/CLAUDE.md), donc OnPush ne peut pas être imposé.
      "@angular-eslint/prefer-on-push-component-change-detection": "off",
      // Dette existante : plusieurs composants génériques (ebutton, einput,
      // emodal, ...) n'ont pas le préfixe « app ». En avertissement pour
      // guider les nouveaux composants sans bloquer le lint.
      "@angular-eslint/component-selector": [
        "warn",
        {
          type: "element",
          prefix: "app",
          style: "kebab-case",
        },
      ],
      "@angular-eslint/directive-selector": [
        "warn",
        {
          type: "attribute",
          prefix: "app",
          style: "camelCase",
        },
      ],
      // Renommer les outputs préfixés « on » changerait l'API des templates.
      "@angular-eslint/no-output-on-prefix": "warn",
    },
  },
  {
    files: ["**/*.html"],
    extends: [...angular.configs.templateRecommended],
    rules: {
      // Dette existante (== vs === et quelques *ngIf restants) : en
      // avertissement pour ne pas bloquer le lint; corriger au fur et à mesure.
      "@angular-eslint/template/eqeqeq": "warn",
      "@angular-eslint/template/prefer-control-flow": "warn",
    },
  }
);
