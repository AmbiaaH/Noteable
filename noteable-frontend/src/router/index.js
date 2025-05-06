import { createRouter, createWebHistory } from 'vue-router'
import RegisterPage from '@/views/RegisterPage.vue'
import LoginPage from '@/views/LoginPage.vue'
import ForgotPass from '@/views/ForgotPass.vue'
import ResetPass from '@/views/ResetPass.vue'
import ConfirmEmailPage from '@/views/ConfirmEmailPage.vue'
import HomePage from '@/views/HomePage.vue'
import NoteJournalPage from '@/views/NoteJournalPage.vue'
import ProfilePage from '@/views/ProfilePage.vue'
import ForumPage from '@/views/ForumPage.vue'
import MentorsPage from '@/views/MentorsPage.vue'
import { useAuthStore } from '@/store/auth'

// define all routes used in the app
const routes = [
  { path: '/', component: LoginPage },
  { path: '/login', component: LoginPage },
  { path: '/register', component: RegisterPage },
  { path: '/forgotpassword', component: ForgotPass },
  { path: '/resetpassword', component: ResetPass },
  { path: '/confirmemail', component: ConfirmEmailPage },
  {
    path: '/journal',
    component: NoteJournalPage,
    meta: { requiresAuth: true }
  },
  {
    path: '/home',
    component: HomePage,
    meta: { requiresAuth: true }
  },
  {
    path: '/profile',
    component: ProfilePage,
    meta: { requiresAuth: true }
  },
  {
    path: '/forum',
    component: ForumPage,
    meta: { requiresAuth: true }
  },
  {
    path: '/mentors',
    component: MentorsPage,
    meta: { requiresAuth: true }
  },
  {
    path: '/reset-password',
    name: 'ResetPass',
    component: () => import('@/views/ResetPass.vue')
  }
]

// create Vue Router instance using history mode
const router = createRouter({
  history: createWebHistory(),
  routes
})

// runs before every route change
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()

  // load auth status from localStorage
  authStore.initializeAuth()

  console.log('Route guard check: navigating to', to.path, 'isAuthenticated?', authStore.isAuthenticated)

  // if route needs login and user is not logged in, redirect to login
  if (to.matched.some(record => record.meta.requiresAuth) && !authStore.isAuthenticated) {
    console.warn(`Unauthorized access attempt to ${to.path}. Redirecting to /login.`)
    return next('/login')
  }

  next()
})

export default router
