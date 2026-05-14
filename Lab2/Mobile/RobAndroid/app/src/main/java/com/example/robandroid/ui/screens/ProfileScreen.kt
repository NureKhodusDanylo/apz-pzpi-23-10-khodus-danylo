package com.example.robandroid.ui.screens

import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.robandroid.data.remote.RetrofitClient
import com.example.robandroid.ui.components.*
import com.example.robandroid.ui.theme.*
import com.example.robandroid.ui.viewmodel.AuthViewModel
import java.io.File

@Composable
fun ProfileScreen(
    authViewModel: AuthViewModel,
    onLogout: () -> Unit,
) {
    val state by authViewModel.uiState.collectAsState()
    val user = state.user ?: return
    val context = LocalContext.current

    LaunchedEffect(Unit) {
        authViewModel.refreshUser()
    }

    var isEditing by remember { mutableStateOf(false) }
    var editName by remember(user) { mutableStateOf(user.userName) }
    var editPhone by remember(user) { mutableStateOf(user.phoneNumber) }
    var editPassword by remember { mutableStateOf("") }
    var selectedPhotoUri by remember { mutableStateOf<Uri?>(null) }

    val photoLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.GetContent()
    ) { uri: Uri? ->
        selectedPhotoUri = uri
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Background)
            .statusBarsPadding()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
    ) {
        Spacer(Modifier.height(16.dp))

        Row(verticalAlignment = Alignment.CenterVertically) {
            androidx.compose.material3.Icon(
                imageVector = Icons.Outlined.Person,
                contentDescription = null,
                tint = PrimaryContainer,
                modifier = androidx.compose.ui.Modifier.size(28.dp).rotate(-5f)
            )
            Spacer(Modifier.width(8.dp))
            Text(
                text = "Profile",
                style = MaterialTheme.typography.headlineMedium,
                fontWeight = FontWeight.Bold,
                color = PrimaryContainer,
                modifier = Modifier.rotate(-0.5f),
            )
        }

        Spacer(Modifier.height(16.dp))

        // ── Avatar + Name Card ──
        SketchCard(rotationDegrees = -0.3f) {
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier.fillMaxWidth(),
            ) {
                SketchAvatar(
                    imageUrl = user.profilePhotoUrl,
                    size = 100.dp,
                    rotationDegrees = 2f,
                )

                Spacer(Modifier.height(12.dp))

                Text(
                    text = user.userName,
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )

                SketchBadge(
                    text = user.role.uppercase(),
                    rotationDegrees = 4f,
                )

                if (isEditing) {
                    Spacer(Modifier.height(12.dp))
                    SketchButton(
                        onClick = { photoLauncher.launch("image/*") },
                        variant = SketchButtonVariant.Secondary,
                        icon = Icons.Outlined.PhotoCamera,
                    ) {
                        Text("Change Photo")
                    }
                    if (selectedPhotoUri != null) {
                        Spacer(Modifier.height(4.dp))
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            androidx.compose.material3.Icon(
                                imageVector = Icons.Outlined.Attachment,
                                contentDescription = null,
                                tint = PrimaryContainer,
                                modifier = androidx.compose.ui.Modifier.size(14.dp)
                            )
                            Spacer(Modifier.width(4.dp))
                            Text(
                                "Photo selected",
                                style = MaterialTheme.typography.bodySmall,
                                color = PrimaryContainer,
                            )
                        }
                    }
                }
            }
        }

        Spacer(Modifier.height(16.dp))

        // ── Info / Edit Card ──
        SketchCard(rotationDegrees = 0.3f) {
            if (isEditing) {
                // ── Edit Mode ──
                Text(
                    "Edit Profile",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )
                Spacer(Modifier.height(12.dp))

                SketchTextField(
                    value = editName,
                    onValueChange = { editName = it },
                    label = "USERNAME",
                )
                Spacer(Modifier.height(12.dp))

                SketchTextField(
                    value = editPhone,
                    onValueChange = { editPhone = it },
                    label = "PHONE NUMBER",
                )
                Spacer(Modifier.height(12.dp))

                SketchTextField(
                    value = editPassword,
                    onValueChange = { editPassword = it },
                    label = "NEW PASSWORD",
                    placeholder = "Leave empty to keep current",
                    isPassword = true,
                )

                Spacer(Modifier.height(16.dp))

                if (state.error != null) {
                    Text(state.error!!, color = Error, style = MaterialTheme.typography.bodySmall)
                    Spacer(Modifier.height(8.dp))
                }

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    SketchButton(
                        onClick = {
                            // Convert Uri to temp file
                            var photoFile: File? = null
                            selectedPhotoUri?.let { uri ->
                                try {
                                    val inputStream = context.contentResolver.openInputStream(uri)
                                    val temp = File.createTempFile("photo", ".jpg", context.cacheDir)
                                    inputStream?.use { input -> temp.outputStream().use { output -> input.copyTo(output) } }
                                    photoFile = temp
                                } catch (_: Exception) {}
                            }

                            authViewModel.updateProfile(
                                userName = editName.takeIf { it != user.userName },
                                phoneNumber = editPhone.takeIf { it != user.phoneNumber },
                                password = editPassword.takeIf { it.isNotEmpty() },
                                photoFile = photoFile,
                            )
                            isEditing = false
                        },
                        modifier = Modifier.weight(1f),
                        isLoading = state.isLoading,
                        icon = Icons.Outlined.Check,
                    ) {
                        Text("Save")
                    }

                    SketchButton(
                        onClick = {
                            isEditing = false
                            editName = user.userName
                            editPhone = user.phoneNumber
                            editPassword = ""
                            selectedPhotoUri = null
                        },
                        modifier = Modifier.weight(1f),
                        variant = SketchButtonVariant.Secondary,
                        icon = Icons.Outlined.Close,
                    ) {
                        Text("Cancel")
                    }
                }
            } else {
                // ── View Mode ──
                Text(
                    "Account Info",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )
                Spacer(Modifier.height(12.dp))

                InfoRow(icon = Icons.Outlined.Email, label = "Email", value = user.email)
                InfoRow(icon = Icons.Outlined.Phone, label = "Phone", value = user.phoneNumber)
                InfoRow(icon = Icons.Outlined.LocationOn, label = "Address", value = user.address)

                Spacer(Modifier.height(16.dp))

                SketchButton(
                    onClick = { isEditing = true },
                    modifier = Modifier.fillMaxWidth(),
                    variant = SketchButtonVariant.Secondary,
                    icon = Icons.Outlined.Edit,
                ) {
                    Text("Edit Profile")
                }
            }
        }

        Spacer(Modifier.height(16.dp))

        // ── Logout ──
        SketchButton(
            onClick = {
                authViewModel.logout()
                onLogout()
            },
            modifier = Modifier.fillMaxWidth(),
            variant = SketchButtonVariant.Error,
            icon = Icons.Outlined.Logout,
        ) {
            Text("Logout", fontWeight = FontWeight.Bold)
        }

        Spacer(Modifier.height(80.dp))
    }
}

@Composable
private fun InfoRow(icon: androidx.compose.ui.graphics.vector.ImageVector, label: String, value: String) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Icon(icon, label, tint = PrimaryContainer, modifier = Modifier.size(20.dp))
        Spacer(Modifier.width(12.dp))
        Column {
            Text(label, style = MaterialTheme.typography.labelSmall, color = OnSurfaceVariant)
            Text(value, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold, color = PrimaryContainer)
        }
    }
}
