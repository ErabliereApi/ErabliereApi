import { type Meta, type StoryObj } from '@storybook/angular';
import { VacuumGraphPanelComponent } from 'src/donnees/sub-panel/vacuum-graph-panel.component';
import { ModuleStoryHelper } from './moduleMetadata/moduleStoryHelper';

const meta: Meta<VacuumGraphPanelComponent> = {
  title: 'VacciumGraphPannelComponent',
  component: VacuumGraphPanelComponent,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  decorators: [
    ModuleStoryHelper.getErabliereApiStoriesApplicationConfig()
  ],
};

export default meta;
type Story = StoryObj<VacuumGraphPanelComponent>;

export const Primary: Story = {
  args: {
    timeaxes: ['2021-01-01 00:00:00', '2021-01-01 00:01:00', '2021-01-01 00:02:00', '2021-01-01 00:03:00', '2021-01-01 00:04:00', '2021-01-01 00:05:00', '2021-01-01 00:06:00', '2021-01-01 00:07:00', '2021-01-01 00:08:00'],
    datasets: [
      { 
          data: [1, 2, 3, 4, 5, 6, 7, 8, 9], 
          label: "Vaccium",
          fill: true,
          pointBackgroundColor: 'rgba(255,255,0,0.8)',
          pointBorderColor: 'black',
          tension: 0.5
      }
    ]
  }
};

export const AvecMoyenne: Story = {
  args: {
    timeaxes: ['2021-01-01 00:00:00', '2021-01-01 00:01:00', '2021-01-01 00:02:00', '2021-01-01 00:03:00', '2021-01-01 00:04:00', '2021-01-01 00:05:00', '2021-01-01 00:06:00', '2021-01-01 00:07:00', '2021-01-01 00:08:00'],
    datasets: [
      { 
          data: [1, 2, 3, 4, 5, 6, 7, 8, 9], 
          label: "Vaccium",
          fill: true,
          pointBackgroundColor: 'rgba(255,255,0,0.8)',
          pointBorderColor: 'black',
          tension: 0.5
      }
    ],
    dateDebutFixRange: '2021-01-01 00:00:00',
    dateFinFixRange: '2021-01-01 00:08:00',
    fixRange: true
  }
};