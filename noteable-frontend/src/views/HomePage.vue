<template>
  <div class="main-wrapper">
    <div class="container mt-4">
      <!-- show greeting at top of homepage -->
      <h1 class="welcome-title">Hi, {{ firstName }}!</h1>
      <p class="welcome-subtitle">Wind down and get ready to study!</p>

      <!-- show admin message if user is admin -->
      <div v-admin class="admin-controls">
        <span class="admin-badge">Admin mode: You have global delete permissions</span>
      </div>

      <!-- this is where search bar filter and upload button sit side by side -->
      <div class="row my-4 align-items-center">
        <!-- search bar lets user find notes -->
        <div class="col-md-4 mb-3 mb-md-0">
          <div class="search-container">
            <input
              type="text"
              class="form-control search-input"
              placeholder="Search notes..."
              v-model="searchQuery"
            />
            <button class="search-button" type="button">
              <i class="fas fa-search"></i>
            </button>
          </div>
        </div>

        <!-- dropdown that lets user filter notes by course -->
        <div class="col-md-3 mb-3 mb-md-0">
          <select class="form-select filter-select" v-model="selectedFilter" @change="applyFilter">
            <option value="">All Courses</option>
            <option v-for="option in courseOptions" :key="option">{{ option }}</option>
          </select>
        </div>

        <!-- button that opens file picker to upload a note -->
        <div class="col-md-3 text-md-end">
          <button class="btn-upload" @click="triggerFileInput">
            Upload Note
          </button>
        </div>
      </div>

      <hr class="divider" />

      <!-- show spinner while waiting for notes -->
      <div v-if="loading" class="text-center py-5">
        <div class="spinner-border text-secondary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2">Loading notes...</p>
      </div>

      <!-- show warning if backend is down or can't connect -->
      <div v-else-if="connectionError" class="alert alert-warning" role="alert">
        <strong>Connection Issue:</strong> {{ connectionError }}
        <button @click="retryConnection" class="btn btn-sm btn-outline-dark ms-2">
          Retry Connection
        </button>
      </div>

      <!-- show the notes in a grid layout -->
      <div v-else class="row">
        <div class="col-md-4 mb-4" v-for="note in paginatedNotes" :key="note.id">
          <div class="note-card">
            <div class="card-body">
              <h5 class="note-title">{{ note.title }}</h5>
              <p class="note-preview">{{ note.preview }}</p>
              <p class="note-author">Uploaded by: {{ note.author }}</p>
              <!-- link to open preview of note as image -->
              <div v-if="note.filePath" class="pdf-preview-link">
                <a href="#" @click.prevent="openPdfPreview(note.id)">Preview PDF</a>
              </div>
            </div>
            <div class="card-footer">
              <div class="d-flex justify-content-between align-items-center">
                <!-- button to add note to user's journal -->
                <button class="btn-journal" @click="addToJournal(note.id)">
                  Add to Journal
                </button>
                <!-- button to permanently delete note if admin -->
                <button 
                  v-admin:delete
                  @click="handleAdminDelete(note.id)" 
                  title="Permanently delete this note"
                >
                  Delete
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- show message if no notes match filter or search -->
        <div v-if="filteredNotes.length === 0" class="col-12 text-center py-5">
          <p>No notes found matching your criteria.</p>
        </div>
      </div>

      <!-- pagination controls to switch between pages of notes -->
      <nav v-if="totalPages > 1" aria-label="Page navigation" class="pagination-container">
        <ul class="pagination justify-content-center">
          <li class="page-item" :class="{ disabled: currentPage === 1 }">
            <a class="page-link" href="#" @click.prevent="currentPage--">Previous</a>
          </li>
          <li
            class="page-item"
            v-for="page in totalPages"
            :key="page"
            :class="{ active: currentPage === page }"
          >
            <a class="page-link" href="#" @click.prevent="currentPage = page">{{ page }}</a>
          </li>
          <li class="page-item" :class="{ disabled: currentPage === totalPages }">
            <a class="page-link" href="#" @click.prevent="currentPage++">Next</a>
          </li>
        </ul>
      </nav>

      <!-- file input used when upload button is clicked -->
      <input
        type="file"
        ref="fileInput"
        style="display: none;"
        accept="application/pdf"
        @change="handleFileChange"
      /> 

      <!-- shows when a file is picked -->
      <div v-if="selectedFile" class="upload-modal">
        <div class="modal-overlay" @click="cancelUpload"></div>
        <div class="modal-content">
          <h4 class="modal-title">Create New Note</h4>
          <div class="mb-3">
            <label class="form-label">Title</label>
            <input class="form-control" v-model="newNote.title" placeholder="Enter note title" />
          </div>
          <div class="mb-3">
            <label class="form-label">Preview</label>
            <input class="form-control" v-model="newNote.preview" placeholder="Enter a preview" />
          </div>
          <div class="mb-3">
            <label class="form-label">Author</label>
            <input class="form-control" v-model="newNote.author" placeholder="Enter author name" />
          </div>
          <div class="mb-3">
            <label class="form-label">Category</label>
            <!-- drop down for selecting course category -->
            <select class="form-select" v-model="newNote.category">
              <option disabled value="">Select a course</option>
              <option v-for="option in courseOptions" :key="option" :value="option">
                {{ option }}
              </option>
            </select>
          </div>
          <div class="mb-3">
            <p class="selected-file">Selected file: {{ selectedFile.name }}</p>
          </div>
          <div class="modal-actions">
            <button class="btn-cancel" @click="cancelUpload">Cancel</button>
            <button class="btn-confirm" @click="confirmUpload">Confirm Upload</button>
          </div>
        </div>
      </div>

      <!-- modal that shows a preview image of the pdf note -->
      <div v-if="showPreviewModal" class="preview-modal">
        <div class="modal-overlay" @click="closePreviewModal"></div>
        <div class="preview-content">
          <h5 class="preview-title">PDF Preview</h5>
          <img v-if="previewImageUrl" :src="previewImageUrl" alt="PDF Preview" class="preview-image" />
          <button class="btn-close-preview" @click="closePreviewModal">Close</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
// imports used in this page
import { useAuthStore } from '@/store/auth';
import apiClient from '@/services/apiClient';
import notesService from '@/services/notesService';
import { courseOptions } from '@/constants/courses';

export default {
  name: 'HomePage',

  data() {
    return {
      // default values used for the homepage
      searchQuery: '',
      selectedFilter: '',
      notes: [],
      currentPage: 1,
      notesPerPage: 15,
      selectedFile: null,
      newNote: {
        title: '',
        preview: '',
        author: '',
        category: 'Computer Science'
      },
      showPreviewModal: false,
      previewImageUrl: null,
      firstName: 'there', // default if name not found
      loadingTimeout: null,
      loading: true,
      connectionError: null
    };
  },

  computed: {
    // values here update automatically when data changes
    authStore() {
      return useAuthStore();
    },

    courseOptions() {
      return courseOptions;
    },

    filteredNotes() {
      let temp = this.notes;
      if (this.searchQuery) {
        const query = this.searchQuery.toLowerCase();
        temp = temp.filter(
          note =>
            note.title.toLowerCase().includes(query) ||
            note.preview.toLowerCase().includes(query) ||
            note.author.toLowerCase().includes(query)
        );
      }
      if (this.selectedFilter) {
        temp = temp.filter(n => n.category === this.selectedFilter);
      }
      return temp;
    },

    totalPages() {
      return Math.ceil(this.filteredNotes.length / this.notesPerPage);
    },

    paginatedNotes() {
      const start = (this.currentPage - 1) * this.notesPerPage;
      return this.filteredNotes.slice(start, start + this.notesPerPage);
    }
  },

  // functions go here, used by the page
  methods: {
    // this is used by admins to delete notes
    handleAdminDelete(noteId) {
      this.$adminUtils.deleteItem(
        '/api/notes',
        noteId,
        (deletedId) => {
          this.notes = this.notes.filter(note => note.id !== deletedId);
        }
      );
    },

    // gets first name from user info to show greeting
    updateFirstName() {
      try {
        if (!this.authStore || !this.authStore.user) {
          this.firstName = 'there';
          return;
        }

        const user = this.authStore.user;
        if (user.firstName) {
          this.firstName = user.firstName;
          return;
        }

        if (user.name && typeof user.name === 'string') {
          this.firstName = user.name.split(' ')[0];
          return;
        }

        if (user.username) {
          const match = user.username.match(/^([a-zA-Z]+)/);
          if (match && match[1]) {
            this.firstName = match[1].charAt(0).toUpperCase() + match[1].slice(1).toLowerCase();
          } else {
            this.firstName = user.username;
          }
          return;
        }

        this.firstName = 'there';
      } catch (error) {
        console.error('Error getting first name:', error);
        this.firstName = 'there';
      }
    },

    // checks if the api is reachable
    async testConnection() {
      try {
        const response = await notesService.testConnection();
        if (!response.data.connected) {
          this.connectionError = "Database connection failed. Please check your backend configuration.";
          return false;
        }
        this.connectionError = null;
        return true;
      } catch (error) {
        this.connectionError = "Could not connect to the API server. Please check if the backend is running.";
        return false;
      }
    },

    // called when user clicks retry connection
    async retryConnection() {
      this.loading = true;
      this.connectionError = null;

      const connectionOk = await this.testConnection();
      if (connectionOk) {
        await this.fetchNotes();
      }

      this.loading = false;
    },

    // gets all notes to display on page
    async fetchNotes() {
      this.loading = true;
      try {
        const connectionOk = await this.testConnection();
        if (!connectionOk) {
          this.loading = false;
          return;
        }

        const response = await notesService.getNotes();
        this.notes = response.data;
        this.connectionError = null;
      } catch (error) {
        console.error('Error fetching notes:', error);
        if (error.response) {
          if (error.response.status === 500) {
            this.connectionError = "Server error: The backend encountered an issue processing the request.";
          } else {
            this.connectionError = `API Error (${error.response.status}): ${error.response.data.message || 'Unknown error'}`;
          }
        } else if (error.request) {
          this.connectionError = "Network error: No response received from the server.";
        } else {
          this.connectionError = `Request error: ${error.message}`;
        }

        // use fake notes if the backend fails
        this.notes = [
          {
            id: 1,
            title: 'Sample Note 1',
            preview: 'This is a sample note that appears when the API is unavailable.',
            author: 'System',
            category: 'Computer Science'
          },
          {
            id: 2,
            title: 'Sample Note 2',
            preview: 'Another sample note with different content for variety.',
            author: 'System',
            category: 'Mathematics'
          },
          {
            id: 3,
            title: 'API Troubleshooting',
            preview: 'If you see this note, it means the notes API is not responding correctly.',
            author: 'System',
            category: 'Computer Science'
          }
        ];
      } finally {
        this.loading = false;
      }
    },

    // extra check to get user info from backend if needed
    async fetchUserProfile() {
      try {
        this.updateFirstName();
        if (this.firstName === 'there') {
          try {
            const response = await apiClient.get('/api/users/profile');
            if (response.data) {
              if (response.data.firstName) {
                this.firstName = response.data.firstName;
              } else if (response.data.name) {
                this.firstName = response.data.name.split(' ')[0];
              } else if (response.data.username) {
                this.firstName = response.data.username;
              }
            }
          } catch (error) {
            console.log('Profile API not available, using auth store data');
          }
        }
      } catch (error) {
        console.log('Using default greeting');
      }
    },

    // opens hidden file input
    triggerFileInput() {
      this.$refs.fileInput.click();
    },

    // updates selected file when user picks one
    handleFileChange(event) {
      const file = event.target.files[0];
      if (file) this.selectedFile = file;
    },

    // resets everything when user cancels upload
    cancelUpload() {
      this.selectedFile = null;
      this.newNote = { title: '', preview: '', author: '', category: 'Computer Science' };
      this.$refs.fileInput.value = '';
    },

    // sends note data to backend and uploads the note
    async confirmUpload() {
      const formData = new FormData();
      formData.append('file', this.selectedFile);
      formData.append('title', this.newNote.title);
      formData.append('preview', this.newNote.preview);
      formData.append('author', this.newNote.author);
      formData.append('category', this.newNote.category);
      formData.append('folderId', 1);

      try {
        const response = await apiClient.post('/api/notes', formData, {
          headers: {
            'Content-Type': 'multipart/form-data',
            'Authorization': `Bearer ${this.authStore.token}`
          }
        });

        alert('Note uploaded successfully!');
        this.selectedFile = null;
        await this.fetchNotes();

        this.newNote = {
          title: '',
          preview: '',
          author: '',
          category: 'Computer Science'
        };
        this.$refs.fileInput.value = '';
      } catch (error) {
        console.error('Full error uploading note:', error);
        let errorMessage = 'Failed to upload note.';
        if (error.response) {
          if (error.response.status === 401) {
            errorMessage = 'Unauthorized. Please log in again.';
            this.$router.push('/login');
          } else if (error.response.data && error.response.data.message) {
            errorMessage = error.response.data.message;
          }
        } else if (error.request) {
          errorMessage = 'No response received from the server. Please check your connection.';
        } else {
          errorMessage = 'Error setting up the upload request.';
        }
        alert(errorMessage);
      }
    },

    // adds the note to the user's journal
    async addToJournal(noteId) {
      try {
        await notesService.addToJournal(noteId);
        alert('Note successfully added to journal!');
      } catch (error) {
        console.error('Error adding note to journal:', error.response || error);
        alert('Failed to add note to journal.');
      }
    },

    // shows preview image of the pdf
    async openPdfPreview(noteId) {
      if (this.previewImageUrl) {
        URL.revokeObjectURL(this.previewImageUrl);
        this.previewImageUrl = null;
      }
      try {
        const response = await notesService.getPdfPreview(noteId);
        const blobUrl = URL.createObjectURL(response.data);
        this.previewImageUrl = blobUrl;
        this.showPreviewModal = true;
      } catch (error) {
        console.error('Error fetching PDF preview:', error.response || error);
        alert('Error fetching PDF preview. Check console for details.');
      }
    },

    // hides the pdf preview
    closePreviewModal() {
      this.showPreviewModal = false;
      if (this.previewImageUrl) {
        URL.revokeObjectURL(this.previewImageUrl);
      }
    },

    // used when user changes the course filter
    applyFilter() {
      this.currentPage = 1;
    }
  },

  // runs when page loads for the first time
  created() {
    if (!this.authStore.token) {
      this.$router.push('/login');
      return;
    }

    this.loadingTimeout = setTimeout(() => {
      if (this.loading) {
        this.loading = false;
        this.connectionError = "Request timed out. The server took too long to respond.";
        this.notes = [
          {
            id: 1,
            title: 'Timeout Note',
            preview: 'This note appears because the API took too long to respond.',
            author: 'System',
            category: 'Computer Science'
          }
        ];
      }
    }, 10000);

    this.fetchNotes();
    this.updateFirstName();
    this.fetchUserProfile();
  },

  // runs after the page is shown in the browser
  mounted() {
    this.updateFirstName();
    console.log("Auth store user data:", this.authStore.user);
  },

  // cleans up preview and timeouts before leaving page
  beforeUnmount() {
    if (this.loadingTimeout) {
      clearTimeout(this.loadingTimeout);
    }
    if (this.previewImageUrl) {
      URL.revokeObjectURL(this.previewImageUrl);
    }
  }
};
</script>

<style scoped>
/* load fonts used on this page */
@import url('https://fonts.googleapis.com/css2?family=Calistoga&family=Inter:wght@300;400;500;600;700&display=swap');

/* default font used everywhere */
* {
  font-family: 'Inter', sans-serif;
}

/* wrapper for the whole page */
.main-wrapper {
  background-color: #f5f2e9;
  min-height: calc(100vh - 70px);
  padding-bottom: 2rem;
}

/* hi name message styling */
.welcome-title {
  font-size: 1.8rem;
  font-weight: 700;
  color: #333333;
  margin-bottom: 0.3rem;
}

/* greeting subtitle under the name */
.welcome-subtitle {
  font-size: 1rem;
  font-weight: 400;
  color: #777777;
  margin-bottom: 1.5rem;
}

/* wrapper for the search bar */
.search-container {
  position: relative;
}

/* search bar input box */
.search-input {
  border: 1px solid #e5e5e5;
  border-radius: 4px;
  padding: 0.5rem 2.5rem 0.5rem 1rem;
  font-size: 0.9rem;
  font-weight: 400;
  box-shadow: none;
}

/* styling when input is focused */
.search-input:focus {
  border-color: #d0d0d0;
  box-shadow: 0 0 0 0.1rem rgba(0, 0, 0, 0.05);
}

/* search icon button */
.search-button {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  color: #999;
  cursor: pointer;
}

/* dropdown filter styling */
.filter-select {
  border: 1px solid #e5e5e5;
  border-radius: 4px;
  padding: 0.5rem 1rem;
  font-size: 0.9rem;
  font-weight: 400;
  color: #555;
  background-color: white;
  box-shadow: none;
}

/* when dropdown is clicked */
.filter-select:focus {
  border-color: #d0d0d0;
  box-shadow: 0 0 0 0.1rem rgba(0, 0, 0, 0.05);
}

/* upload note button */
.btn-upload {
  background-color: #3a3632;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  font-size: 0.9rem;
  font-weight: 600;
  transition: all 0.2s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

/* hover effect for upload button */
.btn-upload:hover {
  background-color: #4a4540;
  transform: translateY(-1px);
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.15);
}

/* light divider line */
.divider {
  border-top: 1px solid #f0f0f0;
  margin: 1.5rem 0;
}

/* container for every note */
.note-card {
  background-color: white;
  border: 1px solid rgba(0, 0, 0, 0.06);
  border-radius: 8px;
  transition: all 0.2s ease;
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}

/* hover effect for note */
.note-card:hover {
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.05);
  transform: translateY(-2px);
}

/* inner padding for note content */
.card-body {
  padding: 1.25rem;
  flex-grow: 1;
}

/* note title */
.note-title {
  font-size: 1.1rem;
  font-weight: 600;
  color: #333333;
  margin-bottom: 0.75rem;
}

/* short description under title */
.note-preview {
  font-size: 0.9rem;
  font-weight: 400;
  color: #666666;
  margin-bottom: 0.75rem;
  line-height: 1.5;
}

/* note uploader name */
.note-author {
  font-size: 0.8rem;
  font-weight: 300;
  color: #888888;
  margin-bottom: 0.5rem;
}

/* link to view the file */
.pdf-preview-link a {
  font-size: 0.85rem;
  color: #555555;
  text-decoration: none;
  border-bottom: 1px solid #ddd;
  padding-bottom: 1px;
}

/* preview link hover effect */
.pdf-preview-link a:hover {
  color: #333333;
  border-color: #aaa;
}

/* bottom bar inside each note */
.card-footer {
  padding: 0.75rem 1.25rem;
  background-color: #f9f9f9;
  border-top: 1px solid #f0f0f0;
}

/* add to journal button */
.btn-journal {
  background-color: #f5f5f5;
  color: #555555;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  padding: 0.35rem 0.75rem;
  font-size: 0.8rem;
  font-weight: 500;
  transition: all 0.2s ease;
}

/* journal button hover effect */
.btn-journal:hover {
  background-color: #eeeeee;
  color: #333333;
}

/* spacing for pagination area */
.pagination-container {
  margin-top: 2rem;
}

/* all pagination buttons */
.pagination .page-link {
  color: #666666;
  border: 1px solid #e5e5e5;
  padding: 0.4rem 0.75rem;
  font-size: 0.9rem;
  margin: 0 0.15rem;
}

/* current page is dark */
.pagination .page-item.active .page-link {
  background-color: #333333;
  border-color: #333333;
}

/* disabled buttons like previous or next */
.pagination .page-item.disabled .page-link {
  color: #cccccc;
}

/* modal used for upload and preview */
.upload-modal, .preview-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  z-index: 1050;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* dark overlay behind modals */
.modal-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(3px);
}

/* white box that holds modal content */
.modal-content {
  background: white;
  width: 90%;
  max-width: 500px;
  z-index: 1060;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
}

/* heading of modal */
.modal-title {
  font-size: 1.3rem;
  font-weight: 600;
  color: #333333;
  margin-bottom: 1.25rem;
}

/* label before each input */
.form-label {
  font-weight: 500;
  color: #444444;
}

/* text that shows chosen file name */
.selected-file {
  font-size: 0.9rem;
  color: #666666;
}

/* buttons at bottom of modal */
.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1rem;
}

/* cancel button inside modal */
.btn-cancel {
  background-color: #f5f5f5;
  color: #666666;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  font-size: 0.9rem;
  font-weight: 500;
  transition: all 0.2s ease;
}

/* hover effect for cancel */
.btn-cancel:hover {
  background-color: #e9e9e9;
}

/* confirm upload button */
.btn-confirm {
  background-color: #3a3632;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  font-size: 0.9rem;
  font-weight: 600;
  transition: all 0.2s ease;
}

/* hover effect for confirm */
.btn-confirm:hover {
  background-color: #4a4540;
  transform: translateY(-1px);
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.15);
}

/* content inside pdf preview */
.preview-content {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  max-width: 80%;
  max-height: 80%;
  overflow: auto;
  text-align: center;
  position: relative;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
}

/* title above pdf preview */
.preview-title {
  font-size: 1.2rem;
  font-weight: 600;
  color: #333333;
  margin-bottom: 1rem;
}

/* image inside preview */
.preview-image {
  max-width: 100%;
  height: auto;
  margin-bottom: 1rem;
  border: 1px solid #f0f0f0;
}

/* close button under pdf preview */
.btn-close-preview {
  background-color: #f5f5f5;
  color: #666666;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  font-size: 0.9rem;
  transition: all 0.2s ease;
  font-weight: 500;
}

/* hover effect for close */
.btn-close-preview:hover {
  background-color: #e9e9e9;
}

/* banner that shows error or warning */
.alert {
  border-radius: 8px;
  padding: 1rem;
  margin-bottom: 1.5rem;
}
</style>
