<template>
  <!-- Page container with top and bottom margin -->
  <div class="container" style="margin-top: 130px; margin-bottom: 30px;">
    <div class="row justify-content-center">
      <div class="col-12 col-xl-10">

        <!-- Header: shows user's name or "Profile" + edit button -->
        <div class="d-flex justify-content-between align-items-center mb-4">
          <h2 class="mb-0">
            {{ user ? capitalize(user.firstName) + "'s Profile" : "Profile" }}
          </h2>
          <button class="btn" style="background-color: #3a3632; color: white;" @click="editUser">
            Edit User 
          </button>
        </div>

        <!-- Profile info row: left = image, right = name -->
        <div class="row g-4">
          <!-- Left box: Profile picture -->
          <div class="col-md-4">
            <div class="card shadow-sm h-100">
              <div class="card-body d-flex flex-column align-items-center justify-content-center">
                <div class="mb-3">
                  <!-- Show profile picture if exists -->
                  <img
                    v-if="user?.profilePicture"
                    :src="pictureAsUrl(user.profilePicture)"
                    alt="Profile Picture"
                    class="rounded-circle"
                    style="width: 120px; height: 120px; object-fit: cover;"
                  />
                  <!-- Otherwise show placeholder -->
                  <div
                    v-else
                    class="d-flex align-items-center justify-content-center rounded-circle bg-light text-muted"
                    style="width: 120px; height: 120px;"
                  >
                    No Image
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Right box: full name and username -->
          <div class="col-md-8">
            <div class="card shadow-sm h-100">
              <div class="card-body d-flex flex-column justify-content-center">
                <h4 class="mb-1">
                  {{ capitalize(user?.firstName) }} {{ capitalize(user?.lastName) }}
                </h4>
                <p class="text-muted mb-0">@{{ user?.userName }}</p>
              </div>
            </div>
          </div>
        </div> <!-- end of profile row -->

        <!-- Info boxes for email, date of birth, and role -->
        <div class="row row-cols-1 row-cols-md-3 g-3 mt-4">
          <!-- Email box -->
          <div class="col">
            <div class="card shadow-sm h-100">
              <div class="card-body pt-4 pb-4">
                <p class="mb-0">
                  <strong>Email:</strong> {{ user?.email }}
                </p>
              </div>
            </div>
          </div>

          <!-- Date of birth box - only shown if value exists -->
          <div class="col" v-if="user?.dateOfBirth">
            <div class="card shadow-sm h-100">
              <div class="card-body pt-4 pb-4">
                <p class="mb-0">
                  <strong>Date of Birth:</strong> {{ formatDate(user.dateOfBirth) }}
                </p>
              </div>
            </div>
          </div>

          <!-- Role box - shows badge or fallback text -->
          <div class="col">
            <div class="card shadow-sm h-100">
              <div class="card-body pt-4 pb-4">
                <p class="mb-0">
                  <strong>Role:</strong>
                  <span class="ms-2">
                    <span v-if="user?.role" class="badge bg-info text-dark">
                      {{ user.role }}
                    </span>
                    <span v-else class="badge bg-secondary">
                      No role
                    </span>
                  </span>
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit profile modal (shown on edit click) -->
    <div v-if="showEditModal" class="modal-backdrop">
      <div class="modal-dialog">
        <div class="modal-content">

          <!-- Modal header -->
          <div class="modal-header">
            <h5>Edit Profile</h5>
            <button type="button" class="btn-close" @click="closeEditModal"></button>
          </div>

          <!-- Modal body -->
          <div class="modal-body">
            <!-- File input to update picture -->
            <div class="mb-3">
              <label class="form-label">Profile Picture</label>
              <input type="file" class="form-control" @change="handleFileUpload" />
              <!-- Remove picture button -->
              <div class="mt-2">
                <button class="btn btn-sm btn-danger" @click="removeProfilePicture">
                  Remove Profile Picture
                </button>
              </div>
            </div>
          </div>

          <!-- Modal buttons -->
          <div class="modal-footer">
            <button class="btn btn-secondary" @click="closeEditModal">Cancel</button>
            <button class="btn" style="background-color: #3a3632; color: white;" @click="updateProfile">Save Changes</button>

          </div>

        </div>
      </div>
    </div>
  </div>
</template>

<script>
import apiClient from "@/services/apiClient";

export default {
  name: "ProfilePage",
  data() {
    return {
      user: null,
      isLoading: true,
      showEditModal: false,
      newProfilePicture: null,
      removePicFlag: false
    };
  },
  async created() {
    try {
      const response = await apiClient.get("/api/user/me");
      this.user = response.data;
    } catch (error) {
      console.error("Error fetching user profile:", error);
    } finally {
      this.isLoading = false;
    }
  },
  methods: {
    formatDate(date) {
      return new Date(date).toLocaleDateString();
    },
    pictureAsUrl(base64String) {
      return "data:image/jpeg;base64," + base64String;
    },
    editUser() {
      this.showEditModal = true;
      this.removePicFlag = false;
    },
    closeEditModal() {
      this.showEditModal = false;
      this.newProfilePicture = null;
    },
    handleFileUpload(event) {
      const file = event.target.files[0];
      if (file) {
        this.newProfilePicture = file;
        this.removePicFlag = false;
      }
    },
    removeProfilePicture() {
      this.newProfilePicture = null;
      this.removePicFlag = true;
      this.user.profilePicture = "";
    },
    async updateProfile() {
      const formData = new FormData();

      if (this.removePicFlag) {
        formData.append("RemoveProfilePicture", "true");
      }

      if (this.newProfilePicture) {
        formData.append("profilePicture", this.newProfilePicture);
      }

      try {
        const response = await apiClient.put("/api/user/me", formData, {
          headers: {
            "Content-Type": "multipart/form-data"
          }
        });
        this.user = response.data;
        this.closeEditModal();
      } catch (error) {
        console.error("Error updating profile:", error);
      }
    },
    capitalize(str) {
      if (!str) return "";
      return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
    }
  }
};
</script>

<style scoped>
.card {
  border-radius: 8px;
}

.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1050;
}

.modal-dialog {
  background: #fff;
  border-radius: 8px;
  width: 90%;
  max-width: 500px;
}

.modal-header,
.modal-footer {
  padding: 1rem;
  border-bottom: 1px solid #dee2e6;
}

.modal-header {
  border-bottom: none;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-body {
  padding: 1rem;
}

.btn-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  line-height: 1;
  cursor: pointer;
}
</style>
