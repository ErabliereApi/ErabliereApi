import { type Meta, type StoryObj } from '@storybook/angular';
import { BarPanelComponent } from 'src/donnees/sub-panel/bar-panel.component';
import { ModuleStoryHelper } from './moduleMetadata/moduleStoryHelper';

const meta: Meta<BarPanelComponent> = {
  title: 'BarPannelComponent',
  component: BarPanelComponent,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  decorators: [
    ModuleStoryHelper.getErabliereApiStoriesApplicationConfig()
  ],
};

export default meta;
type Story = StoryObj<BarPanelComponent>;

export const Primary: Story = {
  args: {
    titre: 'Titre',
    barChartType: 'bar',
    datasets: [
      { data: [65, 59, 80, 81, 56, 55, 40], label: 'Series A' },
      { data: [28, 48, 40, 19, 86, 27, 90], label: 'Series B' },
    ],
    symbole: '%',
    valeurActuel: "40",
    timeaxes: ['2013', '2014', '2015', '2016', '2017', '2018', '2019'],
  }
};

export const NoArgs: Story = {

};
