import { type Meta, type StoryObj } from '@storybook/angular';
import { CapteurPanelsComponent } from 'src/donnees/sub-panel/capteur-panels.component';
import { ModuleStoryHelper } from './moduleMetadata/moduleStoryHelper';

const meta: Meta<CapteurPanelsComponent> = {
  title: 'CapteurPannelsComponent',
  component: CapteurPanelsComponent,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  decorators: [
    ModuleStoryHelper.getErabliereApiStoriesApplicationConfig()
  ],
};

export default meta;
type Story = StoryObj<CapteurPanelsComponent>;

export const Primary: Story = {
  args: {
    capteurs: [
      {
        id: "some-guid",
        nom: "Temperature",
        symbole: "°C",
        afficherCapteurDashboard: true,
        ajouterDonneeDepuisInterface: false,
      }
    ]
  }
};
