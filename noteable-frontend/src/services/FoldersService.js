// src/services/FoldersService.js
import axios from 'axios'
import { useAuthStore } from '@/store/auth'



// set the base endpoint for all folder-related API calls
const API_URL = '/api/folders'

// create a new axios instance just for folder requests
const foldersApi = axios.create({
  timeout: 10000, // set request timeout to 10 seconds
  headers: {
    'Content-Type': 'application/json'
  }
})

// attach token to every request using the auth store
foldersApi.interceptors.request.use(config => {
  const authStore = useAuthStore()
  if (authStore.token) {
    config.headers.Authorization = `Bearer ${authStore.token}`
  }
  return config
})

// handle errors globally for folder requests
foldersApi.interceptors.response.use(
  response => response,
  error => {
    const status = error.response?.status
    const message = error.message
    const responseData = error.response?.data

    console.error(`Folders API Error (${status}): ${message}`)
    if (responseData) {
      console.error('Response details:', responseData)
    }

    return Promise.reject(error)
  }
)

const foldersService = {
  // test if the backend folders route is reachable
  testConnection() {
    console.log('Testing folders API connection')
    return foldersApi.get(`${API_URL}/test`)
  },

  // get all folders for the current user
  getAllFolders() {
    console.log('Getting all folders')
    return foldersApi.get(API_URL)
  },

  // get a specific folder by id
  getFolder(id) {
    console.log(`Getting folder ${id}`)
    return foldersApi.get(`${API_URL}/${id}`)
  },

  // create a new folder with the given name
  createFolder(name) {
    console.log('Creating folder with name:', name)

    // basic input check before sending request
    if (!name || typeof name !== 'string' || name.trim() === '') {
      return Promise.reject(new Error('Folder name cannot be empty'))
    }

    return foldersApi.post(API_URL, { name: name.trim() })
  },

  // update an existing folder's name
  updateFolder(id, name) {
    console.log(`Updating folder ${id} with name:`, name)

    // check id and name before sending request
    if (!id || isNaN(parseInt(id))) {
      return Promise.reject(new Error('Invalid folder ID'))
    }

    if (!name || typeof name !== 'string' || name.trim() === '') {
      return Promise.reject(new Error('Folder name cannot be empty'))
    }

    return foldersApi.put(`${API_URL}/${id}`, { name: name.trim() })
  },

  // delete a folder by id
  deleteFolder(id) {
    console.log(`Deleting folder ${id}`)

    // check if the id is valid
    if (!id || isNaN(parseInt(id))) {
      return Promise.reject(new Error('Invalid folder ID'))
    }

    return foldersApi.delete(`${API_URL}/${id}`)
  }
}

export default foldersService
