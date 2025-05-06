<template>
  <!-- Redesigned navbar with optimized font strategy -->
  <nav class="navbar navbar-expand-lg navbar-light">
    <div class="container">
      <!-- Brand name linking to Home with Calistoga font -->
      <router-link class="navbar-brand" to="/home">Noteable</router-link>
      
      <!-- Navbar toggler (for mobile) -->
      <button
        class="navbar-toggler"
        type="button"
        data-bs-toggle="collapse"
        data-bs-target="#navbarSupportedContent"
        aria-controls="navbarSupportedContent"
        aria-expanded="false"
        aria-label="Toggle navigation"
      >
        <span class="navbar-toggler-icon"></span>
      </button>
      
      <!-- Collapsible section with nav links -->
      <div class="collapse navbar-collapse" id="navbarSupportedContent">
        <ul class="navbar-nav ms-auto mb-2 mb-lg-0">
          <!-- Home link -->
          <li class="nav-item">
            <router-link class="nav-link" to="/home">Home</router-link>
          </li>
          
          <!-- Note Journal link -->
          <li class="nav-item">
            <router-link class="nav-link" to="/journal">Note Journal</router-link>
          </li>
          
          <!-- Forum link -->
          <li class="nav-item">
            <router-link class="nav-link" to="/forum">Forum</router-link>
          </li>
          
          <!-- Mentors link -->
          <li class="nav-item">
            <router-link class="nav-link" to="/mentors">Mentors</router-link>
          </li>
          
          <!-- Profile link (Only show when authenticated) -->
          <li class="nav-item" v-if="authStore.isAuthenticated">
            <router-link class="nav-link" to="/profile">Profile</router-link>
          </li>
          
          <!-- Logout link (Only show when authenticated) -->
          <li class="nav-item" v-if="authStore.isAuthenticated">
            <button class="nav-btn" @click="handleLogout">Logout</button>
          </li>
          
          <!-- Login/Register when not authenticated -->
          <li class="nav-item" v-else>
            <router-link class="nav-link" to="/login">Login</router-link>
          </li>
        </ul>
      </div>
    </div>
  </nav>
</template>

<script>
import { useAuthStore } from '@/store/auth';
import { useRouter } from 'vue-router';

export default {
  name: 'NavBar',
  setup() {
    const authStore = useAuthStore();
    const router = useRouter();
    
    function handleLogout() {
      console.log('Logout link clicked...');
      authStore.logout();
      router.push('/login');
    }
    
    return {
      handleLogout,
      authStore,
    };
  },
};
</script>

<style scoped>
/* Imported fonts - Calistoga for brand, Inter for everything else */
@import url('https://fonts.googleapis.com/css2?family=Calistoga&family=Inter:wght@300;400;500;600;700&display=swap');

/* Apply Inter font to all elements */
* {
  font-family: 'Inter', sans-serif;
}

/* main navbar container styling */
.navbar {
  background-color: rgba(245, 242, 233, 0.85); /* light beige with transparency */
  backdrop-filter: blur(8px); /* blur effect for background */
  -webkit-backdrop-filter: blur(8px); /* safari support for blur */
  padding: 1rem 0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.03); /* subtle shadow */
  border-bottom: 1px solid rgba(0, 0, 0, 0.05); /* light bottom border */
  position: sticky; /* sticks to top on scroll */
  top: 0;
  z-index: 1000; /* stays above other content */
}

/* site logo or brand text */
.navbar-brand {
  font-family: 'Calistoga', serif; /* custom serif font for branding */
  font-size: 1.6rem;
  color: #333333 !important;
  letter-spacing: 0.2px;
  padding-top: 0;
  padding-bottom: 0;
}

/* navigation link style */
.nav-link {
  color: #666666 !important;
  font-size: 0.95rem;
  font-weight: 600;
  padding: 0.5rem 1rem !important;
  transition: color 0.2s ease;
}

/* hover effect for nav links */
.nav-link:hover {
  color: #333333 !important;
}

/* main button inside navbar like login or signup */
.nav-btn {
  background-color: #3a3632; /* dark brown */
  border: none;
  color: white;
  padding: 0.4rem 1rem;
  border-radius: 4px;
  font-size: 0.95rem;
  font-weight: 600;
  transition: all 0.2s ease;
  cursor: pointer;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

/* hover effect for navbar button */
.nav-btn:hover {
  background-color: #4a4540;
  transform: translateY(-1px);
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.15);
}

/* hamburger menu border for mobile */
.navbar-toggler {
  border-color: rgba(0, 0, 0, 0.1);
}
</style>