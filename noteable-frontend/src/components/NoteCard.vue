<template>
  <!-- card layout for showing a single note -->
  <div class="note-card">
    <div class="card-body">
      <!-- note title -->
      <h5 class="note-title">{{ note.title }}</h5>
      <!-- note preview text -->
      <p class="note-preview">{{ note.preview }}</p>
      <!-- uploaded by user -->
      <p class="note-author">Uploaded by: {{ note.author }}</p>
      <!-- link to preview the note's PDF if it exists -->
      <div v-if="note.filePath" class="pdf-preview-link">
        <a href="#" @click.prevent="$emit('preview', note.id)">Preview PDF</a>
      </div>
    </div>

    <!-- action section with journal and delete buttons -->
    <div class="card-footer d-flex justify-content-between align-items-center">
      <slot name="actions">
        <!-- default button to add note to journal -->
        <button class="btn-journal" @click="$emit('add-to-journal', note.id)">
          Add to Journal
        </button>
      </slot>

      <!-- admin-only delete button -->
      <button 
        v-admin:delete
        @click.stop="confirmDelete(note.id)" 
        title="Permanently delete this note"
      >
        Delete
      </button>
    </div>
  </div>
</template>

<script>
import notesService from '@/services/notesService';

export default {
  name: 'NoteCard',
  props: {
    // input note object required from parent
    note: {
      type: Object,
      required: true
    }
  },
  emits: ['deleted', 'preview', 'add-to-journal'],
  methods: {
    // show confirm before deleting and send delete request
    async confirmDelete(noteId) {
      if (!confirm('WARNING: This will permanently delete the note from the system. This action cannot be undone. Continue?')) {
        return;
      }

      try {
        await notesService.deleteNote(noteId);
        this.$emit('deleted', noteId);
      } catch (error) {
        console.error('Error deleting note:', error);
        alert('Failed to delete note: ' + (error.response?.data?.message || error.message));
      }
    }
  }
}
</script>

<style scoped>
/* main card layout */
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

/* hover shadow effect */
.note-card:hover {
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.05);
  transform: translateY(-2px);
}

/* content body inside card */
.card-body {
  padding: 1.25rem;
  flex-grow: 1;
}

/* note title styling */
.note-title {
  font-size: 1.1rem;
  font-weight: 600;
  color: #333333;
  margin-bottom: 0.75rem;
}

/* preview text snippet */
.note-preview {
  font-size: 0.9rem;
  font-weight: 400;
  color: #666666;
  margin-bottom: 0.75rem;
  line-height: 1.5;
}

/* author information */
.note-author {
  font-size: 0.8rem;
  font-weight: 300;
  color: #888888;
  margin-bottom: 0.5rem;
}

/* link style for PDF preview */
.pdf-preview-link a {
  font-size: 0.85rem;
  color: #555555;
  text-decoration: none;
  border-bottom: 1px solid #ddd;
  padding-bottom: 1px;
}

/* hover effect for preview link */
.pdf-preview-link a:hover {
  color: #333333;
  border-color: #aaa;
}

/* footer section with buttons */
.card-footer {
  padding: 0.75rem 1.25rem;
  background-color: #f9f9f9;
  border-top: 1px solid #f0f0f0;
}

/* button to add note to journal */
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

/* hover effect for journal button */
.btn-journal:hover {
  background-color: #eeeeee;
  color: #333333;
}
</style>
