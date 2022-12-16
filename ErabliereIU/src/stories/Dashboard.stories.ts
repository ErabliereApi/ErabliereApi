import { Meta, Story } from '@storybook/angular';
import { DashboardComponent } from '../dashboard/dashboard.component';
import { ModuleStoryHelper } from './moduleMetadata/moduleStoryHelper';

export default {
  title: 'DashboardComponent',
  component: DashboardComponent,
  decorators: [
    ModuleStoryHelper.getErabliereApiStoriesModuleMetadata([])
  ]
} as Meta;

//👇 We create a “template” of how args map to rendering
const Template: Story = (args) => ({
  props: args,
});

//👇 Each story then reuses that template
export const UserLoggedIn = Template.bind({});

UserLoggedIn.args = {
  isLoggedIn: true,
  useAuthentication: true
};

export const AuthEnabled = Template.bind({});

AuthEnabled.args = {
  isLoggedIn: false,
  useAuthentication: true
};

export const NoAuthentication = Template.bind({});

NoAuthentication.args = {
  isLoggedIn: false,
  useAuthentication: false
};