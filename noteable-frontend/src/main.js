// bring in Bootstrap styles
import 'bootstrap/dist/css/bootstrap.min.css';
// bring in Bootstrap JS (needed for dropdowns, modals, etc)
import 'bootstrap/dist/js/bootstrap.bundle.min.js';

import { createApp } from 'vue';
import { createPinia } from 'pinia';

import App from './App.vue';
import router from './router';

// import the auth store so we can init it before mounting the app
import { useAuthStore } from '@/store/auth';

// import custom directive used for admin-only elements
import { registerAdminDirective } from './directives/admin';

// import global admin helper functions (e.g. delete util)
import { installAdminUtils } from './utils/adminUtils';

// import CSS used for admin styling (like badges or delete buttons)
import './assets/admin.css';

// create the Vue app instance
const app = createApp(App);

// create and use Pinia for global state
const pinia = createPinia();
app.use(pinia);

// load user auth info (like token) from local storage on startup
const authStore = useAuthStore();
authStore.initializeAuth();

// add the v-admin directive globally (used to show/hide admin features)
registerAdminDirective(app);

// make admin helpers available throughout the app
installAdminUtils(app);

// setup router and mount app
app.use(router);
app.mount('#app');
