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
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.example.robandroid.data.model.User
import com.example.robandroid.ui.components.*
import com.example.robandroid.ui.theme.*
import com.example.robandroid.ui.viewmodel.FriendsViewModel
import com.example.robandroid.ui.viewmodel.OrderViewModel

@Composable
fun CreateOrderScreen(
    orderViewModel: OrderViewModel,
    friendsViewModel: FriendsViewModel,
    onOrderCreated: () -> Unit,
) {
    val orderState by orderViewModel.uiState.collectAsState()
    val friendsState by friendsViewModel.uiState.collectAsState()

    var name by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var weight by remember { mutableStateOf("") }
    var productPrice by remember { mutableStateOf("") }
    var isProductPaid by remember { mutableStateOf(false) }
    var deliveryPayer by remember { mutableIntStateOf(0) } // 0 = Sender, 1 = Recipient
    var recipientSearch by remember { mutableStateOf("") }
    var selectedRecipient by remember { mutableStateOf<User?>(null) }

    // Fetch delivery price estimate from API when weight changes
    LaunchedEffect(weight) {
        val w = weight.toDoubleOrNull() ?: 0.0
        orderViewModel.estimateDeliveryPrice(w)
    }

    LaunchedEffect(orderState.createSuccess) {
        if (orderState.createSuccess) {
            orderViewModel.clearCreateSuccess()
            onOrderCreated()
        }
    }

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
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Outlined.AddBox, null, tint = PrimaryContainer, modifier = Modifier.size(28.dp).rotate(-5f))
                Spacer(Modifier.width(8.dp))
                Text(
                    text = "Create Order",
                    style = MaterialTheme.typography.headlineMedium,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                    modifier = Modifier.rotate(-0.5f),
                )
            }
        }

        // ── Recipient Search ──
        item {
            SketchCard(rotationDegrees = 0.3f) {
                Text(
                    "Recipient",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )
                Spacer(Modifier.height(8.dp))

                if (selectedRecipient != null) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        modifier = Modifier.fillMaxWidth(),
                    ) {
                        SketchAvatar(
                            imageUrl = selectedRecipient?.profilePhotoUrl,
                            size = 40.dp,
                        )
                        Spacer(Modifier.width(12.dp))
                        Column(modifier = Modifier.weight(1f)) {
                            Text(
                                selectedRecipient!!.userName,
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold,
                            )
                            Text(
                                selectedRecipient!!.email,
                                style = MaterialTheme.typography.bodySmall,
                                color = OnSurfaceVariant,
                            )
                        }
                        IconButton(onClick = {
                            selectedRecipient = null
                            recipientSearch = ""
                        }) {
                            Icon(Icons.Outlined.Close, "Clear", tint = Error)
                        }
                    }
                } else {
                    SketchTextField(
                        value = recipientSearch,
                        onValueChange = {
                            recipientSearch = it
                            if (it.length >= 2) friendsViewModel.searchUsers(it)
                        },
                        placeholder = "Search by name...",
                    )

                    if (friendsState.searchResults.isNotEmpty() && recipientSearch.length >= 2) {
                        Spacer(Modifier.height(8.dp))
                        Column {
                            friendsState.searchResults.take(5).forEach { user ->
                                Row(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .clickable {
                                            selectedRecipient = user
                                            friendsViewModel.clearSearchResults()
                                        }
                                        .padding(vertical = 8.dp),
                                    verticalAlignment = Alignment.CenterVertically,
                                ) {
                                    SketchAvatar(imageUrl = user.profilePhotoUrl, size = 32.dp)
                                    Spacer(Modifier.width(8.dp))
                                    Column {
                                        Text(user.userName, fontWeight = FontWeight.Bold)
                                        Text(user.email, style = MaterialTheme.typography.bodySmall, color = OnSurfaceVariant)
                                    }
                                }
                                if (user != friendsState.searchResults.take(5).last()) {
                                    SketchDivider()
                                }
                            }
                        }
                    }
                }
            }
        }

        // ── Package Details ──
        item {
            SketchCard(rotationDegrees = -0.3f) {
                Text(
                    "Package Details",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )
                Spacer(Modifier.height(12.dp))

                SketchTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = "PACKAGE NAME",
                    placeholder = "e.g. Birthday gift",
                )
                Spacer(Modifier.height(12.dp))

                SketchTextField(
                    value = description,
                    onValueChange = { description = it },
                    label = "DESCRIPTION",
                    placeholder = "What's inside?",
                    singleLine = false,
                    minLines = 3,
                )
                Spacer(Modifier.height(12.dp))

                Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                    SketchTextField(
                        value = weight,
                        onValueChange = { weight = it },
                        label = "WEIGHT (kg)",
                        placeholder = "0.5",
                        keyboardType = KeyboardType.Decimal,
                        modifier = Modifier.weight(1f),
                    )
                    SketchTextField(
                        value = productPrice,
                        onValueChange = { productPrice = it },
                        label = "PRICE ($)",
                        placeholder = "0.00",
                        keyboardType = KeyboardType.Decimal,
                        modifier = Modifier.weight(1f),
                    )
                }

                Spacer(Modifier.height(12.dp))

                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    modifier = Modifier.clickable { isProductPaid = !isProductPaid }
                ) {
                    Checkbox(
                        checked = isProductPaid,
                        onCheckedChange = { isProductPaid = it },
                        colors = CheckboxDefaults.colors(
                            checkedColor = PrimaryContainer,
                            checkmarkColor = OnPrimaryContainer,
                        )
                    )
                    Text(
                        "Product is already paid",
                        style = MaterialTheme.typography.bodyMedium,
                    )
                }
            }
        }

        // ── Who Pays for Delivery ──
        item {
            SketchCard(rotationDegrees = 0.2f) {
                Text(
                    "Who Pays for Delivery?",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = PrimaryContainer,
                )
                Spacer(Modifier.height(12.dp))

                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { deliveryPayer = 0 }
                        .padding(vertical = 8.dp)
                ) {
                    RadioButton(
                        selected = deliveryPayer == 0,
                        onClick = { deliveryPayer = 0 },
                        colors = RadioButtonDefaults.colors(
                            selectedColor = PrimaryContainer,
                        )
                    )
                    Spacer(Modifier.width(8.dp))
                    Text(
                        "I will pay (Sender)",
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = if (deliveryPayer == 0) FontWeight.Bold else FontWeight.Normal,
                    )
                }

                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { deliveryPayer = 1 }
                        .padding(vertical = 8.dp)
                ) {
                    RadioButton(
                        selected = deliveryPayer == 1,
                        onClick = { deliveryPayer = 1 },
                        colors = RadioButtonDefaults.colors(
                            selectedColor = PrimaryContainer,
                        )
                    )
                    Spacer(Modifier.width(8.dp))
                    Text(
                        "Recipient will pay",
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = if (deliveryPayer == 1) FontWeight.Bold else FontWeight.Normal,
                    )
                }
            }
        }

        // ── Delivery Cost Estimate ──
        item {
            val estimatedCost = orderState.estimatedDeliveryPrice

            SketchCard(rotationDegrees = 0.5f) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Column {
                        Text(
                            "Estimated Delivery",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold,
                        )
                        Text(
                            "Calculated by server",
                            style = MaterialTheme.typography.bodySmall,
                            color = OnSurfaceVariant,
                        )
                    }
                    Text(
                        "$${String.format("%.2f", estimatedCost)}",
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Black,
                        color = PrimaryContainer,
                    )
                }
            }
        }

        // ── Error / Submit ──
        item {
            if (orderState.error != null) {
                Text(
                    orderState.error!!,
                    style = MaterialTheme.typography.bodySmall,
                    color = Error,
                    modifier = Modifier.padding(bottom = 8.dp),
                )
            }

            SketchButton(
                onClick = {
                    selectedRecipient?.let { recipient ->
                        orderViewModel.createOrder(
                            recipientId = recipient.id,
                            name = name,
                            description = description,
                            weight = weight.toDoubleOrNull() ?: 0.0,
                            productPrice = productPrice.toDoubleOrNull() ?: 0.0,
                            isProductPaid = isProductPaid,
                            deliveryPayer = deliveryPayer,
                        )
                    }
                },
                modifier = Modifier.fillMaxWidth(),
                isLoading = orderState.isLoading,
                enabled = selectedRecipient != null && name.isNotBlank() && weight.isNotBlank(),
                icon = Icons.Outlined.Send,
            ) {
                Text("Send Package", fontWeight = FontWeight.Bold)
            }
        }

        item { Spacer(Modifier.height(80.dp)) }
    }
}
