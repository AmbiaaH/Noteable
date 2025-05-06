// src/directives/admin.js
import { useAuthStore } from '@/store/auth';

export const admin = {
  mounted(el, binding) {
    const authStore = useAuthStore()
    const isAdmin = authStore.isAdmin

    // hide element if user is not admin
    if (!isAdmin) {
      el.style.display = 'none'
      return
    }

    // apply custom styles if passed
    if (binding.value && typeof binding.value === 'object') {
      Object.entries(binding.value).forEach(([prop, value]) => {
        el.style[prop] = value
      })
    }

    // check for delete action
    if (binding.arg === 'delete') {
      // add admin delete style class
      el.classList.add('admin-delete-btn')

      // add a title if not already present
      if (!el.hasAttribute('title')) {
        el.setAttribute('title', 'Admin: Delete this item')
      }
    }
  },

  // run again if the component updates
  updated(el, binding) {
    const authStore = useAuthStore()
    const isAdmin = authStore.isAdmin

    // show or hide based on admin status
    el.style.display = isAdmin ? '' : 'none'
  }
}

// helper to register the directive in a Vue app
export function registerAdminDirective(app) {
  app.directive('admin', admin)
}
