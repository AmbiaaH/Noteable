// services/api.js
import axios from 'axios'

// create an axios instance with default settings
const apiClient = axios.create({
  baseURL: 'http://localhost:5172', // base URL of the backend API
  headers: {
    'Content-Type': 'application/json'
  },
  withCredentials: true // include cookies in requests if needed
})

// attach token to every request if found in localStorage
apiClient.interceptors.request.use(
  config => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  error => {
    return Promise.reject(error)
  }
)

// handle 401 unauthorized responses globally
apiClient.interceptors.response.use(
  response => response,
  error => {
    if (error.response && error.response.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      // optional: redirect to login page
    }
    return Promise.reject(error)
  }
)

export default apiClient
