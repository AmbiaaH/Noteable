
// src/utils/adminUtils.js
import { useAuthStore } from '@/store/auth';
import apiClient from '@/services/apiClient';




/**
 * Global admin utility functions for performing admin actions
 * throughout the application.
 */
export const adminUtils = {
  /**
   * Delete any item using the API with admin role
   * 
   * @param {string} endpoint - The API endpoint 
   * @param {number|string} itemId - The ID of the item to delete
   * @param {Function} onSuccess - Callback function after successful deletion
   * @param {string} confirmMessage - Optional custom confirmation message
   * @returns {Promise<boolean>} Success result
   */
  async deleteItem(endpoint, itemId, onSuccess, confirmMessage) {
    const authStore = useAuthStore();
    
    // Verify admin status
    if (!authStore.isAdmin) {
      console.error('Unauthorized: Admin access required');
      return false;
    }
    
    // Confirm deletion
    const message = confirmMessage || 'WARNING: This will permanently delete this item. This action cannot be undone. Continue?';
    if (!confirm(message)) {
      return false;
    }
    
    try {
      // Make API call
      await apiClient.delete(`${endpoint}/${itemId}`, {
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      });
      
      // Call success callback 
      if (onSuccess && typeof onSuccess === 'function') {
        onSuccess(itemId);
      }
      
      // Show success message
      alert('Item has been permanently deleted');
      return true;
    } catch (error) {
      console.error('Error in adminDelete:', error);
      alert('Failed to delete: ' + (error.response?.data?.message || error.message));
      return false;
    }
  }
};

/**
 * Install the admin utilities as a plugin
 * 
 * @param {Object} app - Vue application instance
 */
export function installAdminUtils(app) {
  app.config.globalProperties.$adminUtils = adminUtils;
}