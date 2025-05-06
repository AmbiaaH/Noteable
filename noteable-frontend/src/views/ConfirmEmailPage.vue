<template>
  <div class="confirm-email">
    <h2>Email Confirmation</h2>
    <p>{{ message }}</p>
  </div>
</template>

<script>
import apiClient from '@/services/apiClient';


export default {
  data() {
    return { message: "Confirming email..." }; // show default message while confirming
  },
  async mounted() {
    // get userId and token from URL query parameters
    const userId = this.$route.query.userId;
    const token = this.$route.query.token;
    try {
      // send request to backend to confirm email
      const response = await apiClient.post('/auth/confirmemail', { userId, token });
      this.message = response.data; // show success message
    } catch (error) {
      // show error message if confirmation fails
      this.message = error.response?.data || "Error confirming email";
    }
  }
};
</script>

  