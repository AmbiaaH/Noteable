<template>
  <!-- start mentors page -->
  <div class="mentors-page">
    <h1 class="page-title">Mentors</h1>

    <!-- only shows for admin users -->
    <div v-admin class="admin-controls">
      <span class="admin-badge">Admin mode: You have global delete permissions</span>
    </div>

    <!-- top button to become a mentor -->
    <div class="action-buttons">
      <button @click="openRegistrationModal" class="btn primary-btn">
        Become a Mentor
      </button>
    </div>

    <!-- search and filters -->
    <div class="search-filter">
      <input 
        type="text" 
        class="search-input" 
        placeholder="Search mentors by name or specialty..."
        v-model="searchTerm"
        @input="handleSearch"
      >
      <div class="filter-group">
        <!-- dropdown for specialties -->
        <select class="select-filter" v-model="selectedSpecialty" @change="applyFilters">
          <option value="">All Specialties</option>
          <option v-for="specialty in specialties" :key="specialty" :value="specialty">
            {{ specialty }}
          </option>
        </select>
        <!-- dropdown for rating -->
        <select class="select-filter" v-model="minRating" @change="applyFilters">
          <option value="">Rating</option>
          <option value="5">5+ Stars</option>
          <option value="4">4+ Stars</option>
          <option value="3">3+ Stars</option>
        </select>
      </div>
    </div>

    <!-- loading message while data is loading -->
    <div v-if="loading" class="loading-indicator">
      <p>Loading mentors...</p>
    </div>
    
    <!-- message if no mentors found -->
    <div v-else-if="mentors.length === 0" class="no-results">
      <p>No mentors found matching your criteria.</p>
      <button @click="loadTestData" class="btn" style="margin-top: 15px;">Load Test Data</button>
    </div>

    <!-- show mentors grid -->
    <div v-else class="mentors-grid">
      <!-- loop through mentors -->
      <div v-for="mentor in mentors" :key="mentor.id" class="mentor-card-new">
        <div class="mentor-card-content">
          <div class="mentor-image">
            <img 
              :src="mentor.avatarUrl || '/images/default-avatar.png'" 
              :alt="mentor.name" 
              class="mentor-profile-image"
            >
          </div>

          <div class="mentor-details">
            <!-- name and rating -->
            <div class="mentor-header">
              <h3 class="mentor-name">{{ mentor.name }}</h3>

              <!-- show stars -->
              <div class="mentor-rating">
                <span 
                  v-for="n in 5" 
                  :key="n" 
                  class="star"
                >
                  {{ n <= Math.round(mentor.averageRating) ? '★' : '☆' }}
                </span>
                <span class="mentor-rating-count">({{ mentor.ratingCount }} ratings)</span>
              </div>
            </div>

            <!-- title bio and specialties -->
            <div class="mentor-info">
              <p class="mentor-title">{{ mentor.title }}</p>
              <p class="mentor-bio">{{ mentor.bio }}</p>

              <div class="mentor-specialties">
                <h4>Specialties:</h4>
                <div class="specialty-tags">
                  <span 
                    v-for="specialty in mentor.specialties" 
                    :key="specialty"
                    class="specialty-tag"
                  >
                    {{ specialty }}
                  </span>
                </div>
              </div>
            </div>

            <!-- input to leave review -->
            <div class="mentor-review-input">
              <input 
                type="text" 
                placeholder="Leave a review..." 
                class="review-input"
                @click="openFeedbackModal(mentor)"
              >
            </div>
          </div>
        </div>
        
        <!-- delete button only shows for admin -->
        <button 
          v-admin:delete
          @click.stop="handleAdminDelete(mentor.id)" 
          class="admin-delete-btn"
          title="Delete this mentor"
        >
          Delete
        </button>
      </div>
    </div>

    <!-- load more button -->
    <button v-if="hasMoreMentors" class="btn load-more" @click="loadMoreMentors">
      Load More Mentors
    </button>
      
  </div>

  <!-- modal to leave feedback -->
  <div v-if="showFeedbackModal" class="feedback-modal">
    <div class="modal-content">
      <div class="modal-header">
        <h3 class="modal-title">Leave Feedback for {{ selectedMentor?.name }}</h3>
        <button class="close-btn" @click="closeFeedbackModal">&times;</button>
      </div>
      <div class="modal-body">
        <p>Rate your experience:</p>
        <!-- choose star rating -->
        <div class="rating-input">
          <span 
            v-for="n in 5" 
            :key="n" 
            class="star" 
            :class="{ 'filled': n <= feedbackRating }"
            @click="feedbackRating = n"
          >
            {{ n <= feedbackRating ? '★' : '☆' }}
          </span>
        </div>
        <p>Comments (optional):</p>
        <textarea 
          v-model="feedbackComment" 
          placeholder="Share your experience with this mentor..."
        ></textarea>
        <!-- submit feedback -->
        <button class="btn" @click="submitFeedback" :disabled="submittingFeedback">
          {{ submittingFeedback ? 'Submitting...' : 'Submit Feedback' }}
        </button>

        <!-- show old feedback if any -->
        <div v-if="selectedMentor && selectedMentor.feedback && selectedMentor.feedback.length > 0" class="comments-section">
          <h3>Previous Feedback</h3>
          <div v-for="comment in selectedMentor.feedback" :key="comment.id" class="comment">
            <div class="comment-header">
              <span class="comment-user">{{ comment.userName }}</span>
              <span class="comment-date">{{ formatDate(comment.createdAt) }}</span>
            </div>
            <div class="comment-rating">
              <span 
                v-for="n in 5" 
                :key="n" 
                class="star"
              >
                {{ n <= comment.rating ? '★' : '☆' }}
              </span>
            </div>
            <p>{{ comment.comment }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>

  <!-- modal to register as mentor -->
  <div v-if="showRegistrationModal" class="registration-modal">
    <div class="modal-content registration-content">
      <div class="modal-header">
        <h3 class="modal-title">Become a Mentor</h3>
        <button class="close-btn" @click="closeRegistrationModal">&times;</button>
      </div>
      <div class="modal-body">
        <form @submit.prevent="submitMentorProfile" class="mentor-form">
          <!-- name input -->
          <div class="form-group">
            <label for="mentor-name">Name *</label>
            <input 
              id="mentor-name" 
              v-model="mentorForm.name" 
              type="text" 
              required
              placeholder="Your full name"
            >
          </div>

          <!-- title input -->
          <div class="form-group">
            <label for="mentor-title">Professional Title *</label>
            <input 
              id="mentor-title" 
              v-model="mentorForm.title" 
              type="text" 
              required
              placeholder="e.g. Senior Software Engineer"
            >
          </div>

          <!-- bio input -->
          <div class="form-group">
            <label for="mentor-bio">Bio *</label>
            <textarea 
              id="mentor-bio" 
              v-model="mentorForm.bio" 
              rows="4" 
              required
              placeholder="Share your background and expertise"
            ></textarea>
          </div>

          <!-- specialties -->
          <div class="form-group">
            <label>Specialties *</label>
            <div class="specialties-input">
              <div v-for="(specialty, index) in mentorForm.specialties" :key="index" class="specialty-item">
                <input 
                  type="text" 
                  v-model="mentorForm.specialties[index]" 
                  placeholder="e.g. JavaScript"
                >
                <button type="button" @click="removeSpecialty(index)" class="remove-btn">&times;</button>
              </div>
              <button type="button" @click="addSpecialty" class="add-btn">+ Add Specialty</button>
            </div>
          </div>

          <!-- photo upload -->
          <div class="form-group">
            <label for="mentor-avatar">Profile Photo</label>
            <div class="avatar-upload">
              <div class="avatar-preview" v-if="avatarPreview">
                <img :src="avatarPreview" alt="Preview">
              </div>
              <input 
                type="file" 
                id="mentor-avatar" 
                @change="handleAvatarUpload" 
                accept="image/*"
              >
              <p class="help-text">Upload a professional photo (recommended: 400x400px)</p>
            </div>
          </div>

          <!-- form buttons -->
          <div class="form-actions">
            <button type="button" @click="closeRegistrationModal" class="btn cancel-btn">Cancel</button>
            <button type="submit" class="btn submit-btn" :disabled="submittingProfile">
              {{ submittingProfile ? 'Submitting...' : 'Submit Profile' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>


<script>
import { ref, reactive, onMounted, computed } from 'vue'
import mentorsService from '@/services/mentorService'

export default {
  name: 'MentorsPage',
  
  setup() {
    // all data refs
    const mentors = ref([])
    const specialties = ref([])
    const loading = ref(true)
    const page = ref(1)
    const pageSize = ref(8)
    const totalCount = ref(0)
    const searchTerm = ref('')
    const selectedSpecialty = ref('')
    const minRating = ref('')
    const showFeedbackModal = ref(false)
    const selectedMentor = ref(null)
    const feedbackRating = ref(5)
    const feedbackComment = ref('')
    const submittingFeedback = ref(false)

    // register modal data
    const showRegistrationModal = ref(false)
    const mentorForm = ref({
      name: '',
      title: '',
      bio: '',
      specialties: [''],
      avatarUrl: ''
    })
    const avatarPreview = ref('')
    const submittingProfile = ref(false)

    // show if there are more mentors to load
    const hasMoreMentors = computed(() => {
      return mentors.value.length < totalCount.value
    })

    // fetch mentors from api
    const fetchMentors = async (isLoadMore = false) => {
      try {
        loading.value = true
        console.log('Fetching mentors...')

        const params = {
          page: page.value,
          pageSize: pageSize.value
        }

        // add search and filter if needed
        if (searchTerm.value) {
          params.search = searchTerm.value
        }

        if (selectedSpecialty.value) {
          params.specialty = selectedSpecialty.value
        }

        if (minRating.value) {
          params.minRating = parseInt(minRating.value)
        }

        console.log('Request params:', params)
        const response = await mentorsService.getAllMentors(params)
        console.log('FULL API Response:', response)
        console.log('Response data:', response.data)

        // figure out what format the api sent back
        let mentorItems = []

        if (response.data) {
          if (Array.isArray(response.data)) {
            mentorItems = response.data
            console.log('Using array data directly, length:', mentorItems.length)
          } else if (response.data.items) {
            mentorItems = response.data.items
            console.log('Using data.items, length:', mentorItems.length)
          } else if (response.data.results) {
            mentorItems = response.data.results
            console.log('Using data.results, length:', mentorItems.length)
          } else if (response.data.data) {
            mentorItems = response.data.data
            console.log('Using data.data, length:', mentorItems.length)
          } else {
            const arrayProps = Object.entries(response.data)
              .find(([_, value]) => Array.isArray(value))
            
            if (arrayProps) {
              mentorItems = arrayProps[1]
              console.log(`Using data.${arrayProps[0]}, length:`, mentorItems.length)
            } else {
              console.error('Could not find mentor array in response:', response.data)
            }
          }
        }

        if (mentorItems.length > 0) {
          console.log('First mentor sample:', mentorItems[0])
        } else {
          console.log('No mentors found in the response')
        }

        // update list
        if (isLoadMore) {
          mentors.value = [...mentors.value, ...mentorItems]
        } else {
          mentors.value = mentorItems
        }

        // get count
        const count = response.data?.totalCount || 
                     response.data?.total || 
                     response.data?.count || 
                     mentorItems.length

        totalCount.value = count
        console.log('Updated mentors array length:', mentors.value.length)
        console.log('Set totalCount to:', totalCount.value)

      } catch (error) {
        console.error('Error fetching mentors:', error)
        if (error.response) {
          console.error('Error response:', error.response)
          console.error('Error status:', error.response.status)
          console.error('Error data:', error.response.data)
        } else {
          console.error('Error message:', error.message)
        }
      } finally {
        loading.value = false
      }
    }

    // test data for local use
    const loadTestData = () => {
      console.log('Loading test mentor data')

      const testMentors = [
        {
          id: 1,
          mentorId: 'test-mentor-1',
          name: 'Jane Smith',
          title: 'Senior Software Engineer',
          bio: 'Experienced developer with 10+ years in web development and cloud architecture.',
          avatarUrl: 'https://randomuser.me/api/portraits/women/1.jpg',
          specialties: ['JavaScript', 'Vue.js', 'Cloud Architecture'],
          averageRating: 4.8,
          ratingCount: 24
        },
        {
          id: 2,
          mentorId: 'test-mentor-2',
          name: 'John Doe',
          title: 'UX/UI Designer',
          bio: 'Passionate about creating intuitive and beautiful user interfaces.',
          avatarUrl: 'https://randomuser.me/api/portraits/men/2.jpg',
          specialties: ['UI Design', 'User Research', 'Figma'],
          averageRating: 4.5,
          ratingCount: 18
        },
        {
          id: 3,
          mentorId: 'test-mentor-3',
          name: 'Alex Johnson',
          title: 'Full Stack Developer',
          bio: 'Full stack developer with expertise in MERN and LAMP stacks.',
          avatarUrl: 'https://randomuser.me/api/portraits/men/3.jpg',
          specialties: ['React', 'Node.js', 'MongoDB'],
          averageRating: 4.2,
          ratingCount: 15
        }
      ]

      mentors.value = testMentors
      totalCount.value = testMentors.length
      loading.value = false

      console.log('Test data loaded:', mentors.value)
    }

    // fetch all specialties
    const fetchSpecialties = async () => {
      try {
        console.log('Fetching specialties...')
        const response = await mentorsService.getSpecialties()
        console.log('Specialties received:', response.data)
        specialties.value = response.data || []
      } catch (error) {
        console.error('Error fetching specialties:', error)
        console.error('Error details:', error.response || error.message)
      }
    }

    // for load more button
    const loadMoreMentors = () => {
      page.value++
      fetchMentors(true)
    }

    // search debounce
    const handleSearch = () => {
      clearTimeout(window.searchTimeout)
      window.searchTimeout = setTimeout(() => {
        page.value = 1
        fetchMentors()
      }, 300)
    }

    // when filters change
    const applyFilters = () => {
      page.value = 1
      fetchMentors()
    }

    // open feedback popup
    const openFeedbackModal = async (mentor) => {
      try {
        console.log('Opening feedback modal for mentor:', mentor)
        const response = await mentorsService.getMentor(mentor.mentorId)
        console.log('Mentor details received:', response.data)
        selectedMentor.value = response.data
        showFeedbackModal.value = true
        feedbackRating.value = 5
        feedbackComment.value = ''
      } catch (error) {
        console.error('Error fetching mentor details:', error)
        console.error('Error details:', error.response || error.message)
      }
    }

    // close feedback popup
    const closeFeedbackModal = () => {
      showFeedbackModal.value = false
      selectedMentor.value = null
    }

    // submit review
    const submitFeedback = async () => {
      if (!selectedMentor.value || !feedbackRating.value) return

      try {
        submittingFeedback.value = true
        console.log('Submitting feedback:', { rating: feedbackRating.value, comment: feedbackComment.value })

        const feedback = {
          rating: feedbackRating.value,
          comment: feedbackComment.value
        }

        await mentorsService.addFeedback(selectedMentor.value.id, feedback)

        const response = await mentorsService.getMentor(selectedMentor.value.mentorId)
        selectedMentor.value = response.data

        fetchMentors()

        feedbackRating.value = 5
        feedbackComment.value = ''
      } catch (error) {
        console.error('Error submitting feedback:', error)
        console.error('Error details:', error.response || error.message)
      } finally {
        submittingFeedback.value = false
      }
    }

    // delete mentor by id
    const handleAdminDelete = async (mentorId) => {
      try {
        const confirmDelete = window.confirm(
          'Are you sure you want to delete this mentor? This action cannot be undone.'
        )

        if (!confirmDelete) {
          return
        }

        await mentorsService.removeMentor(mentorId)

        mentors.value = mentors.value.filter(mentor => mentor.id !== mentorId)

      } catch (error) {
        console.error('Error deleting mentor:', error)
        alert('Failed to delete mentor. Please try again.')
      }
    }

    // format date to string
    const formatDate = (dateString) => {
      return new Date(dateString).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      })
    }

    // open register modal
    const openRegistrationModal = () => {
      showRegistrationModal.value = true
      mentorForm.value = {
        name: '',
        title: '',
        bio: '',
        specialties: [''],
        avatarUrl: ''
      }
      avatarPreview.value = ''
    }

    // close register modal
    const closeRegistrationModal = () => {
      showRegistrationModal.value = false
    }

    // add new specialty input
    const addSpecialty = () => {
      mentorForm.value.specialties.push('')
    }

    // remove one specialty
    const removeSpecialty = (index) => {
      mentorForm.value.specialties = mentorForm.value.specialties.filter((_, i) => i !== index)
      if (mentorForm.value.specialties.length === 0) {
        mentorForm.value.specialties = ['']
      }
    }

    // handle image upload
    const handleAvatarUpload = (event) => {
      const file = event.target.files[0]
      if (!file) return

      if (!file.type.match('image.*')) {
        alert('Please select an image file')
        return
      }

      const reader = new FileReader()
      reader.onload = (e) => {
        avatarPreview.value = e.target.result
        mentorForm.value.avatarUrl = e.target.result
      }
      reader.readAsDataURL(file)
    }

    // submit new mentor form
    const submitMentorProfile = async () => {
      if (!mentorForm.value.name || !mentorForm.value.title || !mentorForm.value.bio) {
        alert('Please fill out all required fields')
        return
      }

      mentorForm.value.specialties = mentorForm.value.specialties.filter(s => s.trim() !== '')
      if (mentorForm.value.specialties.length === 0) {
        alert('Please add at least one specialty')
        return
      }

      try {
        submittingProfile.value = true

        const profileData = {
          title: mentorForm.value.title,
          bio: mentorForm.value.bio,
          skills: mentorForm.value.specialties.join(','),
          avatarUrl: mentorForm.value.avatarUrl
        }

        console.log('Submitting mentor profile:', profileData)

        const response = await mentorsService.updateMentorProfile(profileData)
        console.log('Profile created:', response.data)

        closeRegistrationModal()
        await fetchMentors()

        alert('Your mentor profile has been created successfully!')
      } catch (error) {
        console.error('Error creating mentor profile:', error)
        console.error('Error details:', error.response || error.message)
        alert('There was an error creating your profile. Please try again.')
      } finally {
        submittingProfile.value = false
      }
    }

    // run once when page loads
    onMounted(async () => {
      console.log('MentorsPage component mounted')
      try {
        await Promise.all([
          fetchMentors(),
          fetchSpecialties()
        ])

        if (mentors.value.length === 0) {
          console.log('No mentors returned from API. Consider using the "Load Test Data" button.')
        }
      } catch (error) {
        console.error('Error during initialization:', error)
      }
    })

    return {
      mentors,
      specialties,
      loading,
      page,
      pageSize,
      totalCount,
      searchTerm,
      selectedSpecialty,
      minRating,
      hasMoreMentors,
      showFeedbackModal,
      selectedMentor,
      feedbackRating,
      feedbackComment,
      submittingFeedback,
      fetchMentors,
      loadMoreMentors,
      handleSearch,
      applyFilters,
      openFeedbackModal,
      closeFeedbackModal,
      submitFeedback,
      handleAdminDelete,
      formatDate,
      loadTestData,
      showRegistrationModal,
      mentorForm,
      avatarPreview,
      submittingProfile,
      openRegistrationModal,
      closeRegistrationModal,
      addSpecialty,
      removeSpecialty,
      handleAvatarUpload,
      submitMentorProfile,
      Math
    }
  }
}
</script>

<style scoped>
/* whole page wrapper */
.mentors-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

/* page title at top */
.page-title {
  margin-bottom: 30px;
  font-size: 2rem;
}

/* blue admin info box */
.admin-controls {
  margin-bottom: 20px;
}

/* button section top right */
.action-buttons {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 20px;
}

/* main button style */
.primary-btn {
  background-color: #4a86e8;
  padding: 10px 16px;
}

.primary-btn:hover {
  background-color: #3a76d8;
}

/* wrapper for search and filters */
.search-filter {
  display: flex;
  justify-content: space-between;
  margin-bottom: 30px;
  flex-wrap: wrap;
  gap: 15px;
}

/* search input box */
.search-input {
  padding: 10px 15px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  width: 350px;
  font-size: 1rem;
}

/* container for dropdowns */
.filter-group {
  display: flex;
  gap: 15px;
}

/* dropdown select style */
.select-filter {
  padding: 10px 15px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  min-width: 180px;
  font-size: 1rem;
}

/* mentor grid layout */
.mentors-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(600px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

/* card outer box */
.mentor-card-new {
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  background-color: white;
  position: relative;
  overflow: hidden;
  transition: box-shadow 0.3s ease;
}

/* shadow on hover */
.mentor-card-new:hover {
  box-shadow: 0 5px 15px rgba(0,0,0,0.1);
}

/* inner layout */
.mentor-card-content {
  display: flex;
}

/* profile image inside card */
.mentor-profile-image {
  width: 100%;
  height: 250px;
  object-fit: cover;
}

/* info area inside card */
.mentor-details {
  flex-grow: 1;
  padding: 20px;
  display: flex;
  flex-direction: column;
}

/* top section name and rating */
.mentor-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

/* big bold name */
.mentor-name {
  font-size: 1.5rem;
  font-weight: bold;
  margin: 0;
}

/* row of stars */
.mentor-rating {
  display: flex;
  align-items: center;
}

/* single star style */
.star {
  color: gold;
  font-size: 1.2rem;
}

/* rating count text */
.mentor-rating-count {
  font-size: 0.8rem;
  color: #555;
  margin-left: 5px;
}

/* info block middle of card */
.mentor-info {
  flex-grow: 1;
}

/* job title */
.mentor-title {
  font-size: 1rem;
  color: #666;
  margin-bottom: 10px;
}

/* short bio */
.mentor-bio {
  margin-bottom: 15px;
}

/* list of skills area */
.mentor-specialties {
  margin-bottom: 15px;
}

/* tag layout */
.specialty-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 5px;
}

/* one tag */
.specialty-tag {
  background-color: #3a3632;
  color: white;
  padding: 5px 10px;
  border-radius: 20px;
  font-size: 0.8rem;
}

/* bottom input for review */
.mentor-review-input {
  margin-top: auto;
}

/* input style for comment */
.review-input {
  width: 100%;
  padding: 10px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  font-size: 0.9rem;
}

/* delete button style */
.admin-delete-btn {
  position: absolute;
  top: 50px;
  right: 10px;
  background-color: #ff4d4d;
  color: white;
  border: none;
  padding: 5px 10px;
  border-radius: 4px;
  cursor: pointer;
  z-index: 10;
}

/* button default */
.btn {
  background-color: #333;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  cursor: pointer;
  font-weight: bold;
}

/* button hover */
.btn:hover {
  background-color: #555;
}

/* load more centered */
.load-more {
  display: block;
  margin: 0 auto;
  padding: 10px 20px;
}

/* popup background */
.feedback-modal, .registration-modal {
  display: flex;
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0,0,0,0.5);
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

/* modal box */
.modal-content {
  background-color: white;
  border-radius: 8px;
  width: 500px;
  max-width: 90%;
  padding: 20px;
  max-height: 80vh;
  overflow-y: auto;
}

/* bigger modal for registration */
.registration-content {
  width: 600px;
  max-width: 90%;
  max-height: 90vh;
}

/* modal top bar */
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

/* modal title text */
.modal-title {
  font-size: 1.5rem;
  font-weight: bold;
}

/* x button */
.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
}

/* row of stars */
.rating-input {
  display: flex;
  gap: 5px;
  margin-bottom: 15px;
}

/* clickable star */
.rating-input .star {
  cursor: pointer;
}

/* gold stars filled */
.rating-input .star.filled {
  color: gold;
}

/* comment text box */
textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  resize: vertical;
  min-height: 100px;
  margin-bottom: 15px;
}

/* feedback section */
.comments-section {
  margin-top: 30px;
}

/* one comment box */
.comment {
  padding: 15px;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  margin-bottom: 15px;
}

/* name and date row */
.comment-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 10px;
}

/* user name bold */
.comment-user {
  font-weight: bold;
}

/* comment date small */
.comment-date {
  color: #555;
  font-size: 0.8rem;
}

/* center text for loading or empty */
.loading-indicator, 
.no-results {
  text-align: center;
  padding: 30px;
  font-size: 1.2rem;
  color: #555;
}

/* form layout */
.mentor-form {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

/* form field */
.form-group {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

/* label bold */
.form-group label {
  font-weight: bold;
}

/* text input and textarea */
.form-group input,
.form-group textarea {
  padding: 10px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  font-size: 1rem;
}

/* column of specialties */
.specialties-input {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

/* row with input and remove button */
.specialty-item {
  display: flex;
  gap: 10px;
}

/* input in row grows */
.specialty-item input {
  flex-grow: 1;
}

/* small x button */
.remove-btn {
  background-color: #f0f0f0;
  border: none;
  border-radius: 50%;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 1.2rem;
}

/* plus add button */
.add-btn {
  background: none;
  border: none;
  color: #3a3632;
  cursor: pointer;
  padding: 5px;
  text-align: left;
  font-weight: bold;
}

/* wrapper for photo upload */
.avatar-upload {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

/* round preview image */
.avatar-preview {
  width: 100px;
  height: 100px;
  border-radius: 50%;
  overflow: hidden;
  margin-bottom: 10px;
}

/* actual img inside preview */
.avatar-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* small text help */
.help-text {
  font-size: 0.8rem;
  color: #666;
  margin-top: 5px;
}

/* button row bottom of form */
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

/* cancel button look */
.cancel-btn {
  background-color: #f0f0f0;
  color: #333;
}

/* submit button style */
.submit-btn {
  background-color: #3a3632;
  color: white;
}

/* hover for submit */
.submit-btn:hover {
  background-color: #4a4540;
}
</style>

