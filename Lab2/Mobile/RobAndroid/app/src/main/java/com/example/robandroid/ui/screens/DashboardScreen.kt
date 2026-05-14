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
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.robandroid.data.model.Order
import com.example.robandroid.data.model.User
import com.example.robandroid.ui.components.*
import com.example.robandroid.ui.theme.*
import com.example.robandroid.ui.viewmodel.OrderViewModel

@Composable
fun DashboardScreen(
    user: User?,
    orderViewModel: OrderViewModel,
    onNavigateToOrders: () -> Unit,
    onNavigateToCreateOrder: () -> Unit,
) {
    val orderState by orderViewModel.uiState.collectAsState()

    LaunchedEffect(Unit) {
        orderViewModel.loadMyOrders()
    }

    val activeOrders = orderState.orders.filter { it.status in listOf("AwaitingPayment", "AwaitingConfirmation", "Pending", "Processing", "EnRoute") }
    val recentOrders = orderState.orders.take(5)

    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .background(Background)
            .statusBarsPadding()
            .padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        item {
            Spacer(Modifier.height(16.dp))

            // ── Welcome section ──
            SketchCard(rotationDegrees = -0.3f) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Column(modifier = Modifier.weight(1f)) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Text(
                                text = "Hello, ${user?.userName ?: "User"}",
                                style = MaterialTheme.typography.headlineMedium,
                                color = PrimaryContainer,
                                fontWeight = FontWeight.Bold,
                            )
                            Spacer(Modifier.width(8.dp))
                            Icon(Icons.Outlined.WavingHand, null, tint = PrimaryContainer, modifier = Modifier.size(28.dp).rotate(10f))
                        }
                        Spacer(Modifier.height(4.dp))
                        Text(
                            text = "Your delivery dashboard",
                            style = MaterialTheme.typography.bodyMedium,
                            color = OnSurfaceVariant,
                        )
                    }
                    SketchAvatar(
                        imageUrl = user?.profilePhotoUrl,
                        size = 56.dp,
                    )
                }
            }
        }

        // ── Quick Actions ──
        item {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                SketchButton(
                    onClick = onNavigateToCreateOrder,
                    modifier = Modifier.weight(1f),
                    icon = Icons.Outlined.Add,
                ) {
                    Text("New Order")
                }
                SketchButton(
                    onClick = onNavigateToOrders,
                    modifier = Modifier.weight(1f),
                    variant = SketchButtonVariant.Secondary,
                    icon = Icons.Outlined.List,
                ) {
                    Text("My Orders")
                }
            }
        }

        // ── Stats Section ──
        item {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Outlined.Leaderboard, null, tint = PrimaryContainer, modifier = Modifier.size(20.dp))
                Spacer(Modifier.width(8.dp))
                Text(
                    "Overview",
                    style = MaterialTheme.typography.titleMedium,
                    color = PrimaryContainer,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.rotate(-1f),
                )
            }
        }

        item {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                StatCard(
                    label = "Active",
                    value = activeOrders.size.toString(),
                    icon = Icons.Outlined.RocketLaunch,
                    modifier = Modifier.weight(1f),
                )
                StatCard(
                    label = "Total",
                    value = orderState.orders.size.toString(),
                    icon = Icons.Outlined.Inventory,
                    modifier = Modifier.weight(1f),
                )
                StatCard(
                    label = "Done",
                    value = orderState.orders.count { it.status == "Delivered" }.toString(),
                    icon = Icons.Outlined.CheckCircle,
                    modifier = Modifier.weight(1f),
                )
            }
        }

        // ── Active Orders ──
        if (activeOrders.isNotEmpty()) {
            item {
                Spacer(Modifier.height(8.dp))
                Row(verticalAlignment = Alignment.CenterVertically) {
                    androidx.compose.material3.Icon(
                        imageVector = Icons.Outlined.LocalShipping,
                        contentDescription = null,
                        tint = PrimaryContainer,
                        modifier = androidx.compose.ui.Modifier.size(20.dp)
                    )
                    Spacer(Modifier.width(8.dp))
                    Text(
                        "Active Deliveries",
                        style = MaterialTheme.typography.titleMedium,
                        color = PrimaryContainer,
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier.rotate(0.5f),
                    )
                }
            }

            items(activeOrders.take(3)) { order ->
                OrderMiniCard(order = order)
            }
        }

        // ── Recent Activity ──
        if (recentOrders.isNotEmpty()) {
            item {
                Spacer(Modifier.height(8.dp))
                Row(verticalAlignment = Alignment.CenterVertically) {
                    androidx.compose.material3.Icon(
                        imageVector = Icons.Outlined.Assignment,
                        contentDescription = null,
                        tint = PrimaryContainer,
                        modifier = androidx.compose.ui.Modifier.size(20.dp)
                    )
                    Spacer(Modifier.width(8.dp))
                    Text(
                        "Recent Activity",
                        style = MaterialTheme.typography.titleMedium,
                        color = PrimaryContainer,
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier.rotate(-0.5f),
                    )
                }
            }

            items(recentOrders) { order ->
                OrderMiniCard(order = order)
            }
        }

        // ── Loading / Empty ──
        if (orderState.isLoading) {
            item {
                Box(
                    modifier = Modifier.fillMaxWidth().padding(32.dp),
                    contentAlignment = Alignment.Center
                ) {
                    CircularProgressIndicator(color = PrimaryContainer)
                }
            }
        }

        if (orderState.orders.isEmpty() && !orderState.isLoading) {
            item {
                SketchCard(rotationDegrees = 0.5f) {
                    Column(
                        horizontalAlignment = Alignment.CenterHorizontally,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        androidx.compose.material3.Icon(
                            imageVector = Icons.Outlined.MailOutline,
                            contentDescription = null,
                            tint = PrimaryContainer,
                            modifier = androidx.compose.ui.Modifier.size(48.dp)
                        )
                        Spacer(Modifier.height(8.dp))
                        Text(
                            "No orders yet",
                            style = MaterialTheme.typography.titleMedium,
                            color = PrimaryContainer,
                        )
                        Spacer(Modifier.height(4.dp))
                        Text(
                            "Create your first delivery!",
                            style = MaterialTheme.typography.bodySmall,
                            color = OnSurfaceVariant,
                        )
                        Spacer(Modifier.height(16.dp))
                        SketchButton(onClick = onNavigateToCreateOrder, icon = Icons.Outlined.Add) {
                            Text("Create Order")
                        }
                    }
                }
            }
        }

        item { Spacer(Modifier.height(80.dp)) }
    }
}

@Composable
private fun StatCard(
    label: String,
    value: String,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    modifier: androidx.compose.ui.Modifier = androidx.compose.ui.Modifier,
) {
    SketchCard(modifier = modifier, rotationDegrees = (-1..1).random().toFloat() * 0.5f) {
        androidx.compose.material3.Icon(
            imageVector = icon,
            contentDescription = null,
            tint = PrimaryContainer,
            modifier = androidx.compose.ui.Modifier.size(24.dp)
        )
        Spacer(androidx.compose.ui.Modifier.height(4.dp))
        Text(
            text = value,
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Black,
            color = PrimaryContainer,
        )
        Text(
            text = label,
            style = MaterialTheme.typography.labelSmall,
            color = OnSurfaceVariant,
        )
    }
}

@Composable
fun OrderMiniCard(order: Order) {
    SketchCard(
        modifier = Modifier.fillMaxWidth(),
        showShadow = false,
    ) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = Modifier.fillMaxWidth(),
        ) {
            val statusIcon = when (order.status) {
                "AwaitingPayment" -> Icons.Outlined.Payment
                "AwaitingConfirmation" -> Icons.Outlined.HourglassTop
                "Pending" -> Icons.Outlined.Schedule
                "Processing" -> Icons.Outlined.Settings
                "EnRoute" -> Icons.Outlined.LocalShipping
                "Delivered" -> Icons.Outlined.CheckCircle
                "Cancelled" -> Icons.Outlined.Cancel
                else -> Icons.Outlined.Inventory
            }
            androidx.compose.material3.Icon(
                imageVector = statusIcon,
                contentDescription = null,
                tint = PrimaryContainer,
                modifier = androidx.compose.ui.Modifier.size(24.dp)
            )
            Spacer(androidx.compose.ui.Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = order.name,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )
                Text(
                    text = "To: ${order.recipientName}",
                    style = MaterialTheme.typography.bodySmall,
                    color = OnSurfaceVariant,
                )
            }
            SketchBadge(text = order.status.uppercase())
        }
    }
}
