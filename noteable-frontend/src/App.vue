<template>
  <!-- Main app container -->
  <div class="app-container">
    <!-- Show navbar on all pages except login, register and landing -->
    <NavBar v-if="showNavBar" />
    
    <!-- Page content will be injected here -->
    <router-view />
  </div>
</template>

<script>
// import the navbar component
import NavBar from '@/components/NavBar.vue';

export default {
  name: 'App',
  components: { NavBar },

  computed: {
    // showNavBar is true unless we're on landing, login or register routes
    showNavBar() {
      return !['/', '/login', '/register'].includes(this.$route.path);
    }
  }
};
</script>

<style>
/* Base styles for all pages */
html, body {
  background-color: #f7f3e9; /* Light beige */
  min-height: 100vh;
  margin: 0;
  padding: 0;
  font-family: 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
}

/* Wraps the whole app */
.app-container {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

/* Card styling used globally */
.card {
  border-radius: 8px;
  border: 1px solid rgba(0, 0, 0, 0.08);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.05);
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  background-color: white;
}

.card:hover {
  transform: translateY(-3px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

/* Button style shared across app */
.btn-primary, .btn-upload, .btn-journal {
  background-color: #2c3e50;
  color: white;
  border: none;
  transition: background-color 0.3s ease;
}

.btn-primary:hover, .btn-upload:hover, .btn-journal:hover {
  background-color: #1a252f;
}

/* Focus styles for inputs and selects */
.form-control:focus, .form-select:focus {
  border-color: #2c3e50;
  box-shadow: 0 0 0 0.25rem rgba(44, 62, 80, 0.25);
}
</style>
