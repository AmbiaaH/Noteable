<template>
  <div class="forgot-password">
    <h2>Forgot Password</h2>
    <form @submit.prevent="forgotPassword">
      <!-- user enters email address here -->
      <input v-model="form.email" placeholder="Email" type="email" required />
      <!-- submit button to request reset link -->
      <button type="submit">Submit</button>
    </form>
    <!-- show a message if one exists -->
    <p v-if="message">{{ message }}</p>
  </div>
</template>

<script>
import apiClient from '@/services/apiClient';


export default {
  name: 'ForgotPass',
  data() {
    return {
      form: { email: '' }, // store the user's email
      message: '' // message shown after submitting
    };
  },
  methods: {
    async forgotPassword() {
      try {
        // send a request to backend to start password reset
        await apiClient.post('/auth/forgotpassword', this.form);
        // show success message
        this.message = 'A Link to reset your password has been sent to your email!';
      } catch (error) {
        // show error message if request fails
        this.message = error.response?.data?.message || 'Something went wrong';
      }
    }
  }
};
</script>

<style scoped>
.forgot-password {
  max-width: 400px; /* set max width for the form */
  margin: 0 auto;   /* center it horizontally */
}
</style>

