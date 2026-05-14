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
fun LoginScreen(
    authViewModel: AuthViewModel,
    onNavigateToRegister: () -> Unit,
    onLoginSuccess: () -> Unit,
) {
    val state by authViewModel.uiState.collectAsState()
    val context = LocalContext.current

    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var showPassword by remember { mutableStateOf(false) }

    // Navigate on successful auth
    LaunchedEffect(state.isAuthenticated) {
        if (state.isAuthenticated) onLoginSuccess()
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
        Spacer(Modifier.height(80.dp))

        // ── Logo area ──
        Icon(
            imageVector = Icons.Outlined.SmartToy,
            contentDescription = null,
            modifier = Modifier.size(80.dp),
            tint = PrimaryContainer
        )
        Spacer(Modifier.height(8.dp))
        Text(
            text = "RobDelivery",
            style = MaterialTheme.typography.headlineLarge,
            color = PrimaryContainer,
        )
        Text(
            text = "robot delivery service",
            style = MaterialTheme.typography.bodyMedium,
            color = OnSurfaceVariant,
        )

        Spacer(Modifier.height(40.dp))

        // ── Login Card ──
        SketchCard(
            modifier = Modifier.fillMaxWidth(),
            rotationDegrees = -0.5f,
        ) {
            Text(
                text = "Welcome back",
                style = MaterialTheme.typography.headlineSmall,
                color = PrimaryContainer,
                fontWeight = FontWeight.Bold,
            )
            Text(
                text = "Sign in to your account",
                style = MaterialTheme.typography.bodySmall,
                color = OnSurfaceVariant,
            )

            Spacer(Modifier.height(20.dp))

            SketchTextField(
                value = email,
                onValueChange = { email = it },
                label = "EMAIL",
                placeholder = "your@email.com",
            )

            Spacer(Modifier.height(12.dp))

            SketchTextField(
                value = password,
                onValueChange = { password = it },
                label = "PASSWORD",
                placeholder = "••••••••",
                isPassword = !showPassword,
            )

            Spacer(Modifier.height(4.dp))
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable { showPassword = !showPassword },
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Icon(
                    if (showPassword) Icons.Outlined.Visibility else Icons.Outlined.VisibilityOff,
                    contentDescription = "Toggle password",
                    modifier = Modifier.size(16.dp),
                    tint = OnSurfaceVariant,
                )
                Spacer(Modifier.width(4.dp))
                Text(
                    if (showPassword) "Hide" else "Show",
                    style = MaterialTheme.typography.labelSmall,
                    color = OnSurfaceVariant,
                )
            }

            Spacer(Modifier.height(20.dp))

            // Error
            if (state.error != null) {
                Text(
                    text = state.error!!,
                    style = MaterialTheme.typography.bodySmall,
                    color = Error,
                    modifier = Modifier.padding(bottom = 8.dp),
                )
            }

            SketchButton(
                onClick = { authViewModel.login(email, password) },
                modifier = Modifier.fillMaxWidth(),
                isLoading = state.isLoading,
                enabled = email.isNotBlank() && password.isNotBlank(),
            ) {
                Text("Sign In", fontWeight = FontWeight.Bold)
            }

            Spacer(Modifier.height(16.dp))

            // ── Divider ──
            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.fillMaxWidth(),
            ) {
                HorizontalDivider(modifier = Modifier.weight(1f), color = OutlineVariant)
                Text(
                    "  or continue with  ",
                    style = MaterialTheme.typography.labelSmall,
                    color = OnSurfaceVariant.copy(alpha = 0.5f),
                )
                HorizontalDivider(modifier = Modifier.weight(1f), color = OutlineVariant)
            }

            Spacer(Modifier.height(16.dp))

            // ── Google Sign-In ──
            SketchButton(
                onClick = { authViewModel.signInWithGoogle(context) },
                modifier = Modifier.fillMaxWidth(),
                variant = SketchButtonVariant.Secondary,
                isLoading = state.isLoading,
                icon = Icons.Outlined.Search
            ) {
                Text("Continue with Google")
            }
        }

        Spacer(Modifier.height(24.dp))

        // ── Register link ──
        Row(
            horizontalArrangement = Arrangement.Center,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text(
                "Don't have an account? ",
                style = MaterialTheme.typography.bodyMedium,
                color = OnSurfaceVariant,
            )
            Text(
                "Sign Up",
                style = MaterialTheme.typography.bodyMedium,
                color = PrimaryContainer,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.clickable { onNavigateToRegister() },
            )
        }

        Spacer(Modifier.height(40.dp))
    }
}
