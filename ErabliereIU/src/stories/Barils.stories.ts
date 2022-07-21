import { Meta, Story } from '@storybook/angular';
import { Customer } from 'src/model/customer';
import { CustomerAccess } from 'src/model/customerAccess';

import { BarilsComponent } from '../barils/barils.component';
import faker from '@faker-js/faker';
import { ModuleStoryHelper } from './moduleMetadata/moduleStoryHelper';
import { ChartsModule } from 'ng2-charts';
import { BrowserModule } from '@angular/platform-browser';

export default {
  title: 'BarilsComponent',
  component: BarilsComponent,
  decorators: [
    ModuleStoryHelper.getErabliereApiStoriesModuleMetadata([], [
      ChartsModule
    ])
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