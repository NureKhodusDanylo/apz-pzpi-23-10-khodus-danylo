package com.example.robandroid.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
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
import com.example.robandroid.data.model.Order
import com.example.robandroid.ui.components.*
import com.example.robandroid.ui.theme.*
import com.example.robandroid.ui.viewmodel.OrderViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OrdersScreen(
    orderViewModel: OrderViewModel,
    userId: Int,
    onNavigateToCreateOrder: () -> Unit,
) {
    val state by orderViewModel.uiState.collectAsState()

    var filterTab by remember { mutableStateOf("all") }
    var statusFilter by remember { mutableStateOf("All") }
    var selectedOrder by remember { mutableStateOf<Order?>(null) }

    // Handle payment success
    LaunchedEffect(state.paySuccess) {
        if (state.paySuccess) {
            orderViewModel.clearPaySuccess()
        }
    }

    LaunchedEffect(Unit) {
        orderViewModel.loadMyOrders()
    }

    val filteredOrders = state.orders.filter { order ->
        val tabMatch = when (filterTab) {
            "sent" -> order.senderId == userId
            "received" -> order.recipientId == userId
            else -> true
        }
        val statusMatch = statusFilter == "All" || order.status == statusFilter
        tabMatch && statusMatch
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Background)
            .statusBarsPadding()
    ) {
        // ── Header ──
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.weight(1f)) {
                Icon(Icons.Outlined.Inventory, null, tint = PrimaryContainer, modifier = Modifier.size(28.dp).rotate(-5f))
                Spacer(Modifier.width(8.dp))
                Text(
                    text = "Orders",
                    style = MaterialTheme.typography.headlineMedium,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                    modifier = Modifier.rotate(-0.5f),
                )
            }
            SketchButton(
                onClick = onNavigateToCreateOrder,
                icon = Icons.Outlined.Add,
            ) {
                Text("New")
            }
        }

        // ── Filter Tabs ──
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            listOf("all" to "All", "sent" to "Sent", "received" to "Received").forEach { (key, label) ->
                SketchButton(
                    onClick = { filterTab = key },
                    variant = if (filterTab == key) SketchButtonVariant.Primary else SketchButtonVariant.Secondary,
                    modifier = Modifier.weight(1f).height(36.dp),
                ) {
                    Text(label, style = MaterialTheme.typography.labelSmall, fontWeight = FontWeight.Bold)
                }
            }
        }

        Spacer(Modifier.height(8.dp))

        // ── Status Filter (all 7 statuses) ──
        androidx.compose.foundation.lazy.LazyRow(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp),
            horizontalArrangement = Arrangement.spacedBy(6.dp),
            contentPadding = PaddingValues(end = 16.dp)
        ) {
            items(listOf("All", "AwaitingPayment", "AwaitingConfirmation", "Pending", "Processing", "EnRoute", "Delivered", "Cancelled")) { status ->
                FilterChip(
                    selected = statusFilter == status,
                    onClick = { statusFilter = status },
                    label = {
                        Text(
                            when (status) {
                                "AwaitingPayment" -> "Awaiting Pay"
                                "AwaitingConfirmation" -> "Awaiting Confirm"
                                else -> status
                            },
                            style = MaterialTheme.typography.labelSmall
                        )
                    },
                    colors = FilterChipDefaults.filterChipColors(
                        selectedContainerColor = PrimaryContainer,
                        selectedLabelColor = OnPrimaryContainer,
                    ),
                    shape = SketchShapeThin,
                )
            }
        }

        Spacer(Modifier.height(8.dp))

        // ── Error display ──
        state.payError?.let { err ->
            Text(
                err,
                style = MaterialTheme.typography.bodySmall,
                color = Error,
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp),
            )
        }

        // ── Orders List ──
        if (state.isLoading) {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = PrimaryContainer)
            }
        } else if (filteredOrders.isEmpty()) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(32.dp),
                contentAlignment = Alignment.Center,
            ) {
                SketchCard {
                    Column(
                        horizontalAlignment = Alignment.CenterHorizontally,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Icon(Icons.Outlined.MailOutline, null, tint = PrimaryContainer, modifier = Modifier.size(64.dp))
                        Spacer(Modifier.height(8.dp))
                        Text(
                            "No orders found",
                            style = MaterialTheme.typography.titleMedium,
                            color = PrimaryContainer,
                        )
                    }
                }
            }
        } else {
            LazyColumn(
                modifier = Modifier.fillMaxSize().padding(horizontal = 16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                items(filteredOrders) { order ->
                    OrderCard(
                        order = order,
                        userId = userId,
                        isPayLoading = state.isLoading,
                        onCancel = { orderViewModel.cancelOrder(order.id) },
                        onExecute = { orderViewModel.executeOrder(order.id) },
                        onPayDelivery = { orderViewModel.payOrder(order.id, payProduct = false, payDelivery = true) },
                        onConfirmAndPay = {
                            orderViewModel.payOrder(
                                order.id,
                                payProduct = true,
                                payDelivery = order.deliveryPayerName == "Recipient"
                            )
                        },
                        onDetails = { selectedOrder = order },
                    )
                }
                item { Spacer(Modifier.height(80.dp)) }
            }
        }
    }

    // ── Order Detail Dialog ──
    selectedOrder?.let { order ->
        AlertDialog(
            onDismissRequest = { selectedOrder = null },
            confirmButton = {
                TextButton(onClick = { selectedOrder = null }) {
                    Text("Close")
                }
            },
            title = {
                Text(order.name, fontWeight = FontWeight.Bold)
            },
            text = {
                Column {
                    DetailRow("Status", order.status)
                    DetailRow("From", order.senderName)
                    DetailRow("To", order.recipientName)
                    DetailRow("Weight", "${order.weight} kg")
                    DetailRow("Product Price", "$${String.format("%.2f", order.productPrice)}")
                    DetailRow("Delivery Cost", "$${String.format("%.2f", order.deliveryPrice)}")
                    DetailRow("Delivery Payer", order.deliveryPayerName)
                    DetailRow("Product Paid", if (order.isProductPaid) "Yes" else "No")
                    DetailRow("Delivery Paid", if (order.isDeliveryPaid) "Yes" else "No")
                    if (order.description.isNotBlank()) {
                        DetailRow("Description", order.description)
                    }
                    DetailRow("Created", order.createdAt.take(10))
                    if (order.robotName != null) {
                        DetailRow("Robot", order.robotName)
                    }
                    SketchDivider()
                    Spacer(Modifier.height(8.dp))
                    DetailRow("Total Value", "$${String.format("%.2f", order.productPrice + order.deliveryPrice)}")
                }
            },
            shape = SketchShape,
        )
    }
}

@Composable
private fun OrderCard(
    order: Order,
    userId: Int,
    isPayLoading: Boolean,
    onCancel: () -> Unit,
    onExecute: () -> Unit,
    onPayDelivery: () -> Unit,
    onConfirmAndPay: () -> Unit,
    onDetails: () -> Unit,
) {
    SketchCard(
        modifier = Modifier.fillMaxWidth(),
        onClick = onDetails,
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.Top,
        ) {
            Column(modifier = Modifier.weight(1f)) {
                // Sent / Received badge
                SketchBadge(
                    text = if (order.senderId == userId) "SENT" else "RECEIVED",
                    backgroundColor = SecondaryFixed,
                    textColor = OnSecondaryFixed,
                    rotationDegrees = -2f,
                )
                Spacer(Modifier.height(8.dp))

                Text(
                    text = order.name,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )
                Spacer(Modifier.height(4.dp))
                Text(
                    text = if (order.senderId == userId) "To: ${order.recipientName}" else "From: ${order.senderName}",
                    style = MaterialTheme.typography.bodySmall,
                    color = OnSurfaceVariant,
                )
                Spacer(Modifier.height(4.dp))

                // Your Cost calculation (matching web logic)
                val yourCost = if (order.senderId == userId) {
                    if (order.deliveryPayerName == "Sender") order.deliveryPrice else 0.0
                } else {
                    order.productPrice + (if (order.deliveryPayerName == "Recipient") order.deliveryPrice else 0.0)
                }
                Text(
                    text = "$${String.format("%.2f", yourCost)} Your Cost",
                    style = MaterialTheme.typography.bodySmall,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )
                Text(
                    text = "Total: $${String.format("%.2f", order.productPrice + order.deliveryPrice)}",
                    style = MaterialTheme.typography.labelSmall,
                    color = OnSurfaceVariant,
                )
            }

            Column(horizontalAlignment = Alignment.End) {
                SketchBadge(
                    text = order.status.uppercase(),
                    backgroundColor = when (order.status) {
                        "AwaitingPayment" -> ErrorContainer
                        "AwaitingConfirmation" -> ErrorContainer
                        "Pending" -> TertiaryFixedDim
                        "Processing" -> SecondaryFixed
                        "EnRoute" -> TertiaryFixedDim.copy(alpha = 0.7f)
                        "Delivered" -> TertiaryFixedDim
                        "Cancelled" -> ErrorContainer
                        else -> SurfaceVariant
                    },
                    rotationDegrees = 3f,
                )

                // ── Action buttons based on status ──

                // Sender: AwaitingPayment → Pay Delivery
                if (order.senderId == userId && order.status == "AwaitingPayment") {
                    Spacer(Modifier.height(8.dp))
                    SketchButton(
                        onClick = onPayDelivery,
                        variant = SketchButtonVariant.Primary,
                        isLoading = isPayLoading,
                        icon = Icons.Outlined.Payment,
                    ) {
                        Text("Pay Delivery", style = MaterialTheme.typography.labelSmall)
                    }
                }

                // Recipient: AwaitingConfirmation → Confirm & Pay
                if (order.recipientId == userId && order.status == "AwaitingConfirmation") {
                    Spacer(Modifier.height(8.dp))
                    SketchButton(
                        onClick = onConfirmAndPay,
                        variant = SketchButtonVariant.Primary,
                        isLoading = isPayLoading,
                        icon = Icons.Outlined.CheckCircle,
                    ) {
                        Text("Confirm & Pay", style = MaterialTheme.typography.labelSmall)
                    }
                }

                // Sender: Pending → Execute + Cancel
                if (order.status == "Pending") {
                    Spacer(Modifier.height(8.dp))
                    Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                        if (order.senderId == userId) {
                            SketchButton(
                                onClick = onExecute,
                                variant = SketchButtonVariant.Primary,
                            ) {
                                Text("Execute", style = MaterialTheme.typography.labelSmall)
                            }
                        }
                        SketchButton(
                            onClick = onCancel,
                            variant = SketchButtonVariant.Error,
                        ) {
                            Text("Cancel", style = MaterialTheme.typography.labelSmall)
                        }
                    }
                }
            }
        }

        // ── Payment required banners ──
        if (order.senderId == userId && order.status == "AwaitingPayment") {
            Spacer(Modifier.height(8.dp))
            SketchCard(rotationDegrees = -0.5f, showShadow = false) {
                Text(
                    "Pay $${String.format("%.2f", order.deliveryPrice)} for delivery to proceed.",
                    style = MaterialTheme.typography.bodySmall,
                    fontWeight = FontWeight.Bold,
                    color = Error,
                )
            }
        }

        if (order.recipientId == userId && order.status == "AwaitingConfirmation") {
            Spacer(Modifier.height(8.dp))
            val amount = order.productPrice + (if (order.deliveryPayerName == "Recipient") order.deliveryPrice else 0.0)
            val breakdown = if (order.deliveryPayerName == "Recipient") "Product + Delivery" else "Product only"
            SketchCard(rotationDegrees = 0.5f, showShadow = false) {
                Text(
                    "Pay $${String.format("%.2f", amount)} ($breakdown) to receive this order.",
                    style = MaterialTheme.typography.bodySmall,
                    fontWeight = FontWeight.Bold,
                    color = Error,
                )
            }
        }
    }
}

@Composable
private fun DetailRow(label: String, value: String) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 2.dp),
    ) {
        Text(
            text = "$label: ",
            style = MaterialTheme.typography.bodySmall,
            fontWeight = FontWeight.Bold,
            color = PrimaryContainer,
        )
        Text(
            text = value,
            style = MaterialTheme.typography.bodySmall,
            color = OnSurfaceVariant,
        )
    }
}
