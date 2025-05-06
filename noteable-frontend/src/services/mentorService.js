import apiClient from './apiClient';

export default {
  // Get all mentors with optional filtering
  getAllMentors(params = {}) {
    return apiClient.get('/api/mentor', { params });
  },
  
  // Get a specific mentor by ID
  getMentor(mentorId) {
    return apiClient.get(`/api/mentor/${mentorId}`);
  },
     
  // Get all unique specialties
  getSpecialties() {
    return apiClient.get('/api/mentor/specialties');
  },
  
  // Add or update feedback for a mentor
  addFeedback(mentorProfileId, feedback) {
    return apiClient.post(`/api/mentor/${mentorProfileId}/feedback`, feedback);
  },
  
  // Create or update the current users mentor profile (requires Mentor role)
  updateMentorProfile(profileData) {
    return apiClient.post('/api/mentor', profileData);
  },
  
  // Remove a mentor admin only - using POST endpoint
  removeMentor(mentorId) {
    return apiClient.post(`/api/mentor/delete/${mentorId}`, {}, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    });
  }
}