package com.example.robandroid.ui.viewmodel

import android.content.Context
import androidx.credentials.CredentialManager
import androidx.credentials.CustomCredential
import androidx.credentials.GetCredentialRequest
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.robandroid.data.local.TokenManager
import com.example.robandroid.data.model.*
import com.example.robandroid.data.remote.RetrofitClient
import com.google.android.libraries.identity.googleid.GetGoogleIdOption
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential
import com.google.android.libraries.identity.googleid.GoogleIdTokenParsingException
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.asRequestBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.File

data class AuthUiState(
    val user: User? = null,
    val token: String? = null,
    val isAuthenticated: Boolean = false,
    val isLoading: Boolean = false,
    val error: String? = null,
    val needsAdditionalInfo: Boolean = false,
    val googleId: String? = null,
    val prefillEmail: String? = null,
    val prefillUserName: String? = null,
)

class AuthViewModel(private val tokenManager: TokenManager) : ViewModel() {

    companion object {
        // IMPORTANT: For Google Sign-In on Android (Credential Manager), 
        // you MUST use the "Web Application" Client ID as the serverClientId.
        // Using an "Android" Client ID here will cause BAD_AUTHENTICATION.
        const val WEB_CLIENT_ID = "277199733405-bn8d717l6h3s39qgu5g9fe15lrf5v18n.apps.googleusercontent.com"
        
        // This is your mobile-specific key, ensure it's also a "Web Application" type 
        // if you want to use it as a separate serverClientId.
        const val MOBILE_WEB_CLIENT_ID = "277199733405-6mbgbe61g38ktf6lsqrhckgv2jbdu9m5.apps.googleusercontent.com"
    }

    private val _uiState = MutableStateFlow(AuthUiState())
    val uiState: StateFlow<AuthUiState> = _uiState.asStateFlow()

    private val api = RetrofitClient.apiService

    init {
        // Restore persisted auth state
        viewModelScope.launch {
            tokenManager.token.combine(tokenManager.user) { token, user ->
                if (!token.isNullOrEmpty() && user != null) {
                    _uiState.value = AuthUiState(
                        user = user,
                        token = token,
                        isAuthenticated = true,
                    )
                }
            }.collect()
        }
    }

    fun login(email: String, password: String) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                // Trim whitespace from inputs to avoid trailing-space bugs
                val response = api.login(LoginRequest(email = email.trim(), password = password.trim()))
                if (response.isSuccessful) {
                    val body = response.body()!!
                    handleAuthResponse(body)
                } else {
                    val errorBody = response.errorBody()?.string()
                    val msg = errorBody ?: "Login failed (${response.code()})"
                    _uiState.update { it.copy(isLoading = false, error = msg) }
                }
            } catch (e: java.net.SocketTimeoutException) {
                _uiState.update { it.copy(isLoading = false, error = "Server not responding. Open the API URL in a browser first to bypass tunnel protection.") }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = e.message ?: "Network error") }
            }
        }
    }

    // ════════ Google Sign-In via Credential Manager ════════
    fun signInWithGoogle(context: Context) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val credentialManager = CredentialManager.create(context)

                val googleIdOption = GetGoogleIdOption.Builder()
                    .setFilterByAuthorizedAccounts(false)
                    .setServerClientId(MOBILE_WEB_CLIENT_ID)
                    .setAutoSelectEnabled(true)
                    .build()

                val request = GetCredentialRequest.Builder()
                    .addCredentialOption(googleIdOption)
                    .build()

                val result = credentialManager.getCredential(
                    request = request,
                    context = context,
                )

                val credential = result.credential
                if (credential is CustomCredential &&
                    credential.type == GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL
                ) {
                    try {
                        val googleIdTokenCredential = GoogleIdTokenCredential.createFrom(credential.data)
                        val idToken = googleIdTokenCredential.idToken

                        // Send Google JWT to backend (same as web does)
                        val response = api.login(LoginRequest(googleJwtToken = idToken))
                        if (response.isSuccessful) {
                            handleAuthResponse(response.body()!!)
                        } else {
                            _uiState.update { it.copy(isLoading = false, error = "Google login failed: ${response.code()}") }
                        }
                    } catch (e: GoogleIdTokenParsingException) {
                        _uiState.update { it.copy(isLoading = false, error = "Invalid Google token") }
                    }
                } else {
                    _uiState.update { it.copy(isLoading = false, error = "Unexpected credential type") }
                }
            } catch (e: androidx.credentials.exceptions.NoCredentialException) {
                _uiState.update { it.copy(isLoading = false, error = "No Google account found. Add your Google account to this device first.") }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = "Google Sign-In: ${e.message}") }
            }
        }
    }

    fun completeGoogleRegistration(
        googleId: String, email: String, userName: String,
        phoneNumber: String, latitude: Double, longitude: Double, address: String?
    ) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val request = CompleteGoogleRegRequest(
                    googleId = googleId, email = email, userName = userName,
                    phoneNumber = phoneNumber, latitude = latitude, longitude = longitude,
                    address = address,
                )
                val response = api.completeGoogleRegistration(request)
                if (response.isSuccessful) {
                    handleAuthResponse(response.body()!!)
                } else {
                    _uiState.update { it.copy(isLoading = false, error = "Registration failed: ${response.code()}") }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = e.message ?: "Network error") }
            }
        }
    }

    fun register(
        userName: String, email: String, password: String,
        phoneNumber: String, latitude: Double, longitude: Double, address: String?
    ) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val request = RegisterRequest(
                    userName = userName.trim(), email = email.trim(), password = password,
                    phoneNumber = phoneNumber.trim(), latitude = latitude, longitude = longitude,
                    address = address?.trim()
                )
                val response = api.register(request)
                if (response.isSuccessful) {
                    handleAuthResponse(response.body()!!)
                } else {
                    val errorBody = response.errorBody()?.string()
                    _uiState.update { it.copy(isLoading = false, error = errorBody ?: "Registration failed (${response.code()})") }
                }
            } catch (e: java.net.SocketTimeoutException) {
                _uiState.update { it.copy(isLoading = false, error = "Server not responding. Open the API URL in a browser first.") }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = e.message ?: "Network error") }
            }
        }
    }

    private suspend fun handleAuthResponse(body: AuthResponse) {
        when (body.status) {
            "NeedsAdditionalInfo" -> {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        needsAdditionalInfo = true,
                        googleId = body.googleId,
                        prefillEmail = body.email,
                        prefillUserName = body.userName,
                    )
                }
            }
            "Success" -> {
                val token = body.token ?: return
                // Fetch full user profile
                try {
                    tokenManager.saveAuth(token, User()) // temporary save for interceptor
                    val profileResp = api.getCurrentUser()
                    if (profileResp.isSuccessful) {
                        val user = profileResp.body()!!
                        tokenManager.saveAuth(token, user)
                        _uiState.update {
                            AuthUiState(
                                user = user,
                                token = token,
                                isAuthenticated = true,
                            )
                        }
                    }
                } catch (e: Exception) {
                    _uiState.update { it.copy(isLoading = false, error = "Failed to load profile") }
                }
            }
            else -> {
                _uiState.update { it.copy(isLoading = false, error = body.message) }
            }
        }
    }

    fun updateProfile(userName: String?, phoneNumber: String?, password: String?, photoFile: File?) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val userNameBody = userName?.toRequestBody("text/plain".toMediaTypeOrNull())
                val phoneBody = phoneNumber?.toRequestBody("text/plain".toMediaTypeOrNull())
                val passwordBody = if (!password.isNullOrEmpty()) password.toRequestBody("text/plain".toMediaTypeOrNull()) else null
                val photoPart = photoFile?.let {
                    val requestFile = it.asRequestBody("image/*".toMediaTypeOrNull())
                    MultipartBody.Part.createFormData("profilePhoto", it.name, requestFile)
                }

                val response = api.updateProfile(userNameBody, phoneBody, passwordBody, photoPart)
                if (response.isSuccessful) {
                    val updatedUser = response.body()?.profile ?: _uiState.value.user
                    if (updatedUser != null) {
                        tokenManager.updateUser(updatedUser)
                        _uiState.update { it.copy(user = updatedUser, isLoading = false) }
                    }
                } else {
                    _uiState.update { it.copy(isLoading = false, error = "Update failed") }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = e.message) }
            }
        }
    }

    fun clearNeedsAdditionalInfo() {
        _uiState.update { it.copy(needsAdditionalInfo = false) }
    }

    fun refreshUser() {
        viewModelScope.launch {
            try {
                val response = api.getCurrentUser()
                if (response.isSuccessful) {
                    val user = response.body()!!
                    tokenManager.updateUser(user)
                    _uiState.update { it.copy(user = user) }
                }
            } catch (_: Exception) {}
        }
    }

    fun logout() {
        viewModelScope.launch {
            tokenManager.clearAuth()
            _uiState.value = AuthUiState()
        }
    }

    fun clearError() {
        _uiState.update { it.copy(error = null) }
    }
}
