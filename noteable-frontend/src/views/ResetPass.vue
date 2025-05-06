<template>
  <!-- Centered reset password box -->
  <div class="reset-password">
    <h2>Reset Password</h2>

    <!-- Reset password form -->
    <form @submit.prevent="resetPassword">
      <!-- Email input -->
      <input v-model="form.email" placeholder="Email" type="email" required />

      <!-- Hidden token input (from query param) -->
      <input v-model="form.token" placeholder="Reset Token" hidden />

      <!-- New password input -->
      <input v-model="form.newPassword" placeholder="New Password" type="password" required />

      <!-- Submit button -->
      <button type="submit">Reset Password</button>
    </form>

    <!-- Message display -->
    <p v-if="message">{{ message }}</p>
  </div>
</template>

<script>
// import the API client
import apiClient from '@/services/apiClient'

export default {
  name: 'ResetPass',

  data() {
    return {
      // form holds the user's input
      form: {
        email: '',
        token: '',
        newPassword: ''
      },
      message: '' // shows success or error
    }
  },

  mounted() {
    // fill email and token from the URL query params
    this.form.email = this.$route.query.email || ''
    this.form.token = this.$route.query.token || ''
  },

  methods: {
    async resetPassword() {
      try {
        // send reset request to the backend
        const response = await apiClient.post('/auth/resetpassword', this.form)

        // show success message from server
        this.message = response.data.message || 'Password has been reset!'

        // redirect to login after 2 seconds
        setTimeout(() => {
          this.$router.push('/login')
        }, 2000)

      } catch (error) {
        // fallback error message
        this.message = error.response?.data?.message || 'Password reset failed.'
      }
    }
  }
}
</script>

<style scoped>
/* wrapper box style */
.reset-password {
  max-width: 400px;
  margin: 2rem auto;
  text-align: center;
}

/* input box styles */
input {
  width: 100%;
  padding: 0.5rem;
  margin-bottom: 1rem;
}

/* submit button style */
button {
  padding: 0.5rem 1.5rem;
}
</style>
