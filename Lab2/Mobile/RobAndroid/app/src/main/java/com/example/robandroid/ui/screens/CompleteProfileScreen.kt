package com.example.robandroid.ui.screens

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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.robandroid.ui.components.*
import com.example.robandroid.ui.theme.*
import com.example.robandroid.ui.viewmodel.AuthViewModel

@Composable
fun CompleteProfileScreen(
    authViewModel: AuthViewModel,
    onSuccess: () -> Unit,
) {
    val state by authViewModel.uiState.collectAsState()

    var userName by remember { mutableStateOf(state.prefillUserName ?: "") }
    var email by remember { mutableStateOf(state.prefillEmail ?: "") }
    var phoneNumber by remember { mutableStateOf("") }
    var address by remember { mutableStateOf("") }
    var localError by remember { mutableStateOf<String?>(null) }

    // Navigate on successful auth
    LaunchedEffect(state.isAuthenticated) {
        if (state.isAuthenticated) onSuccess()
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Background)
            .statusBarsPadding()
            .verticalScroll(rememberScrollState())
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Spacer(Modifier.height(40.dp))

        Icon(
            imageVector = Icons.Outlined.AssignmentInd,
            contentDescription = null,
            modifier = Modifier.size(80.dp),
            tint = PrimaryContainer
        )
        Spacer(Modifier.height(8.dp))
        Text(
            text = "Complete Profile",
            style = MaterialTheme.typography.headlineMedium,
            color = PrimaryContainer,
            fontWeight = FontWeight.Bold
        )
        Text(
            text = "Tell us a bit more to get started",
            style = MaterialTheme.typography.bodyMedium,
            color = OnSurfaceVariant
        )

        Spacer(Modifier.height(32.dp))

        SketchCard(
            modifier = Modifier.fillMaxWidth(),
            rotationDegrees = -0.5f,
        ) {
            Text(
                "Personal Details",
                style = MaterialTheme.typography.titleMedium,
                color = PrimaryContainer,
                fontWeight = FontWeight.Bold,
            )

            Spacer(Modifier.height(16.dp))

            SketchTextField(
                value = userName,
                onValueChange = { userName = it },
                label = "USERNAME",
                placeholder = "How should we call you?"
            )
            
            Spacer(Modifier.height(12.dp))

            SketchTextField(
                value = email,
                onValueChange = { /* Email usually locked for Google auth */ },
                label = "EMAIL",
                placeholder = "your@email.com",
                enabled = false
            )

            Spacer(Modifier.height(12.dp))

            SketchTextField(
                value = phoneNumber,
                onValueChange = { phoneNumber = it },
                label = "PHONE NUMBER",
                placeholder = "+380XXXXXXXXX"
            )

            Spacer(Modifier.height(12.dp))

            SketchTextField(
                value = address,
                onValueChange = { address = it },
                label = "DELIVERY ADDRESS",
                placeholder = "City, Street, House"
            )

            Spacer(Modifier.height(24.dp))

            // Error
            val errorText = localError ?: state.error
            if (errorText != null) {
                Text(
                    text = errorText,
                    style = MaterialTheme.typography.bodySmall,
                    color = Error,
                    modifier = Modifier.padding(bottom = 8.dp),
                )
            }

            SketchButton(
                onClick = {
                    if (userName.isBlank() || phoneNumber.isBlank()) {
                        localError = "Please fill in all required fields"
                        return@SketchButton
                    }
                    authViewModel.completeGoogleRegistration(
                        googleId = state.googleId ?: "",
                        email = email,
                        userName = userName,
                        phoneNumber = phoneNumber,
                        latitude = 50.4501, // Default Kyiv for now
                        longitude = 30.5234,
                        address = address.ifBlank { null }
                    )
                },
                modifier = Modifier.fillMaxWidth(),
                isLoading = state.isLoading,
                enabled = userName.isNotBlank() && phoneNumber.isNotBlank()
            ) {
                Text("Finish Registration", fontWeight = FontWeight.Bold)
            }
        }

        Spacer(Modifier.height(24.dp))
        
        TextButton(onClick = { authViewModel.logout() }) {
            Text("Cancel", color = OnSurfaceVariant)
        }

        Spacer(Modifier.height(40.dp))
    }
}
