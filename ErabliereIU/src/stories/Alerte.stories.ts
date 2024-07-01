import { type Meta, type StoryObj } from '@storybook/angular';
import { AlerteComponent } from 'src/alerte/alerte.component';
import { ModuleStoryHelper } from './moduleMetadata/moduleStoryHelper';

const meta: Meta<AlerteComponent> = {
  title: 'AlerteComponent',
  component: AlerteComponent,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  decorators: [
    ModuleStoryHelper.getErabliereApiStoriesApplicationConfig()
  ],
};

export default meta;
type Story = StoryObj<AlerteComponent>;

export const Primary: Story = {
  args: {
    alertes: [
      {
        id: "guid1",
        idErabliere: "guid2",
        emails: [
          "demo@email.com",
          "demo2@email.com"
        ]
      }
    ],
    alertesCapteur: [
      {
        id: "guid3",
        idCapteur: "guid4",
        minVaue: 1,
        maxValue: 30,
        capteur: {
          id: "guid4",
          idErabliere: "guid2",
          nom: "Temperature",
          symbole: "°C",
          ajouterDonneeDepuisInterface: false
        }
      }
    ],
  }  
};

export const Empty: Story = {

};