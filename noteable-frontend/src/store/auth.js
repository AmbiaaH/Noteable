// src/store/auth.js
import { defineStore } from 'pinia'
import apiClient from '@/services/apiClient';


// Pinia store to manage user authentication
export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null, // holds the current users info
    token: null // holds the JWT token
  }),

  getters: {
    // check if the user is logged in
    isAuthenticated: (state) => !!state.token,

    // check if user has admin role
    isAdmin: (state) => {
      return state.user?.role?.toLowerCase() === 'admin' ||
             state.user?.role === 'Admin' ||
             (Array.isArray(state.user?.roles) &&
              state.user?.roles.some(r =>
                r.toLowerCase() === 'admin' || r === 'Admin'
              ))
    },

    // check if user has mentor role
    isMentor: (state) => {
      return state.user?.role?.toLowerCase() === 'mentor' ||
             state.user?.role === 'Mentor' ||
             (Array.isArray(state.user?.roles) &&
              state.user?.roles.some(r =>
                r.toLowerCase() === 'mentor' || r === 'Mentor'
              ))
    },

    // check if user has student role
    isStudent: (state) => {
      return state.user?.role?.toLowerCase() === 'student' ||
             state.user?.role === 'Student' ||
             (Array.isArray(state.user?.roles) &&
              state.user?.roles.some(r =>
                r.toLowerCase() === 'student' || r === 'Student'
              ))
    },

    // try to get a friendly first name from different sources
    firstName: (state) => {
      if (!state.user) return 'there'

      if (state.user.firstName) return state.user.firstName
      if (state.user.firstname) return state.user.firstname
      if (state.user.first_name) return state.user.first_name

      if (state.user.name) return state.user.name.split(' ')[0]
      if (state.user.fullName) return state.user.fullName.split(' ')[0]
      if (state.user.full_name) return state.user.full_name.split(' ')[0]

      if (state.user.username) {
        const match = state.user.username.match(/^([a-zA-Z]+)/)
        if (match) return match[1].charAt(0).toUpperCase() + match[1].slice(1).toLowerCase()
        return state.user.username
      }

      if (state.user.email) {
        const emailName = state.user.email.split('@')[0]
        const match = emailName.match(/^([a-zA-Z]+)/)
        if (match) return match[1].charAt(0).toUpperCase() + match[1].slice(1).toLowerCase()
      }

      return 'there'
    }
  },

  actions: {
    // set the token and user data from localStorage
    initializeAuth() {
      const token = localStorage.getItem('token')
      const user = localStorage.getItem('user')

      if (token && token !== 'undefined') {
        this.token = token
      } else {
        localStorage.removeItem('token')
      }

      if (user && user !== 'undefined') {
        try {
          this.user = JSON.parse(user)
        } catch (err) {
          console.error('Failed to parse user from localStorage:', err)
          localStorage.removeItem('user')
        }
      } else {
        localStorage.removeItem('user')
      }
    },

    // log in with given credentials
    async login(credentials) {
      console.log('Attempting login with credentials:', credentials)
      try {
        const response = await apiClient.post('/auth/login', credentials)

        this.token = response.data.token
        this.user = response.data.user

        localStorage.setItem('token', this.token)
        localStorage.setItem('user', JSON.stringify(this.user))

        await this.refreshUserProfile()

        return { success: true }
      } catch (error) {
        console.error('Login error:', error)
        return {
          success: false,
          message: error.response?.data || 'Login failed'
        }
      }
    },

    // get the latest user info from the server
    async refreshUserProfile() {
      if (!this.token) return

      try {
        const response = await apiClient.get('/auth/currentuser')
        this.user = response.data
        localStorage.setItem('user', JSON.stringify(this.user))
      } catch (error) {
        console.error('Failed to refresh user profile:', error)
      }
    },

    // log out the user and clear all data
    logout() {
      this.token = null
      this.user = null
      localStorage.removeItem('token')
      localStorage.removeItem('user')
    },

    // check if the user has a certain role
    hasRole(role) {
      const normalizedRole = role.toLowerCase()
      return this.user?.role?.toLowerCase() === normalizedRole ||
             this.user?.role === role ||
             (Array.isArray(this.user?.roles) &&
              this.user?.roles.some(r =>
                r.toLowerCase() === normalizedRole || r === role
              ))
    },

    // assign a role to another user (admin only)
    async assignRole(username, role) {
      if (!this.isAdmin) {
        throw new Error('You do not have permission to assign roles')
      }

      return await apiClient.post('/auth/assignrole', {
        username,
        role
      })
    }
  }
})
