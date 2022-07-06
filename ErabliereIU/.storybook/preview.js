import { setCompodocJson } from "@storybook/addon-docs/angular";
import { themes } from '@storybook/theming';
import docJson from "../documentation.json";
setCompodocJson(docJson);

export const parameters = {
  actions: { argTypesRegex: "^on[A-Z].*" },
  controls: {
    matchers: {
      color: /(background|color)$/i,
      date: /Date$/,
    },
  },
  docs: { 
    inlineStories: true,
    theme: themes.light,
  },
}