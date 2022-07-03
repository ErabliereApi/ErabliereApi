import { Meta, Story } from '@storybook/angular';
import { Customer } from 'src/model/customer';
import { CustomerAccess } from 'src/model/customerAccess';

import { CapteurPannelsComponent } from '../donnees/sub-panel/capteur-pannels.component';
import faker from '@faker-js/faker';
import { ModuleStoryHelper } from './moduleMetadata/moduleStoryHelper';

export default {
  title: 'CapteurPannelsComponent',
  component: CapteurPannelsComponent,
  decorators: [
    ModuleStoryHelper.getErabliereApiStoriesModuleMetadata()
  ]
} as Meta;

var fixture = {};

//👇 We create a “template” of how args map to rendering
const Template: Story = (args) => ({
  props: args,
});

//👇 Each story then reuses that template
export const Primary = Template.bind({});

Primary.args = {
  
};
