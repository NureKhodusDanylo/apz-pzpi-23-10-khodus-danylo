package com.example.robandroid.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.robandroid.data.model.*
import com.example.robandroid.data.remote.RetrofitClient
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class FriendsUiState(
    val friends: List<Friend> = emptyList(),
    val pendingRequests: List<FriendRequest> = emptyList(),
    val searchResults: List<User> = emptyList(),
    val isLoading: Boolean = false,
    val isSearching: Boolean = false,
    val error: String? = null,
    val message: String? = null,
)

class FriendsViewModel : ViewModel() {

    private val _uiState = MutableStateFlow(FriendsUiState())
    val uiState: StateFlow<FriendsUiState> = _uiState.asStateFlow()

    private val api = RetrofitClient.apiService

    fun loadFriends() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val friendsResp = api.getFriends()
                val requestsResp = api.getPendingRequests()
                _uiState.update {
                    it.copy(
                        friends = friendsResp.body() ?: emptyList(),
                        pendingRequests = requestsResp.body() ?: emptyList(),
                        isLoading = false,
                    )
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = e.message) }
            }
        }
    }

    fun searchUsers(query: String) {
        if (query.isBlank()) {
            _uiState.update { it.copy(searchResults = emptyList()) }
            return
        }
        viewModelScope.launch {
            _uiState.update { it.copy(isSearching = true) }
            try {
                val response = api.searchUsers(query)
                if (response.isSuccessful) {
                    _uiState.update { it.copy(searchResults = response.body() ?: emptyList(), isSearching = false) }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isSearching = false, error = e.message) }
            }
        }
    }

    fun sendFriendRequest(userId: Int) {
        viewModelScope.launch {
            try {
                api.sendFriendRequest(SendFriendRequest(targetUserId = userId))
                _uiState.update { it.copy(message = "Friend request sent!") }
                loadFriends()
            } catch (e: Exception) {
                _uiState.update { it.copy(error = e.message) }
            }
        }
    }

    fun acceptRequest(requestId: Int) {
        viewModelScope.launch {
            try {
                api.acceptFriendRequest(requestId)
                _uiState.update { it.copy(message = "Request accepted!") }
                loadFriends()
            } catch (e: Exception) {
                _uiState.update { it.copy(error = e.message) }
            }
        }
    }

    fun rejectRequest(requestId: Int) {
        viewModelScope.launch {
            try {
                api.rejectFriendRequest(requestId)
                loadFriends()
            } catch (e: Exception) {
                _uiState.update { it.copy(error = e.message) }
            }
        }
    }

    fun removeFriend(friendId: Int) {
        viewModelScope.launch {
            try {
                api.removeFriend(friendId)
                _uiState.update { it.copy(message = "Friend removed") }
                loadFriends()
            } catch (e: Exception) {
                _uiState.update { it.copy(error = e.message) }
            }
        }
    }

    fun clearSearchResults() {
        _uiState.update { it.copy(searchResults = emptyList()) }
    }

    fun clearMessage() {
        _uiState.update { it.copy(message = null, error = null) }
    }
}
