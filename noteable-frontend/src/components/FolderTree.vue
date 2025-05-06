<template>
  <!-- wrapper for the folder section -->
  <div class="folder-container">
    <!-- header with title and new folder button -->
    <div class="folder-header">
      <h5>Folders</h5>
      <button @click="showCreateModal = true" class="btn btn-sm btn-outline-primary">
        <i class="fas fa-plus"></i> New
      </button>
    </div>
    
    <div class="folder-list">
      <!-- display all notes option -->
      <div class="folder-item">
        <i class="fas fa-book"></i> All Notes
      </div>
      
      <!-- loop through each folder and show name with actions -->
      <div v-for="folder in folders" :key="folder.id" class="folder-item">
        <div class="d-flex justify-content-between w-100">
          <div>
            <i class="fas fa-folder"></i> {{ folder.name }}
          </div>
          <div class="folder-actions">
            <!-- edit folder name -->
            <button @click.stop="promptRenameFolder(folder)" class="btn btn-sm">
              <i class="fas fa-edit"></i>
            </button>
            <!-- delete folder -->
            <button @click.stop="confirmDeleteFolder(folder.id)" class="btn btn-sm">
              <i class="fas fa-trash"></i>
            </button>
          </div>
        </div>
      </div>
    </div>
    
    <!-- modal for creating a new folder -->
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
</template>

<script>
import foldersService from '@/services/foldersService';

export default {
  name: 'FolderTree',
  data() {
    return {
      folders: [],
      showCreateModal: false,
      newFolderName: '',
    };
  },
  emits: ['folder-created'],
  created() {
    this.fetchFolders();
  },
  methods: {
    // load all folders from the server
    async fetchFolders() {
      try {
        const response = await foldersService.getAllFolders();
        this.folders = response.data;
      } catch (error) {
        console.error('Error fetching folders:', error);
      }
    },
    
    // send request to create a new folder
    async createFolder() {
      if (!this.newFolderName.trim()) {
        alert('Folder name cannot be empty');
        return;
      }
      
      try {
        const response = await foldersService.createFolder(this.newFolderName.trim());
        
        this.showCreateModal = false;
        this.newFolderName = '';
        await this.fetchFolders();
        
        // tell parent that a folder was created
        this.$emit('folder-created', response.data);
      } catch (error) {
        console.error('Error creating folder:', error);
        
        if (error.response) {
          alert(`Failed to create folder: ${error.response.status} - ${error.response.statusText || 'Unknown error'}`);
        } else if (error.request) {
          alert('Failed to create folder: No response received from server');
        } else {
          alert(`Failed to create folder: ${error.message}`);
        }
      }
    },
    
    // show prompt to rename a folder
    promptRenameFolder(folder) {
      const newName = prompt('Enter new folder name:', folder.name);
      if (newName && newName.trim() && newName !== folder.name) {
        this.renameFolder(folder.id, newName);
      }
    },
    
    // send request to update folder name
    async renameFolder(folderId, newName) {
      try {
        await foldersService.updateFolder(folderId, newName);
        await this.fetchFolders();
      } catch (error) {
        console.error('Error renaming folder:', error);
        alert('Failed to rename folder');
      }
    },
    
    // ask for confirmation before deleting folder
    confirmDeleteFolder(folderId) {
      if (confirm('Are you sure you want to delete this folder? Notes in this folder will not be deleted')) {
        this.deleteFolder(folderId);
      }
    },
    
    // send request to delete folder
    async deleteFolder(folderId) {
      try {
        await foldersService.deleteFolder(folderId);
        await this.fetchFolders();
      } catch (error) {
        console.error('Error deleting folder:', error);
        alert('Failed to delete folder');
      }
    }
  }
};
</script>

<style scoped>
/* container for folder list section */
.folder-container {
  margin-bottom: 20px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  padding: 10px;
}

/* header section with title and button */
.folder-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
  padding-bottom: 10px;
  border-bottom: 1px solid #e0e0e0;
}

/* scrollable list area */
.folder-list {
  max-height: 300px;
  overflow-y: auto;
}

/* each folder row style */
.folder-item {
  padding: 8px 10px;
  border-radius: 4px;
  margin-bottom: 2px;
  transition: background-color 0.2s;
  display: flex;
  align-items: center;
}

/* highlight on hover */
.folder-item:hover {
  background-color: #f5f5f5;
}

/* hide folder action buttons by default */
.folder-actions {
  display: none;
}

/* show actions when hovering over item */
.folder-item:hover .folder-actions {
  display: block;
}

/* style for action buttons */
.folder-actions button {
  padding: 0 5px;
  background: none;
  border: none;
  color: #757575;
}

/* change color on hover */
.folder-actions button:hover {
  color: #1976d2;
}

/* backdrop for modal */
.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1050;
}
</style>
