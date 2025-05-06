// src/services/notesService.js
import apiClient from './apiClient'
import { useAuthStore } from '@/store/auth'



export default {
  // fetch all notes from the backend
  async getNotes() {
    try {
      const authStore = useAuthStore()
      return await apiClient.get('/api/notes', {
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      })
    } catch (error) {
      console.error('Error in getNotes service:', error)
      throw error
    }
  },

  // test if the backend notes endpoint is reachable
  async testConnection() {
    try {
      const authStore = useAuthStore()
      return await apiClient.get('/api/notes/test-connection', {
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      })
    } catch (error) {
      console.error('Error in testConnection service:', error)
      throw error
    }
  },

  // get notes that the current user has added to their journal
  async getJournalNotes() {
    try {
      const authStore = useAuthStore()
      return await apiClient.get('/api/notes/my-journal', {
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      })
    } catch (error) {
      console.error('Error in getJournalNotes service:', error)
      throw error
    }
  },

  // add a specific note to the user's journal
  async addToJournal(noteId) {
    try {
      const authStore = useAuthStore()
      return await apiClient.post(`/api/notes/add-to-journal/${noteId}`, {}, {
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${authStore.token}`,
        },
      })
    } catch (error) {
      console.error('Error in addToJournal service:', error)
      throw error
    }
  },

  // remove a specific note from the user's journal
  async removeFromJournal(noteId) {
    try {
      const authStore = useAuthStore()
      return await apiClient.delete(`/api/notes/remove-from-journal/${noteId}`, {
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      })
    } catch (error) {
      console.error('Error in removeFromJournal service:', error)
      throw error
    }
  },

  // permanently delete a note (only allowed if user is admin or owner)
  async deleteNote(noteId) {
    try {
      const authStore = useAuthStore()
      return await apiClient.delete(`/api/notes/${noteId}`, {
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      })
    } catch (error) {
      console.error('Error in deleteNote service:', error)
      throw error
    }
  },

  // fetch a preview image of the first page of a note's PDF
  async getPdfPreview(noteId) {
    try {
      const authStore = useAuthStore()
      return await apiClient.get(`/api/notes/preview/${noteId}`, {
        responseType: 'blob',
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      })
    } catch (error) {
      console.error('Error in getPdfPreview service:', error)
      throw error
    }
  },

  // fetch the full PDF file for a note
  async getFullPdf(noteId) {
    try {
      const authStore = useAuthStore()
      return await apiClient.get(`/api/notes/full/${noteId}`, {
        responseType: 'blob',
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      })
    } catch (error) {
      console.error('Error in getFullPdf service:', error)
      throw error
    }
  },
    
  // update the folder a note is stored in
  async updateNoteFolder(noteId, folderId) {
    try {
      console.log(`notesService: Updating note ${noteId} to folder ${folderId}`)
      const authStore = useAuthStore()
      return await apiClient.put(`/api/notes/${noteId}/folder`, { folderId }, {
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${authStore.token}`,
        },
      })
    } catch (error) {
      console.error('Error in updateNoteFolder service:', error)
      throw error
    }
  },
}
