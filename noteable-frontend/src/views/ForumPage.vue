<template>
  <!-- Forum main container -->
  <div class="forum-container">
    <!-- Question list section (default view) -->
    <div v-if="!selectedQuestion" class="questions-list-view">
      <!-- Header with title and Ask button -->
      <div class="forum-header">
        <div class="header-left">
          <h1 class="forum-title">Community Forum</h1>
          <p class="forum-subtitle">Feel free to ask mentors any questions you have! They're here to help</p>
        </div>
        <div class="header-right">
          <button @click="showQuestionModal = true" class="ask-question-btn">
            Ask a Question
          </button>
        </div>
      </div>

      <!-- Admin-only info banner -->
      <div v-admin class="admin-controls">
        <span class="admin-badge">Admin mode: You have global delete permissions</span>
      </div>

      <!-- Question creation modal -->
      <div v-if="showQuestionModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h2>Ask a Question</h2>
            <button @click="showQuestionModal = false" class="close-btn">&times;</button>
          </div>
          <form @submit.prevent="submitQuestion">
            <div class="form-group">
              <label>Question Title</label>
              <input v-model="newQuestion.title" type="text" placeholder="Enter your question" required>
            </div>
            <div class="form-group">
              <label>Category</label>
              <select v-model="newQuestion.category" required>
              <option value="">Select Category</option>
              <option v-for="course in courseOptions" :key="course" :value="course">
               {{ course }}
              </option>
              </select>

            </div>
            <div class="form-group">
              <label>Question Details</label>
              <textarea v-model="newQuestion.description" placeholder="Provide more context..." required rows="6"></textarea>
            </div>
            <div class="modal-actions">
              <button type="button" @click="showQuestionModal = false" class="btn-cancel">Cancel</button>
              <button type="submit" class="btn-submit" :disabled="isSubmitting">
                {{ isSubmitting ? 'Posting...' : 'Post Question' }}
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Render list of all questions -->
      <div class="questions-grid">
        <div v-for="question in questions" :key="question.id" class="question-card">
          <div class="question-header">
            <h3 @click="viewQuestionDetails(question)">{{ question.title }}</h3>
            <span class="category-badge">{{ question.category }}</span>
          </div>
          <p class="question-description" @click="viewQuestionDetails(question)">{{ truncateDescription(question.description) }}</p>
          <div class="question-footer">
            <div class="question-meta">
              <img :src="userAvatars[question.authorName] || ''" @error="loadFallbackAvatar(question.authorName)" class="author-avatar">
              <span class="author-name">{{ question.authorName }}</span>
              <span class="post-date">{{ formatDate(question.createdAt) }}</span>
            </div>
            <div class="question-actions">
              <span class="comments-count" @click="viewQuestionDetails(question)">{{ question.commentsCount || 0 }} Comments</span>
              <!-- Admin delete button -->
              <button
                v-admin:delete
                @click.stop="handleAdminDeleteQuestion(question.id)"
                title="Delete this question"
                :disabled="isDeleting === question.id"
              >
                {{ isDeleting === question.id ? 'Deleting...' : 'Delete' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Single question detail section -->
    <div v-else class="question-detail-view">
      <div class="question-detail-card">
        <a @click.prevent="selectedQuestion = null" class="back-link">
          &larr; Back to Questions
        </a>

        <!-- Admin delete button in detail view -->
        <div v-admin class="admin-controls">
          <div class="admin-actions">
            <button
              v-admin:delete
              @click="handleAdminDeleteQuestion(selectedQuestion.id)"
              title="Delete this question and return to forum"
              :disabled="isDeleting === selectedQuestion.id"
            >
              {{ isDeleting === selectedQuestion.id ? 'Deleting...' : 'Delete Question' }}
            </button>
          </div>
        </div>

        <!-- Question content section -->
        <div class="question-card-detail">
          <div class="question-content">
            <h2 class="question-title">{{ selectedQuestion.title }}</h2>
            <span class="category-badge">{{ selectedQuestion.category }}</span>
            <p class="question-description">{{ selectedQuestion.description }}</p>
          </div>

          <div class="question-author-box">
            <img :src="userAvatars[selectedQuestion.authorName] || ''" @error="loadFallbackAvatar(selectedQuestion.authorName)" class="question-avatar">
            <div class="question-author-details">
              <span class="question-author">{{ selectedQuestion.authorName }}</span>
              <span class="question-date">{{ formatDate(selectedQuestion.createdAt) }}</span>
            </div>
          </div>
        </div>

        <!-- Comment section with list and add form -->
        <div class="comments-section">
          <div class="comments-header">
            <h3 class="comments-title">Comments ({{ comments.length }})</h3>
          </div>

          <!-- New comment form -->
          <div class="comment-form">
            <textarea v-model="newComment" class="comment-textarea" placeholder="Add a comment..." rows="4"></textarea>
            <button @click="submitComment" class="post-comment-btn" :disabled="!newComment">
              Post Comment
            </button>
          </div>

          <!-- Comments list -->
          <div v-if="comments.length > 0" class="comments-list">
            <div v-for="comment in comments" :key="comment.id" class="comment-card">
              <div class="comment-header">
                <img :src="userAvatars[comment.authorName] || ''" @error="loadFallbackAvatar(comment.authorName)" class="comment-avatar">
                <div class="comment-meta">
                  <div class="comment-author-line">
                    <span class="comment-author">{{ comment.authorName }}</span>
                    <span class="comment-date">{{ formatDate(comment.createdAt) }}</span>
                  </div>
                  <p class="comment-content">{{ comment.content }}</p>
                </div>
              </div>

              <!-- Admin delete for individual comments -->
              <div v-admin class="admin-comment-actions">
                <button
                  v-admin:delete
                  @click="handleAdminDeleteComment(comment.id)"
                  title="Delete this comment"
                  :disabled="isDeleting === comment.id"
                >
                  {{ isDeleting === comment.id ? 'Deleting...' : 'Delete' }}
                </button>
              </div>
            </div>
          </div>
          <p v-else class="no-comments">No comments yet. Be the first to add one</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, computed } from 'vue'
import apiClient from '@/services/apiClient'
import { useAuthStore } from '@/store/auth'
import { courseOptions } from '@/constants/courses';


export default {
  name: 'ForumPage',
  setup() {
    // setup store and state
    const authStore = useAuthStore()
    const questions = ref([])
    const selectedQuestion = ref(null)
    const comments = ref([])
    const newQuestion = ref({
      title: '',
      category: '',
      description: ''
    })
    const newComment = ref('')
    const showQuestionModal = ref(false)
    const isSubmitting = ref(false)
    const currentPage = ref(1)
    const pageSize = ref(10)
    const totalQuestions = ref(0)
    const userAvatars = ref({})
    const isDeleting = ref(null)

    // total pages for pagination
    const totalPages = computed(() => 
      Math.ceil(totalQuestions.value / pageSize.value)
    )

    // fetch list of forum questions
    const fetchQuestions = async () => {
      try {
        const response = await apiClient.get('/api/forumquestions', {
          params: {
            page: currentPage.value,
            pageSize: pageSize.value
          },
          headers: {
            'Authorization': `Bearer ${authStore.token}`
          }
        })

        questions.value = response.data.questions
        totalQuestions.value = response.data.totalCount

        await loadUserAvatars()
      } catch (error) {
        console.error('Error fetching questions:', error.response ? error.response.data : error.message)
      }
    }

    // fetch comments for a specific question
    const fetchComments = async (questionId) => {
      try {
        const response = await apiClient.get(`/api/ForumComments/${questionId}`, {
          headers: {
            'Authorization': `Bearer ${authStore.token}`
          }
        })
        comments.value = response.data
      } catch (error) {
        console.error('Error fetching comments:', error.response ? error.response.data : error.message)
      }
    }

    // switch view to see selected question and its comments
    const viewQuestionDetails = async (question) => {
      selectedQuestion.value = question
      await fetchComments(question.id)
    }

    // submit a new forum question
    const submitQuestion = async () => {
      if (isSubmitting.value) return

      isSubmitting.value = true
      try {
        await apiClient.post('/api/forumquestions', newQuestion.value, {
          headers: {
            'Authorization': `Bearer ${authStore.token}`
          }
        })

        newQuestion.value = {
          title: '',
          category: '',
          description: ''
        }
        showQuestionModal.value = false
        currentPage.value = 1
        await fetchQuestions()
      } catch (error) {
        console.error('Error submitting question:', error.response ? error.response.data : error.message)
      } finally {
        isSubmitting.value = false
      }
    }

    // submit a comment on a question
    const submitComment = async () => {
      if (!selectedQuestion.value) return

      try {
        await apiClient.post('/api/ForumComments', {
          forumQuestionId: selectedQuestion.value.id,
          content: newComment.value,
          authorId: ''
        }, {
          headers: {
            'Authorization': `Bearer ${authStore.token}`
          }
        })

        newComment.value = ''
        await fetchComments(selectedQuestion.value.id)
        await fetchQuestions()
        selectedQuestion.value.commentsCount = comments.value.length
      } catch (error) {
        console.error('Error submitting comment:', error.response ? error.response.data : error.message)
      }
    }

    // delete a question as admin
    const handleAdminDeleteQuestion = async (questionId) => {
      try {
        isDeleting.value = questionId

        const confirmDelete = window.confirm('Are you sure you want to delete this question? This will also delete all its comments and cannot be undone.')
        if (!confirmDelete) {
          isDeleting.value = null
          return
        }

        await apiClient.post(`/api/forumquestions/remove/${questionId}`, {}, {
          headers: {
            'Authorization': `Bearer ${authStore.token}`
          }
        })

        if (selectedQuestion.value && selectedQuestion.value.id === questionId) {
          selectedQuestion.value = null
        }

        questions.value = questions.value.filter(q => q.id !== questionId)
      } catch (error) {
        console.error('Error deleting question:', error)
        alert('Failed to delete question')
      } finally {
        isDeleting.value = null
      }
    }

   // Fix for handleAdminDeleteComment function
const handleAdminDeleteComment = async (commentId) => {
  try {
    isDeleting.value = commentId;

    const confirmDelete = window.confirm('Are you sure you want to delete this comment? This action cannot be undone.');
    if (!confirmDelete) {
      isDeleting.value = null;
      return;
    }

    // Include the auth token in the delete request
    const response = await apiClient.delete(`/api/forumcomments/${commentId}`, {
      headers: {
        'Authorization': `Bearer ${authStore.token}`
      }
    });

    // Only update UI if delete was successful
    if (response.status === 204) { // 204 No Content is the success response for DELETE
      comments.value = comments.value.filter(c => c.id !== commentId);
      if (selectedQuestion.value) {
        selectedQuestion.value.commentsCount = comments.value.length;
      }
    }

    // Update question list to reflect the new comment count
    await fetchQuestions();
  } catch (error) {
    console.error('Error deleting comment:', error);
    
    // More detailed error logging to help with debugging
    if (error.response) {
      console.error('Response status:', error.response.status);
      console.error('Response data:', error.response.data);
    }
    
    // Show the specific error message if available
    const errorMessage = error.response?.data || 'Failed to delete comment';
    alert(`Error: ${errorMessage}`);
  } finally {
    isDeleting.value = null;
  }
}

    // change pagination page
    const changePage = async (page) => {
      if (page > 0 && page <= totalPages.value) {
        currentPage.value = page
        await fetchQuestions()
      }
    }

    // format date to readable string
    const formatDate = (dateString) => {
      return new Date(dateString).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      })
    }

    // shorten long descriptions
    const truncateDescription = (description, maxLength = 200) => {
      return description.length > maxLength 
        ? description.substring(0, maxLength) + '...'
        : description
    }

    // fetch avatar for a user or generate fallback
    const getUserAvatar = async (username) => {
      if (userAvatars.value[username]) return userAvatars.value[username]

      try {
        const response = await apiClient.get(`/api/user/byUsername/${username}`, {
          headers: {
            'Authorization': `Bearer ${authStore.token}`
          }
        })

        if (response.data?.profilePicture) {
          const avatarUrl = `data:image/jpeg;base64,${response.data.profilePicture}`
          userAvatars.value[username] = avatarUrl
          return avatarUrl
        } else {
          return generateFallbackAvatar(username)
        }
      } catch (error) {
        console.error('Error fetching user avatar:', error)
        return generateFallbackAvatar(username)
      }
    }

    // generate SVG fallback avatar
    const generateFallbackAvatar = (username) => {
      const colors = ['#FF6B6B', '#4ECDC4', '#45B7D1', '#FDCB6E', '#6C5CE7']
      const colorIndex = username.length % colors.length
      const svgAvatar = `data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='40' height='40' viewBox='0 0 40 40'><circle cx='20' cy='20' r='20' fill='${colors[colorIndex]}'/><text x='50%' y='50%' text-anchor='middle' dy='.3em' fill='white' font-size='20'>${username.charAt(0).toUpperCase()}</text></svg>`
      userAvatars.value[username] = svgAvatar
      return svgAvatar
    }

    // load avatars for all authors in question list
    const loadUserAvatars = async () => {
      for (const question of questions.value) {
        if (!userAvatars.value[question.authorName]) {
          await getUserAvatar(question.authorName)
        }
      }
    }

    // show fallback avatar when error occurs
    const loadFallbackAvatar = (username) => {
      generateFallbackAvatar(username)
    }

    // load questions when component mounts
    onMounted(() => {
      fetchQuestions()
    })

    return {
      questions,
      selectedQuestion,
      comments,
      newQuestion,
      newComment,
      submitQuestion,
      submitComment,
      showQuestionModal,
      isSubmitting,
      isDeleting,
      fetchQuestions,
      viewQuestionDetails,
      changePage,
      currentPage,
      totalQuestions,
      pageSize,
      totalPages,
      formatDate,
      truncateDescription,
      getUserAvatar,
      userAvatars,
      loadFallbackAvatar,
      handleAdminDeleteQuestion,
      handleAdminDeleteComment,
      courseOptions,
    }
  }
}
</script>


<style scoped>
/* import Inter font from Google Fonts */
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');

/* apply border-box sizing model to all elements */
*, *::before, *::after {
  box-sizing: border-box;
}

/* remove default margin from all elements */
* {
  margin: 0;
}

/* ensure full height layout */
html, body {
  height: 100%;
}

/* define CSS variables for colors, fonts, and layout */
:root {
  --button-primary: #333333 /* dark gray for main buttons */;
  --button-primary-hover: #4a4a4a /* hover color for main buttons */;
  --button-gray: #8A8A8A /* used for secondary buttons */;
  --header-color: #4A2A40 /* dark purple for headers */;
  --bg-color: #F5F1EA /* beige background for overall layout */;
  --bg-color-light: #FFFFFF /* white background for sections or cards */;
  --bg-color-card: #FCFCFC /* light gray for cards */;

  --text-heading: #333333 /* heading text color */;
  --text-title: #333333 /* title text color */;
  --text-body: #333333 /* body text color */;
  --text-muted: #666666 /* faded secondary text */;

  --card-border: #E0E0E0 /* border color for cards */;
  --card-shadow: 0 1px 2px rgba(0, 0, 0, 0.05) /* light card shadow */;
}

/* set Inter as the default font and base styles for body */
body {
  font-family: 'Inter', sans-serif;
  line-height: 1.5;
  -webkit-font-smoothing: antialiased;
  background-color: var(--bg-color);
  color: var(--text-body);
  margin: 0;
  padding: 0;
}

/* make media elements fully responsive */
img, picture, video, canvas, svg {
  display: block;
  max-width: 100%;
}

/* inherit font styles for form elements */
input, button, textarea, select {
  font: inherit;
}

/* allow text wrapping for long headings */
p, h1, h2, h3, h4, h5, h6 {
  overflow-wrap: break-word;
}

/* container for the whole forum */
.forum-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
  background-color: #F5F1EA;
  box-shadow: none;
}

/* layout for forum header with two columns */
.forum-header {
  display: grid;
  grid-template-columns: 70% 30%;
  width: 100%;
  margin-bottom: 2rem;
}

/* styles for left side of forum header */
.header-left {
  justify-self: start;
  align-self: start;
  text-align: left;
}

/* main title for the forum */
.forum-title {
  font-size: 2rem;
  font-weight: 700;
  color: var(--text-heading);
  margin-top: 0;
  margin-bottom: 0.5rem;
  text-align: left;
}

/* subtitle under the forum title */
.forum-subtitle {
  font-size: 1.125rem;
  color: var(--text-muted);
  margin: 0;
}

/* styles for right side of forum header */
.header-right {
  justify-self: end;
  align-self: start;
}

/* button to open ask question modal */
.ask-question-btn {
  padding: 0.75rem 1.5rem;
  font-size: 1rem;
  font-weight: 600;
  color: white;
  background-color: #333333;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

/* hover effect for ask button */
.ask-question-btn:hover {
  background-color: #4a4a4a;
}

/* focus effect for ask button */
.ask-question-btn:focus {
  outline: none;
  box-shadow: 0 0 0 3px rgba(58, 54, 50, 0.3);
}

/* overlay background for modal */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

/* container for modal content */
.modal-content {
  background-color: white;
  padding: 2rem;
  border-radius: 8px;
  max-width: 600px;
  width: 100%;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1),
              0 10px 10px -5px rgba(0, 0, 0, 0.04);
}

/* layout for modal header */
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

/* modal title */
.modal-header h2 {
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--text-heading);
}

/* close button for modal */
.close-btn {
  font-size: 1.5rem;
  color: var(--text-muted);
  background-color: transparent;
  border: none;
  cursor: pointer;
}

/* spacing for form fields inside modal */
.form-group {
  margin-bottom: 1.5rem;
}

/* label styling for form inputs */
.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: var(--text-body);
}

/* common styles for input, select, and textarea */
input[type="text"], 
select,
textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #000000;
  border-radius: 6px;
  background-color: #FDFDFE;
  color: var(--text-body);
  resize: vertical;
}

/* focus effect for input, select, and textarea */
input[type="text"]:focus,
select:focus, 
textarea:focus {
  outline: none;
  box-shadow: 0 0 0 3px rgba(58, 54, 50, 0.2);
}

/* layout for modal footer buttons */
.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
}

/* cancel button inside modal */
.btn-cancel {
  padding: 0.5rem 1rem;
  font-size: 1rem;
  color: var(--text-body);
  background-color: #F1F5F9;
  border: 1px solid #E2E8F0;
  border-radius: 6px;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

/* hover effect for cancel button */
.btn-cancel:hover {
  background-color: #fafafa;
}

/* submit button inside modal */
.btn-submit {
  padding: 0.5rem 1rem;
  font-size: 1rem;
  font-weight: 600;
  color: white;
  background-color: #333333;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

/* hover effect for submit button */
.btn-submit:hover {
  background-color: #4a4a4a;
}

/* disabled state for submit button */
.btn-submit:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* grid layout for questions */
.questions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
  gap: 1.5rem;
  margin-top: 2rem;
}

.question-card {
  background-color: #FFFFFF; /* white background for each question card */
  border: 1px solid var(--card-border); /* light border using CSS variable */
  border-radius: 8px; /* rounded corners for the card */
  padding: 1.5rem; /* space inside the card */
  box-shadow: var(--card-shadow); /* subtle shadow */
  transition: transform 0.2s ease, box-shadow 0.2s ease; /* smooth hover animation */
  cursor: pointer; /* show pointer when hovering */
  margin-bottom: 1rem; /* spacing between cards */
}

.question-card:hover {
  transform: translateY(-2px); /* slightly lift card on hover */
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1); /* stronger shadow on hover */
}

.question-header {
  display: flex; /* layout title and badge side by side */
  justify-content: space-between; /* space between title and badge */
  align-items: center; /* vertically align items */
  margin-bottom: 1rem; /* spacing below header */
}

.question-header h3 {
  font-size: 1.25rem; /* size of the question title */
  font-weight: 600; /* bold title */
  color: var(--text-title); /* use title color */
  margin: 0; /* remove default margin */
  cursor: pointer; /* make it clickable */
}

.category-badge {
  padding: 0.25rem 0.75rem; /* padding around the text */
  font-size: 0.875rem; /* smaller font */
  font-weight: 500; /* semi-bold */
  color: white; /* white text */
  background-color: #333333; /* dark background */
  border-radius: 20px; /* pill shape */
}

.question-description {
  margin-bottom: 1rem; /* spacing below text */
  color: var(--text-body); /* main body text color */
  line-height: 1.6; /* better readability */
  cursor: pointer; /* indicate it's clickable */
}

.question-footer {
  display: flex; /* layout elements side by side */
  justify-content: space-between; /* space between left and right */
  align-items: center; /* align items vertically */
  color: var(--text-muted); /* muted text color */
  font-size: 0.875rem; /* slightly smaller font */
}

.question-meta {
  display: flex; /* layout avatar and text side by side */
  align-items: center; /* center align */
}

.author-avatar {
  width: 30px; /* avatar size */
  height: 30px;
  border-radius: 50%; /* circular avatar */
  margin-right: 0.75rem; /* spacing after avatar */
}

.author-name {
  margin-right: 1rem; /* spacing between name and date */
}

.post-date {
  font-size: 0.75rem; /* smaller date text */
  color: var(--text-muted); /* muted color */
}

.question-actions {
  display: flex; /* layout buttons side by side */
  align-items: center;
  gap: 10px; /* space between buttons */
}

.comments-count {
  cursor: pointer; /* make it clickable */
}

.back-link {
  display: inline-flex; /* layout arrow and text inline */
  align-items: center;
  color: var(--text-muted); /* muted color */
  font-weight: 500; /* semi-bold */
  text-decoration: none; /* remove underline */
  margin-bottom: 1rem;
  cursor: pointer;
  transition: color 0.3s ease; /* smooth color change */
}

.back-link:hover {
  color: var(--text-body); /* change color on hover */
}


.question-detail-view {
  max-width: 850px; /* limit width of the detail section */
  margin: 0 auto; /* center the content */
  background-color: var(--bg-color); /* background matching page */
  padding-top: 1rem; /* space at the top */
}

.question-detail-card {
  background-color: var(--bg-color-light); /* light background for card */
  border: 1px solid var(--card-border); /* border using variable */
  border-radius: 8px; /* rounded corners */
  padding: 1.5rem 2rem; /* space inside the card */
  box-shadow: var(--card-shadow); /* subtle shadow */
  margin-bottom: 2rem; /* space below the card */
}

.admin-actions {
  display: flex; /* layout buttons in a row */
  justify-content: flex-end; /* align buttons to the right */
  margin-bottom: 1rem; /* space below */
}

.question-card-detail {
  display: flex; /* layout question and author side by side */
  justify-content: space-between; /* space between two sections */
  margin-bottom: 1.5rem; /* space below */
  padding-bottom: 1.5rem; /* space below inside */
  border-bottom: 1px solid var(--card-border); /* divider line */
}

.question-content {
  flex: 1; /* take up remaining space */
}

.question-author-box {
  display: flex; /* layout avatar and text side by side */
  align-items: flex-start; /* align at the top */
  margin-left: 2rem; /* space from question */
  min-width: 150px; /* prevent shrinking too small */
}

.question-title {
  font-size: 1.5rem; /* large title */
  font-weight: 600; /* bold */
  color: var(--text-heading); /* use heading color */
  margin-bottom: 0.5rem; /* space below */
}

.question-avatar {
  width: 40px; /* avatar size */
  height: 40px;
  border-radius: 50%; /* round avatar */
  margin-right: 1rem; /* space after avatar */
}

.question-author-details {
  display: flex;
  flex-direction: column; /* stack name and date */
}

.question-author {
  font-weight: 600; /* bold name */
  color: var(--text-body); /* regular text color */
}

.question-date {
  font-size: 0.875rem; /* small font */
  color: var(--text-muted); /* muted color */
}

/* Comments section container */
.comments-section {
  margin-top: 0.5rem;
}

/* Header for comments block */
.comments-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

/* Title for comments block */
.comments-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--text-heading);
  margin: 0;
}


/* New Comment Form Section */
/* comment form container */
.comment-form {
  margin-bottom: 1.5rem; /* space below form */
}

/* textarea for writing comments */
.comment-textarea {
  width: 100%; /* full width */
  min-height: 80px; /* set minimum height */
  padding: 0.75rem; /* space inside */
  border: 1px solid #E2E8F0; /* light border */
  border-radius: 6px; /* rounded corners */
  background-color: #FDFDFE; /* light background */
  color: var(--text-body); /* normal text color */
  resize: vertical; /* allow vertical resizing */
  margin-bottom: 0.75rem; /* space below */
  outline: none; /* remove blue outline */
  box-shadow: 0 0 0 3px rgba(58, 54, 50, 0.2); /* subtle focus shadow */
}

/* post comment button */
.post-comment-btn {
  padding: 0.5rem 1rem;
  font-size: 1rem;
  font-weight: 600;
  color: white;
  background-color: #8A8A8A; /* gray button */
  border: none;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.post-comment-btn:hover {
  opacity: 0.9; /* slight transparency on hover */
}

.post-comment-btn:disabled {
  opacity: 0.5; /* lighter color when disabled */
  cursor: not-allowed; /* not clickable */
}

/* container for all comments */
.comments-list {
  margin-top: 1rem;
}

/* individual comment box */
.comment-card {
  padding: 1rem;
  background-color: #FCFCFC; /* very light background */
  border: 1px solid #EEEEEE; /* light border */
  border-radius: 8px;
  margin-bottom: 0.75rem;
  position: relative;
}

/* layout for comment content */
.comment-header {
  display: flex;
  align-items: flex-start;
}

/* avatar image in comment */
.comment-avatar {
  width: 30px;
  height: 30px;
  border-radius: 50%; /* round shape */
  margin-right: 0.75rem;
  flex-shrink: 0; /* prevent shrinking */
}

/* container for name, date and content */
.comment-meta {
  flex: 1;
}

/* layout name and date in one row */
.comment-author-line {
  display: flex;
  align-items: center;
  margin-bottom: 0.5rem;
}

/* comment author's name */
.comment-author {
  font-weight: 600;
  color: var(--text-body);
  margin-right: 0.75rem;
}

/* comment date text */
.comment-date {
  font-size: 0.75rem;
  color: var(--text-muted);
}

/* actual comment text */
.comment-content {
  color: var(--text-body);
  line-height: 1.6;
  margin: 0;
}

/* admin button container for comments */
.admin-comment-actions {
  margin-top: 0.5rem;
  display: flex;
  justify-content: flex-end;
}

/* message when there are no comments */
.no-comments {
  padding: 1.5rem;
  border-radius: 8px;
  color: var(--text-muted);
  font-size: 1rem;
  text-align: center;
  background-color: var(--bg-color-card);
  border: 1px solid var(--card-border);
}

/* admin notice box */
.admin-controls {
  margin-bottom: 1rem;
}
</style scoped>