<template>
  <div class="container mt-5">
    <div class="mb-4">
      <h1 class="homepage-style-heading">My Note Journal</h1>
      <div v-if="isAdmin" class="admin-badge">
        Admin mode: You have global delete permissions
      </div>
    </div>

    <div class="row">
      <div class="col-md-3">
        <div class="card mb-4">
          <div class="card-header d-flex justify-content-between align-items-center">
            <h5 class="m-0">Folders</h5>
            <button @click="showCreateModal = true" class="btn btn-sm btn-outline-secondary">
              Create New Folder
            </button>
          </div>
          <div class="list-group list-group-flush">
            <a
              href="#"
              class="list-group-item list-group-item-action"
              :class="{ active: selectedFolder === null }"
              @click.prevent="selectFolder(null)"
            >
              All Notes
            </a>

            <div
              v-for="folder in folders"
              :key="folder.id"
              class="list-group-item list-group-item-action folder-item d-flex justify-content-between align-items-center"
              :class="{ active: selectedFolder && selectedFolder.id === folder.id }"
            >
              <span class="folder-name" @click.prevent="selectFolder(folder)">{{ folder.name }}</span>
              <div class="folder-actions">
                <button @click.stop="promptRenameFolder(folder)" class="btn btn-sm" style="background-color: #3a3632; color: white;">
                  Edit
                </button>
                <button @click.stop="confirmDeleteFolder(folder.id)" class="btn btn-sm btn-danger">
                  Delete
                </button>
              </div>
            </div>
          </div>
        </div>

        <div v-if="showCreateModal" class="modal-backdrop">
          <div class="modal-dialog">
            <div class="modal-content">
              <div class="modal-header">
                <h5 class="modal-title">Create New Folder</h5>
                <button type="button" class="btn-close" @click="showCreateModal = false"></button>
              </div>
              <div class="modal-body">
                <div class="mb-3">
                  <label for="folderName" class="form-label">Folder Name</label>
                  <input type="text" class="form-control" id="folderName" v-model="newFolderName">
                </div>
              </div>
              <div class="modal-footer">
                <button type="button" class="btn btn-secondary" @click="showCreateModal = false">Cancel</button>
                <button type="button" class="btn btn-primary" @click="createFolder">Create</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="col-md-9">
        <h3 class="mb-3">{{ currentFolderName }}</h3>
        <div class="mb-4">
          <input
            type="text"
            class="form-control"
            placeholder="Search notes..."
            v-model="searchQuery"
          >
        </div>

        <div v-if="isLoading" class="text-center">
          <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>

        <div v-else-if="displayedNotes.length === 0" class="alert alert-info">
          No notes found in this folder.
        </div>

        <div v-else>
          <div
            class="card mb-3"
            v-for="note in displayedNotes"
            :key="note.id"
            :class="{ 'admin-delete-highlight': adminDeleteMode }"
            @click="adminDeleteMode ? confirmDeleteNote(note.id) : null"
          >
            <div class="card-header py-2">
              <h5 class="card-title mb-0">{{ note.title }}</h5>
            </div>
            <div class="card-body py-2">
              <p class="card-text">{{ note.preview || 'No preview available' }}</p>
              <p class="text-muted mb-0">Author: {{ note.author }}</p>
            </div>
            <div class="card-footer py-2 d-flex justify-content-between">
              <button @click="viewPDF(note.id)" class="btn btn-sm" style="background-color: #3a3632; color: white;">
                View PDF
              </button>
              <div class="d-flex align-items-center">
                <div class="save-bookmark-wrapper">
                  <button class="btn-save" @click.stop="toggleFolderDropdown(note.id)">
                    <span class="save-text">Save to</span>
                    <svg width="16" height="20" viewBox="0 0 16 20" fill="none" xmlns="http://www.w3.org/2000/svg" class="bookmark-icon-svg">
                      <path d="M1 3C1 1.89543 1.89543 1 3 1H13C14.1046 1 15 1.89543 15 3V19L8 14L1 19V3Z" stroke="#333" stroke-width="1.5" fill="none"/>
                    </svg>
                  </button>
                  <div v-if="activeSaveDropdown === note.id" class="folder-dropdown">
                    <div class="folder-option" :class="{'active': note.folderId === null}" @click.stop="moveNoteToFolder(note.id, null)">
                      All Notes
                    </div>
                    <div
                      v-for="folder in folders"
                      :key="folder.id"
                      class="folder-option"
                      :class="{'active': note.folderId === folder.id}"
                      @click.stop="moveNoteToFolder(note.id, folder.id)"
                    >
                      {{ folder.name }}
                    </div>
                  </div>
                </div>
                <button @click.stop="removeFromJournal(note.id)" class="btn btn-danger btn-sm ms-2">
                  Remove
                </button>
                <button
                  v-if="isAdmin && !adminDeleteMode"
                  @click.stop="confirmDeleteNote(note.id)"
                  class="btn btn-danger btn-sm ms-2"
                  title="Permanently delete this note from the system"
                >
                  Delete
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import notesService from '@/services/notesService';
import foldersService from '@/services/foldersService';
import { useAuthStore } from '@/store/auth';

export default {
  name: 'NoteJournalPage',
  data() {
    return {
      notes: [],
      folders: [],
      selectedFolder: null,
      isLoading: false,
      searchQuery: '',
      showCreateModal: false,
      newFolderName: '',
      username: 'Student',
      activeSaveDropdown: null,
      adminDeleteMode: false,
      isAdmin: false,
    };
  },
  computed: {
    currentFolderName() {
      return this.selectedFolder ? this.selectedFolder.name : 'All Notes';
    },
    displayedNotes() {
      let filtered = [...this.notes];
      if (this.selectedFolder) filtered = filtered.filter(n => n.folderId === this.selectedFolder.id);
      if (this.searchQuery) {
        const q = this.searchQuery.toLowerCase();
        filtered = filtered.filter(n =>
          (n.title && n.title.toLowerCase().includes(q)) ||
          (n.preview && n.preview.toLowerCase().includes(q)) ||
          (n.author && n.author.toLowerCase().includes(q))
        );
      }
      return filtered;
    }
  },
  created() {
    this.fetchNotes();
    this.fetchFolders();
    this.getUserInfo();
    this.checkAdminStatus();
  },
  mounted() {
    document.addEventListener('mousedown', this.handleClickOutside);
  },
  beforeUnmount() {
    document.removeEventListener('mousedown', this.handleClickOutside);
  },
  methods: {
    checkAdminStatus() {
      this.isAdmin = useAuthStore().isAdmin;
    },
    getUserInfo() {
      const u = useAuthStore().user;
      if (u && u.username) {
        const base = u.username.split('@')[0];
        this.username = base.charAt(0).toUpperCase() + base.slice(1);
      }
    },
    selectFolder(folder) {
      this.selectedFolder = folder;
    },
    async fetchNotes() {
      this.isLoading = true;
      try {
        const res = await notesService.getJournalNotes();
        this.notes = res.data;
      } catch (e) {
        console.error('Error fetching notes:', e);
      } finally {
        this.isLoading = false;
      }
    },
    async fetchFolders() {
      try {
        const res = await foldersService.getAllFolders();
        this.folders = res.data;
      } catch (e) {
        console.error('Error fetching folders:', e);
      }
    },
    async viewPDF(noteId) {
      try {
        const res = await notesService.getFullPdf(noteId);
        const file = new Blob([res.data], { type: 'application/pdf' });
        const url = URL.createObjectURL(file);
        window.open(url, '_blank');
      } catch (e) {
        console.error('PDF error:', e);
        alert('Could not load PDF');
      }
    },
    async removeFromJournal(noteId) {
      // Check if we're in a specific folder or "All Notes"
      if (this.selectedFolder) {
        // If in a specific folder, just remove from this folder
        if (!confirm(`Remove this note from the "${this.selectedFolder.name}" folder?`)) return;
        this.isLoading = true;
        try {
          // Update the note to have no folder (move to "All Notes")
          await notesService.updateNoteFolder(noteId, null);
          
          // Update the local state - only update the folderId without removing from array
          const noteIndex = this.notes.findIndex(n => n.id === noteId);
          if (noteIndex !== -1) {
            this.notes[noteIndex].folderId = null;
          }
          
          // Give user feedback
          alert(`Note removed`);
          
          // Automatically navigate to All Notes to see the note
          this.selectFolder(null);
        } catch (e) {
          console.error('Remove from folder failed:', e);
          alert('Remove failed');
        } finally {
          this.isLoading = false;
        }
      } else {
        // If in "All Notes", completely remove from journal
        if (!confirm('Remove this note from your journal completely?')) return;
        this.isLoading = true;
        try {
          // Use the actual removeFromJournal endpoint
          await notesService.removeFromJournal(noteId);
          // Remove the note from the local array completely
          this.notes = this.notes.filter(n => n.id !== noteId);
        } catch (e) {
          console.error('Remove failed:', e);
          alert('Remove failed');
        } finally {
          this.isLoading = false;
        }
      }
    },
    async confirmDeleteNote(noteId) {
      if (!confirm('Permanently delete this note?')) return;
      this.isLoading = true;
      try {
        await notesService.deleteNote(noteId);
        this.notes = this.notes.filter(n => n.id !== noteId);
        alert('Deleted');
      } catch (e) {
        console.error('Delete error:', e);
        alert('Delete failed');
      } finally {
        this.isLoading = false;
      }
    },
    async createFolder() {
      if (!this.newFolderName.trim()) return alert('Folder name required');
      try {
        await foldersService.createFolder(this.newFolderName.trim());
        this.showCreateModal = false;
        this.newFolderName = '';
        await this.fetchFolders();
      } catch (e) {
        console.error('Create folder error:', e);
        alert('Folder create failed');
      }
    },
    promptRenameFolder(folder) {
      const newName = prompt('Enter new folder name:', folder.name);
      if (newName && newName.trim() !== '' && newName !== folder.name) {
        this.renameFolder(folder.id, newName);
      }
    },
    async renameFolder(folderId, newName) {
      try {
        await foldersService.updateFolder(folderId, newName);
        if (this.selectedFolder && this.selectedFolder.id === folderId) this.selectedFolder.name = newName;
        await this.fetchFolders();
      } catch (e) {
        console.error('Rename error:', e);
      }
    },
    confirmDeleteFolder(id) {
      if (confirm('Delete this folder?')) this.deleteFolder(id);
    },
    async deleteFolder(id) {
      try {
        await foldersService.deleteFolder(id);
        if (this.selectedFolder && this.selectedFolder.id === id) this.selectedFolder = null;
        await this.fetchFolders();
      } catch (e) {
        console.error('Delete folder error:', e);
      }
    },
    toggleFolderDropdown(id) {
      if (this.adminDeleteMode) return;
      this.activeSaveDropdown = this.activeSaveDropdown === id ? null : id;
    },
    handleClickOutside(e) {
      if (!e.target.closest('.save-bookmark-wrapper')) this.activeSaveDropdown = null;
    },
    async moveNoteToFolder(noteId, folderId) {
      try {
        this.isLoading = true;
        await notesService.updateNoteFolder(noteId, folderId);
        const idx = this.notes.findIndex(n => n.id === noteId);
        if (idx !== -1) this.notes[idx].folderId = folderId;
        this.activeSaveDropdown = null;
      } catch (e) {
        console.error('Move error:', e);
        alert('Move failed');
      } finally {
        this.isLoading = false;
      }
    }
  }
};
</script>

<style scoped>
.card {
  margin-bottom: 1rem;
  border-radius: 0.375rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  position: relative;
  overflow: visible !important;
  z-index: 1;
}
.card:hover {
  z-index: 2;
}
.folder-dropdown {
  position: absolute;
  top: 100%;
  right: 0;
  z-index: 99999;
  background: white;
  box-shadow: 0 2px 10px rgba(0,0,0,0.2);
  padding: 5px 0;
  width: 140px;
  pointer-events: auto;
}
.folder-option {
  padding: 0.5rem 0.75rem;
  cursor: pointer;
  transition: background 0.2s;
  font-size: 0.875rem;
  pointer-events: auto;
}
.folder-option:hover {
  background: #f8f8f8;
}
.folder-option.active {
  background: #6c757d;
  color: white;
}
.save-bookmark-wrapper {
  position: relative;
  z-index: 3;
  display: inline-block;
  border: none;
}
.container, .row, .col-md-9 {
  position: static;
  overflow: visible;
}
.list-group-item.active {
  background-color: transparent !important;
  color: #333 !important;
  font-weight: bold;
}

</style>