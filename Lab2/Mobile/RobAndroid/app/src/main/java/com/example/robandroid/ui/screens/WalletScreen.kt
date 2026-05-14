package com.example.robandroid.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.example.robandroid.ui.components.*
import com.example.robandroid.ui.theme.*
import com.example.robandroid.ui.viewmodel.WalletViewModel

@Composable
fun WalletScreen(
    walletViewModel: WalletViewModel,
) {
    val state by walletViewModel.uiState.collectAsState()
    var showConfirmDialog by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        walletViewModel.loadBalance()
    }

    // Handle withdraw success
    LaunchedEffect(state.withdrawSuccess) {
        if (state.withdrawSuccess) {
            walletViewModel.clearWithdrawSuccess()
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Background)
            .statusBarsPadding()
            .padding(horizontal = 16.dp),
    ) {
        Spacer(Modifier.height(16.dp))

        // ── Header ──
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(
                Icons.Outlined.AccountBalanceWallet,
                null,
                tint = PrimaryContainer,
                modifier = Modifier.size(28.dp).rotate(-5f)
            )
            Spacer(Modifier.width(8.dp))
            Text(
                text = "My Wallet",
                style = MaterialTheme.typography.headlineMedium,
                fontWeight = FontWeight.Bold,
                color = PrimaryContainer,
                modifier = Modifier.rotate(-0.5f),
            )
        }

        Spacer(Modifier.height(8.dp))
        Text(
            text = "Manage your virtual earnings and withdraw to your bank.",
            style = MaterialTheme.typography.bodyMedium,
            color = OnSurfaceVariant,
        )

        Spacer(Modifier.height(24.dp))

        if (state.isLoading && state.balance == 0.0) {
            Box(
                modifier = Modifier.fillMaxWidth().padding(64.dp),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = PrimaryContainer)
            }
        } else {
            // ── Balance Card ──
            SketchCard(rotationDegrees = -0.5f) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    modifier = Modifier.fillMaxWidth().padding(vertical = 24.dp)
                ) {
                    Icon(
                        Icons.Outlined.AccountBalanceWallet,
                        contentDescription = null,
                        tint = PrimaryContainer,
                        modifier = Modifier.size(56.dp),
                    )
                    Spacer(Modifier.height(12.dp))
                    Text(
                        text = "Available Balance",
                        style = MaterialTheme.typography.labelMedium,
                        fontWeight = FontWeight.Black,
                        color = PrimaryContainer.copy(alpha = 0.6f),
                    )
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = "$${String.format("%.2f", state.balance)}",
                        style = MaterialTheme.typography.displayMedium,
                        fontWeight = FontWeight.Black,
                        color = PrimaryContainer,
                    )
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = "Funds are safe in the RobDelivery Ledger",
                        style = MaterialTheme.typography.bodySmall,
                        color = OnSurfaceVariant,
                    )
                }
            }

            Spacer(Modifier.height(20.dp))

            // ── Withdraw Section ──
            SketchCard(rotationDegrees = 0.5f) {
                Text(
                    "Withdraw Funds",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )
                Spacer(Modifier.height(16.dp))

                // Info box
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(SurfaceContainerLow, SketchShapeThin)
                        .padding(12.dp),
                    verticalAlignment = Alignment.Top,
                ) {
                    Icon(
                        Icons.Outlined.Info,
                        null,
                        tint = PrimaryContainer,
                        modifier = Modifier.size(20.dp),
                    )
                    Spacer(Modifier.width(8.dp))
                    Column {
                        Text(
                            "Standard Payout",
                            style = MaterialTheme.typography.bodyMedium,
                            fontWeight = FontWeight.Bold,
                        )
                        Text(
                            "Your entire balance will be transferred to your connected bank account in 2-3 business days.",
                            style = MaterialTheme.typography.bodySmall,
                            color = OnSurfaceVariant,
                        )
                    }
                }

                Spacer(Modifier.height(16.dp))

                // Error display
                state.error?.let { err ->
                    Text(
                        err,
                        style = MaterialTheme.typography.bodySmall,
                        color = Error,
                        modifier = Modifier.padding(bottom = 8.dp),
                    )
                }

                // Withdraw button
                SketchButton(
                    onClick = { showConfirmDialog = true },
                    modifier = Modifier.fillMaxWidth(),
                    isLoading = state.isLoading,
                    enabled = !state.isLoading && state.balance > 0,
                    icon = Icons.Outlined.Payment,
                ) {
                    Text(
                        "Withdraw All ($${String.format("%.2f", state.balance)})",
                        fontWeight = FontWeight.Bold,
                    )
                }

                // Success message
                if (state.withdrawSuccess) {
                    Spacer(Modifier.height(8.dp))
                    Text(
                        "Successfully withdrew $${String.format("%.2f", state.withdrawnAmount)}",
                        style = MaterialTheme.typography.bodySmall,
                        fontWeight = FontWeight.Bold,
                        color = PrimaryContainer,
                        textAlign = TextAlign.Center,
                        modifier = Modifier.fillMaxWidth(),
                    )
                }
            }
        }
    }

    // ── Confirm Dialog ──
    if (showConfirmDialog) {
        AlertDialog(
            onDismissRequest = { showConfirmDialog = false },
            confirmButton = {
                TextButton(onClick = {
                    showConfirmDialog = false
                    walletViewModel.withdraw()
                }) {
                    Text("Withdraw", color = PrimaryContainer, fontWeight = FontWeight.Bold)
                }
            },
            dismissButton = {
                TextButton(onClick = { showConfirmDialog = false }) {
                    Text("Cancel")
                }
            },
            title = { Text("Confirm Withdrawal", fontWeight = FontWeight.Bold) },
            text = {
                Text("Are you sure you want to withdraw all your funds ($${String.format("%.2f", state.balance)})?")
            },
            shape = SketchShape,
        )
    }
}
