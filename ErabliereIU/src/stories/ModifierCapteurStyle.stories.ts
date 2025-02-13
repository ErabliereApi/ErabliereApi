import { type Meta, type StoryObj } from '@storybook/angular';
import { ModuleStoryHelper } from './moduleMetadata/moduleStoryHelper';
import { ModifierCapteurStyleComponent } from 'src/capteurs/modifier-capteur-style.component';

const meta: Meta<ModifierCapteurStyleComponent> = {
  title: 'ModifierCapteurStyleComponent',
  component: ModifierCapteurStyleComponent,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  decorators: [
    ModuleStoryHelper.getErabliereApiStoriesApplicationConfig()
  ],
};

export default meta;
type Story = StoryObj<ModifierCapteurStyleComponent>;

export const Primary: Story = {
    args: {
        inputCapteur: {
            ajouterDonneeDepuisInterface: false,
            id: "1",
            idErabliere: "1",
            nom: "Capteur 1",
            symbole: "*",
            capteurStyle: {
                backgroundColor: "#000000",
                borderColor: "#000000",
                color: "#000000",
                dSetBorderColors: "#000000",
                fill: false,
                g1Color: "#000000",
                g1Stop: 0,
                g2Color: "#000000",
                g2Stop: 0,
                g3Color: "#000000",
                g3Stop: 0,
                id: 1,
                pointBackgroundColor: "#000000",
                pointBorderColor: "#000000",
                tension: 0,
                useGradient: false
            }
        }   
    }
};
