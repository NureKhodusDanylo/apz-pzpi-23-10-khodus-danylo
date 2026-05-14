package com.example.robandroid.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.robandroid.data.model.Friend
import com.example.robandroid.data.model.FriendRequest
import com.example.robandroid.data.model.User
import com.example.robandroid.ui.components.*
import com.example.robandroid.ui.theme.*
import com.example.robandroid.ui.viewmodel.FriendsViewModel

@Composable
fun FriendsScreen(
    friendsViewModel: FriendsViewModel,
    currentUserId: Int,
) {
    val state by friendsViewModel.uiState.collectAsState()

    var searchQuery by remember { mutableStateOf("") }

    LaunchedEffect(Unit) {
        friendsViewModel.loadFriends()
    }

    // ── Snackbar for messages ──
    val snackbarHostState = remember { SnackbarHostState() }
    LaunchedEffect(state.message, state.error) {
        val msg = state.message ?: state.error
        if (msg != null) {
            snackbarHostState.showSnackbar(msg)
            friendsViewModel.clearMessage()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        containerColor = Background,
    ) { padding ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(horizontal = 16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            item {
                Spacer(Modifier.height(16.dp))
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Outlined.People, null, tint = PrimaryContainer, modifier = Modifier.size(28.dp).rotate(5f))
                    Spacer(Modifier.width(8.dp))
                    Text(
                        text = "Friends",
                        style = MaterialTheme.typography.headlineMedium,
                        fontWeight = FontWeight.Bold,
                        color = PrimaryContainer,
                        modifier = Modifier.rotate(-0.5f),
                    )
                }
            }

            // ── Search ──
            item {
                SketchCard(rotationDegrees = 0.3f) {
                    Text(
                        "Find Friends",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = PrimaryContainer,
                    )
                    Spacer(Modifier.height(8.dp))
                    SketchTextField(
                        value = searchQuery,
                        onValueChange = {
                            searchQuery = it
                            if (it.length >= 2) friendsViewModel.searchUsers(it)
                            else friendsViewModel.clearSearchResults()
                        },
                        placeholder = "Search by name...",
                    )

                    if (state.isSearching) {
                        Spacer(Modifier.height(8.dp))
                        CircularProgressIndicator(
                            modifier = Modifier.size(24.dp),
                            color = PrimaryContainer,
                            strokeWidth = 2.dp,
                        )
                    }

                    if (state.searchResults.isNotEmpty()) {
                        Spacer(Modifier.height(8.dp))
                        state.searchResults
                            .filter { it.id != currentUserId }
                            .forEach { user ->
                                SearchResultItem(
                                    user = user,
                                    isFriend = state.friends.any { f -> f.id == user.id },
                                    onAddFriend = { friendsViewModel.sendFriendRequest(user.id) }
                                )
                            }
                    }
                }
            }

            // ── Pending Requests ──
            if (state.pendingRequests.isNotEmpty()) {
                item {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Outlined.MarkEmailUnread, null, tint = PrimaryContainer, modifier = Modifier.size(20.dp))
                        Spacer(Modifier.width(8.dp))
                        Text(
                            "Pending Requests (${state.pendingRequests.size})",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold,
                            color = PrimaryContainer,
                            modifier = Modifier.rotate(0.5f),
                        )
                    }
                }

                items(state.pendingRequests) { request ->
                    PendingRequestCard(
                        request = request,
                        onAccept = { friendsViewModel.acceptRequest(request.id) },
                        onReject = { friendsViewModel.rejectRequest(request.id) },
                    )
                }
            }

            // ── Friends List ──
            item {
                Spacer(Modifier.height(8.dp))
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Outlined.Group, null, tint = PrimaryContainer, modifier = Modifier.size(20.dp))
                    Spacer(Modifier.width(8.dp))
                    Text(
                        "My Friends (${state.friends.size})",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = PrimaryContainer,
                        modifier = Modifier.rotate(-0.3f),
                    )
                }
            }

            if (state.friends.isEmpty() && !state.isLoading) {
                item {
                    SketchCard {
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally,
                            modifier = Modifier.fillMaxWidth(),
                        ) {
                            Icon(Icons.Outlined.PersonSearch, null, tint = PrimaryContainer, modifier = Modifier.size(64.dp))
                            Spacer(Modifier.height(8.dp))
                            Text(
                                "No friends yet",
                                style = MaterialTheme.typography.titleMedium,
                                color = PrimaryContainer,
                            )
                            Text(
                                "Search and add friends above!",
                                style = MaterialTheme.typography.bodySmall,
                                color = OnSurfaceVariant,
                            )
                        }
                    }
                }
            }

            items(state.friends) { friend ->
                FriendCard(
                    friend = friend,
                    onRemove = { friendsViewModel.removeFriend(friend.id) },
                )
            }

            // Loading
            if (state.isLoading) {
                item {
                    Box(
                        modifier = Modifier.fillMaxWidth().padding(32.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        CircularProgressIndicator(color = PrimaryContainer)
                    }
                }
            }

            item { Spacer(Modifier.height(80.dp)) }
        }
    }
}

@Composable
private fun SearchResultItem(
    user: User,
    isFriend: Boolean,
    onAddFriend: () -> Unit,
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        SketchAvatar(imageUrl = user.profilePhotoUrl, size = 40.dp)
        Spacer(Modifier.width(12.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(user.userName, fontWeight = FontWeight.Bold, color = PrimaryContainer)
            Text(user.email, style = MaterialTheme.typography.bodySmall, color = OnSurfaceVariant)
        }
        if (!isFriend) {
            SketchButton(
                onClick = onAddFriend,
                icon = Icons.Outlined.PersonAdd,
            ) {
                Text("Add")
            }
        } else {
            SketchBadge(text = "FRIENDS")
        }
    }
}

@Composable
private fun PendingRequestCard(
    request: FriendRequest,
    onAccept: () -> Unit,
    onReject: () -> Unit,
) {
    SketchCard(modifier = Modifier.fillMaxWidth(), showShadow = false) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            SketchAvatar(imageUrl = request.senderPhotoUrl, size = 44.dp)
            Spacer(Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(request.senderName, fontWeight = FontWeight.Bold, color = PrimaryContainer)
                Text(request.senderEmail, style = MaterialTheme.typography.bodySmall, color = OnSurfaceVariant)
            }
            Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                SketchButton(onClick = onAccept, variant = SketchButtonVariant.Primary) {
                    Icon(Icons.Outlined.Check, null, tint = OnPrimaryContainer, modifier = Modifier.size(18.dp))
                }
                SketchButton(onClick = onReject, variant = SketchButtonVariant.Error) {
                    Icon(Icons.Outlined.Close, null, tint = OnPrimaryContainer, modifier = Modifier.size(18.dp))
                }
            }
        }
    }
}

@Composable
private fun FriendCard(
    friend: Friend,
    onRemove: () -> Unit,
) {
    SketchCard(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            SketchAvatar(imageUrl = friend.profilePhotoUrl, size = 48.dp)
            Spacer(Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(friend.userName, fontWeight = FontWeight.Bold, color = PrimaryContainer)
                Text(friend.email, style = MaterialTheme.typography.bodySmall, color = OnSurfaceVariant)
                if (!friend.phoneNumber.isNullOrBlank()) {
                    Text(friend.phoneNumber, style = MaterialTheme.typography.bodySmall, color = OnSurfaceVariant)
                }
            }
            IconButton(onClick = onRemove) {
                Icon(Icons.Outlined.PersonRemove, "Remove", tint = Error)
            }
        }
    }
}
