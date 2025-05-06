<template>
  <!-- layout with two columns: branding info on the left, login form on the right -->
  <div class="container vh-100">
    <div class="row h-100 align-items-center">

      <!-- left side shows app name and features -->
      <div class="col-md-6">
        <h1 class="fw-bold display-5">Noteable</h1>
        <p class="fw-semibold text-secondary fs-4">Your ultimate platform for academic collaboration and growth</p>

        <p class="mb-4">Noteable connects students with resources and mentors to help them succeed</p>

        <!-- feature: share notes -->
        <div class="d-flex align-items-start mb-4">
          <div class="me-3" style="min-width: 30px;">
            <!-- icon -->
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" viewBox="0 0 16 16">
              <path d="M5 10.5a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5m0-2a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5"/>
              <path d="M3 0h10a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2v-12a2 2 0 0 1 2-2m0 1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1z"/>
            </svg>
          </div>
          <div>
            <h5 class="fw-bold">Share & Access Notes</h5>
            <p class="text-secondary">Exchange study materials with peers from your classes</p>
          </div>
        </div>

        <!-- feature: connect with mentors -->
        <div class="d-flex align-items-start mb-4">
          <div class="me-3" style="min-width: 30px;">
            <!-- icon -->
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" viewBox="0 0 16 16">
              <path d="M15 14s1 0 1-1-1-4-5-4-5 3-5 4 1 1 1 1zm-7.978-1L7 12.996c.001-.264.167-1.03.76-1.72C8.312 10.629 9.282 10 11 10c1.717 0 2.687.63 3.24 1.276.593.69.758 1.457.76 1.72l-.008.002-.014.002zM11 7a2 2 0 1 0 0-4 2 2 0 0 0 0 4m3-2a3 3 0 1 1-6 0 3 3 0 0 1 6 0M6.936 9.28a6 6 0 0 0-1.23-.247A7 7 0 0 0 5 9c-4 0-5 3-5 4q0 1 1 1h4.216A2.24 2.24 0 0 1 5 13c0-1.01.377-2.042 1.09-2.904.243-.294.526-.569.846-.816M4.92 10A5.5 5.5 0 0 0 4 13H1c0-.26.164-1.03.76-1.724.545-.636 1.492-1.256 3.16-1.275ZM1.5 5.5a3 3 0 1 1 6 0 3 3 0 0 1-6 0m3-2a2 2 0 1 0 0 4 2 2 0 0 0 0-4"/>
            </svg>
          </div>
          <div>
            <h5 class="fw-bold">Connect with Mentors</h5>
            <p class="text-secondary">Get help from experienced mentors for your questions</p>
          </div>
        </div>

        <!-- feature: organize your notes -->
        <div class="d-flex align-items-start mb-4">
          <div class="me-3" style="min-width: 30px;">
            <!-- icon -->
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" viewBox="0 0 16 16">
              <path fill-rule="evenodd" d="M6 8V1h1v6.117L8.743 6.07a.5.5 0 0 1 .514 0L11 7.117V1h1v7a.5.5 0 0 1-.757.429L9 7.083 6.757 8.43A.5.5 0 0 1 6 8"/>
              <path d="M3 0h10a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2v-12a2 2 0 0 1 2-2m0 1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1z"/>
            </svg>
          </div>
          <div>
            <h5 class="fw-bold">Organize Your Notes</h5>
            <p class="text-secondary">Save all your notes in one place for easy access</p>
          </div>
        </div>
      </div>

      <!-- right side has the login form -->
      <div class="col-md-6 d-flex justify-content-center align-items-center">
        <div class="d-flex flex-column align-items-center">
          <!-- login section title -->
          <div class="mb-3 text-center">
            <p class="fw-semibold text-secondary fs-4">Join us here!</p>
          </div>

          <!-- login form box -->
          <div class="card shadow login-box">
            <form @submit.prevent="login" class="login-form">
              <div class="form-group">
                <label for="username" class="form-label">Username</label>
                <input
                  id="username"
                  v-model="form.username"
                  type="text"
                  class="form-control input-field"
                  placeholder="Enter your username"
                  required
                />
              </div>
              <div class="form-group">
                <label for="password" class="form-label">Password</label>
                <input
                  id="password"
                  v-model="form.password"
                  type="password"
                  class="form-control input-field"
                  placeholder="Enter your password"
                  required
                />
              </div>
              <button type="submit" class="btn btn-primary w-100 login-button">Log In</button>

              <!-- error message if login fails -->
              <div v-if="message" 
                   :class="['mt-4', 'text-center', errorClass]">
                {{ message }}
              </div>

              <!-- links for forgot password and register -->
              <div class="mt-4 d-flex justify-content-between">
                <router-link to="/forgotpassword">Forgot Password?</router-link>
                <router-link to="/register">Register</router-link>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>


<script>
import { useAuthStore } from '@/store/auth';

export default {
  name: 'LoginPage',
  data() {
    return {
      form: {
        username: '',
        password: ''
      },
      message: '',          // shows any login error or success message
      errorClass: 'text-danger'  // bootstrap class for red text
    };
  },
  methods: {
    async login() {
      // clear any old messages
      this.message = '';
      this.errorClass = 'text-danger';
      
      console.log('Login button clicked. Form data:', this.form);
      
      const authStore = useAuthStore();
      
      try {
        // make sure both fields are filled
        if (!this.form.username || !this.form.password) {
          this.message = 'Please enter both username and password';
          return;
        }

        // try logging in with auth store
        const result = await authStore.login(this.form);
        
        if (result.success) {
          console.log('Login successful, redirecting to /home...');
          this.$router.push('/home'); // go to homepage after login
        } else {
          // show message from backend if login fails
          this.message = result.message || 'Login failed. Please try again';
        }
      } catch (error) {
        console.error('Unexpected login error:', error);
        
        // different error handling based on type of failure
        if (error.response) {
          switch (error.response.status) {
            case 401:
              this.message = 'Invalid username or password';
              break;
            case 500:
              this.message = 'Server error. Please try again later';
              break;
            default:
              this.message = error.response.data?.message || 'An unexpected error occurred';
          }
        } else if (error.request) {
          this.message = 'No response from server. Please check your connection';
        } else {
          this.message = 'Error processing login. Please try again';
        }
      }
    }
  }
}
</script>


<style scoped>
/* set fixed size and padding for login card */
.login-box {
  width: 400px !important;
  max-width: 450px !important;
  min-width: 400px !important;
  padding: 30px !important;
}

/* make form use full width */
.login-form {
  width: 100%;
}

/* add space between input fields */
.form-group {
  margin-bottom: 20px !important;
}

/* space between label and input */
.form-label {
  margin-bottom: 8px !important;
  font-weight: 500;
}

/* style input fields: taller with padding */
.input-field {
  height: 45px !important;
  padding: 10px 15px !important;
  font-size: 16px !important;
}

/* style login button: larger and spaced */
.login-button {
  height: 48px !important;
  font-size: 16px !important;
  margin-top: 10px !important;
  padding: 10px !important;
}

/* add top margin for links below the button */
.mt-4 {
  margin-top: 25px !important;
}
</style>
