<template>
  <!-- Center everything vertically and horizontally -->
  <div class="container d-flex flex-column justify-content-center align-items-center vh-100">

    <!-- App heading and subheading -->
    <div class="mb-4 text-center">
      <h1 class="fw-bold display-5">Noteable</h1>
      <p class="fw-semibold text-secondary fs-4">Create an Account</p>
    </div>

    <!-- Registration form card -->
    <div class="card p-4 shadow" style="max-width: 400px; width: 100%;">
      <form @submit.prevent="register">
        
        <!-- First Name -->
        <div class="mb-3">
          <label for="firstName" class="form-label">First Name</label>
          <input
            id="firstName"
            v-model="form.firstName"
            type="text"
            class="form-control"
            placeholder="Enter your first name"
            required
          />
        </div>

        <!-- Last Name -->
        <div class="mb-3">
          <label for="lastName" class="form-label">Last Name</label>
          <input
            id="lastName"
            v-model="form.lastName"
            type="text"
            class="form-control"
            placeholder="Enter your last name"
            required
          />
        </div>

        <!-- DOB -->
        <div class="mb-3">
          <label for="dateOfBirth" class="form-label">Date of Birth</label>
          <input
            id="dateOfBirth"
            v-model="form.dateOfBirth"
            type="date"
            class="form-control"
            required
          />
        </div>

        <!-- Username -->
        <div class="mb-3">
          <label for="username" class="form-label">Username</label>
          <input
            id="username"
            v-model="form.username"
            type="text"
            class="form-control"
            placeholder="Enter your username"
            required
          />
        </div>

        <!-- Email -->
        <div class="mb-3">
          <label for="email" class="form-label">Email</label>
          <input
            id="email"
            v-model="form.email"
            type="email"
            class="form-control"
            placeholder="Enter your email"
            required
          />
        </div>

        <!-- Password -->
        <div class="mb-3">
          <label for="password" class="form-label">Password</label>
          <input
            id="password"
            v-model="form.password"
            type="password"
            class="form-control"
            placeholder="Enter your password"
            required
          />
        </div>

        <!-- Confirm Password -->
        <div class="mb-3">
          <label for="confirmPassword" class="form-label">Confirm Password</label>
          <input
            id="confirmPassword"
            v-model="form.confirmPassword"
            type="password"
            class="form-control"
            placeholder="Re-enter your password"
            required
          />
        </div>

        <!-- Register Button -->
        <button type="submit" class="btn btn-primary w-100">Register</button>
      </form>

      <!-- Show any success or error messages -->
      <p v-if="message" class="mt-3 text-center text-danger">{{ message }}</p>
    </div>
  </div>
</template>

<script>
// brings in API calls
import apiClient from '@/services/apiClient'
// pulls in the store for logging the user in after registering
import { useAuthStore } from '@/store/auth'

export default {
  name: 'RegisterPage',
  data() {
    return {
      form: {
        firstName: '',
        lastName: '',
        dateOfBirth: '',
        username: '',
        email: '',
        password: '',
        confirmPassword: ''
      },
      message: '' // shows success or error message
    };
  },
  methods: {
    async register() {
      try {
        // build the registration request body
        const requestData = {
          Username: this.form.username,
          Email: this.form.email,
          Password: this.form.password,
          ConfirmPassword: this.form.confirmPassword,
          FirstName: this.form.firstName,
          LastName: this.form.lastName,
          DateOfBirth: this.form.dateOfBirth
        };

        // send registration data to the server
        const response = await apiClient.post('/auth/register', requestData);

        // show success message from backend
        this.message = response.data.Message || 'User registered successfully!';

        // auto-login user after registering
        const authStore = useAuthStore();
        const loginResult = await authStore.login({
          username: this.form.username,
          password: this.form.password
        });

        // redirect to homepage on success
        if (loginResult.success) {
          this.$router.push('/home');
        } else {
          this.message = loginResult.message || 'Login after registration failed.';
        }

      } catch (error) {
        // show backend error or fallback
        this.message = error.response?.data?.message || 'Registration failed.';
      }
    }
  }
};
</script>
