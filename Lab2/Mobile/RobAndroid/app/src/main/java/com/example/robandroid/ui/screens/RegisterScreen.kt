package com.example.robandroid.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.example.robandroid.ui.components.*
import com.example.robandroid.ui.theme.*
import com.example.robandroid.ui.viewmodel.AuthViewModel

@Composable
fun RegisterScreen(
    authViewModel: AuthViewModel,
    onNavigateToLogin: () -> Unit,
    onRegisterSuccess: () -> Unit,
) {
    val state by authViewModel.uiState.collectAsState()
    val context = LocalContext.current

    var userName by remember { mutableStateOf("") }
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var confirmPassword by remember { mutableStateOf("") }
    var phoneNumber by remember { mutableStateOf("") }
    var address by remember { mutableStateOf("") }
    var showPassword by remember { mutableStateOf(false) }
    var localError by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(state.isAuthenticated) {
        if (state.isAuthenticated) onRegisterSuccess()
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
            imageVector = Icons.Outlined.SmartToy,
            contentDescription = null,
            modifier = Modifier.size(80.dp),
            tint = PrimaryContainer
        )
        Spacer(Modifier.height(4.dp))
        Text("RobDelivery", style = MaterialTheme.typography.headlineLarge, color = PrimaryContainer)
        Text("Create an account", style = MaterialTheme.typography.bodyMedium, color = OnSurfaceVariant)

        Spacer(Modifier.height(24.dp))

        SketchCard(
            modifier = Modifier.fillMaxWidth(),
            rotationDegrees = 0.3f,
        ) {
            Text(
                "Join the Team",
                style = MaterialTheme.typography.headlineSmall,
                color = PrimaryContainer,
                fontWeight = FontWeight.Bold,
            )

            Spacer(Modifier.height(16.dp))

            SketchTextField(value = userName, onValueChange = { userName = it }, label = "USERNAME", placeholder = "Your name")
            Spacer(Modifier.height(12.dp))
            SketchTextField(value = email, onValueChange = { email = it }, label = "EMAIL", placeholder = "your@email.com")
            Spacer(Modifier.height(12.dp))

            SketchTextField(value = password, onValueChange = { password = it }, label = "PASSWORD", placeholder = "••••••••", isPassword = !showPassword)
            Spacer(Modifier.height(12.dp))
            SketchTextField(value = confirmPassword, onValueChange = { confirmPassword = it }, label = "CONFIRM PASSWORD", placeholder = "••••••••", isPassword = !showPassword)

            Spacer(Modifier.height(4.dp))
            Row(
                modifier = Modifier.fillMaxWidth().clickable { showPassword = !showPassword },
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Icon(
                    if (showPassword) Icons.Outlined.Visibility else Icons.Outlined.VisibilityOff,
                    contentDescription = null, modifier = Modifier.size(16.dp), tint = OnSurfaceVariant,
                )
                Spacer(Modifier.width(4.dp))
                Text(if (showPassword) "Hide" else "Show", style = MaterialTheme.typography.labelSmall, color = OnSurfaceVariant)
            }

            Spacer(Modifier.height(12.dp))
            SketchTextField(value = phoneNumber, onValueChange = { phoneNumber = it }, label = "PHONE", placeholder = "+380XXXXXXXXX")
            Spacer(Modifier.height(12.dp))
            SketchTextField(value = address, onValueChange = { address = it }, label = "ADDRESS", placeholder = "City, Street")

            Spacer(Modifier.height(20.dp))

            // Errors
            val errorText = localError ?: state.error
            if (errorText != null) {
                Text(errorText, style = MaterialTheme.typography.bodySmall, color = Error, modifier = Modifier.padding(bottom = 8.dp))
            }

            SketchButton(
                onClick = {
                    localError = null
                    if (password != confirmPassword) {
                        localError = "Passwords do not match"
                        return@SketchButton
                    }
                    if (password.length < 6) {
                        localError = "Password must be at least 6 characters"
                        return@SketchButton
                    }
                    authViewModel.register(
                        userName = userName, email = email, password = password,
                        phoneNumber = phoneNumber,
                        latitude = 50.4501, longitude = 30.5234,
                        address = address.ifBlank { null },
                    )
                },
                modifier = Modifier.fillMaxWidth(),
                isLoading = state.isLoading,
                enabled = userName.isNotBlank() && email.isNotBlank() && password.isNotBlank() && phoneNumber.isNotBlank(),
            ) {
                Text("Create Account", fontWeight = FontWeight.Bold)
            }

            Spacer(Modifier.height(16.dp))

            Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.fillMaxWidth()) {
                HorizontalDivider(modifier = Modifier.weight(1f), color = OutlineVariant)
                Text("  or  ", style = MaterialTheme.typography.labelSmall, color = OnSurfaceVariant.copy(alpha = 0.5f))
                HorizontalDivider(modifier = Modifier.weight(1f), color = OutlineVariant)
            }

            Spacer(Modifier.height(16.dp))

            SketchButton(
                onClick = { authViewModel.signInWithGoogle(context) },
                modifier = Modifier.fillMaxWidth(),
                variant = SketchButtonVariant.Secondary,
                isLoading = state.isLoading,
                icon = Icons.Outlined.Search
            ) {
                Text("Sign up with Google")
            }
        }

        Spacer(Modifier.height(24.dp))

        Row(horizontalArrangement = Arrangement.Center, modifier = Modifier.fillMaxWidth()) {
            Text("Already have an account? ", style = MaterialTheme.typography.bodyMedium, color = OnSurfaceVariant)
            Text("Sign In", style = MaterialTheme.typography.bodyMedium, color = PrimaryContainer, fontWeight = FontWeight.Bold,
                modifier = Modifier.clickable { onNavigateToLogin() })
        }

        Spacer(Modifier.height(40.dp))
    }
}
