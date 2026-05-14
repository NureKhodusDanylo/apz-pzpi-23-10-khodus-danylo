package com.example.robandroid.data.model

import com.google.gson.annotations.SerializedName

// ── User ──
data class User(
    val id: Int = 0,
    val userName: String = "",
    val email: String = "",
    val phoneNumber: String = "",
    val address: String = "",
    val role: String = "User",           // "User" | "Admin" | "Iot"
    val personalNodeId: Int = 0,
    val profilePhotoUrl: String? = null
)

// ── Order ──
data class Order(
    val id: Int = 0,
    val name: String = "",
    val description: String = "",
    val weight: Double = 0.0,
    val deliveryPrice: Double = 0.0,
    val productPrice: Double = 0.0,
    val isProductPaid: Boolean = false,
    val deliveryPayer: String = "",
    val deliveryPayerName: String = "",
    val isDeliveryPaid: Boolean = false,
    val status: String = "Pending",      // AwaitingPayment | AwaitingConfirmation | Pending | Processing | EnRoute | Delivered | Cancelled
    val senderId: Int = 0,
    val senderName: String = "",
    val recipientId: Int = 0,
    val recipientName: String = "",
    val robotId: Int? = null,
    val robotName: String? = null,
    val pickupNodeId: Int = 0,
    val pickupNodeName: String = "",
    val dropoffNodeId: Int = 0,
    val dropoffNodeName: String = "",
    val createdAt: String = "",
    val completedAt: String? = null,
    val images: List<OrderImage>? = null
)

data class OrderImage(
    val id: Int = 0,
    val fileName: String = "",
    val filePath: String = "",
    val contentType: String = ""
)

// ── Robot ──
data class Robot(
    val id: Int = 0,
    val serialNumber: String = "",
    val type: String = "Ground",         // Ground | Aerial
    val status: String = "Idle",         // Idle | Delivering | Charging | Maintenance
    val batteryLevel: Double = 0.0,
    val currentNodeId: Int? = null,
    val currentNode: Node? = null,
    val currentLatitude: Double? = null,
    val currentLongitude: Double? = null,
    val targetNodeId: Int? = null
)

// ── Node ──
data class Node(
    val id: Int = 0,
    val name: String = "",
    val type: String = "UserNode",       // UserNode | ChargingStation | Depot
    val latitude: Double = 0.0,
    val longitude: Double = 0.0,
    val address: String? = null
)

// ── Admin Stats ──
data class AdminStats(
    val totalUsers: Int = 0,
    val totalOrders: Int = 0,
    val totalRobots: Int = 0,
    val totalNodes: Int = 0,
    val activeOrders: Int = 0,
    val completedOrders: Int = 0,
    val cancelledOrders: Int = 0,
    val availableRobots: Int = 0,
    val busyRobots: Int = 0,
    val chargingRobots: Int = 0,
    val averageBatteryLevel: Double = 0.0,
    val totalRevenue: Double = 0.0,
    val deliveryRevenue: Double = 0.0,
    val productRevenue: Double = 0.0
)

data class RobotEfficiency(
    val robotId: Int = 0,
    val serialNumber: String = "",
    val completedOrders: Int = 0,
    val batteryLevel: Double = 0.0,
    val efficiencyScore: Double = 0.0
)

// ── Auth ──
data class LoginRequest(
    val email: String? = null,
    val password: String? = null,
    val googleJwtToken: String? = null
)

data class RegisterRequest(
    val userName: String = "",
    val email: String = "",
    val password: String? = null,
    val googleJwtToken: String? = null,
    val phoneNumber: String = "",
    val latitude: Double = 0.0,
    val longitude: Double = 0.0,
    val address: String? = null
)

data class AuthResponse(
    val token: String? = null,
    val status: String = "",
    val message: String = "",
    val googleId: String? = null,
    val email: String? = null,
    val userName: String? = null
)

data class CompleteGoogleRegRequest(
    val googleId: String = "",
    val email: String = "",
    val userName: String = "",
    val phoneNumber: String = "",
    val latitude: Double = 0.0,
    val longitude: Double = 0.0,
    val address: String? = null,
    val adminKey: String? = null
)

data class UpdateOrderStatusRequest(
    val status: String = ""
)

// ── Map ──
data class RobotMapPosition(
    val id: Int = 0,
    val name: String = "",
    val model: String = "",
    val type: String = "Ground",
    val typeName: String = "",
    val status: String = "Idle",
    val statusName: String = "",
    val batteryLevel: Double = 0.0,
    val latitude: Double? = null,
    val longitude: Double? = null,
    val currentNodeId: Int? = null,
    val currentNodeName: String? = null,
    val targetNodeId: Int? = null,
    val targetNodeName: String? = null,
    val activeOrdersCount: Int = 0
)

data class NodeMapPosition(
    val id: Int = 0,
    val name: String = "",
    val latitude: Double = 0.0,
    val longitude: Double = 0.0,
    val type: String = "UserNode",
    val typeName: String = "",
    val robotsAtNode: Int? = null
)

data class MapData(
    val robots: List<RobotMapPosition> = emptyList(),
    val nodes: List<NodeMapPosition> = emptyList()
)

// ── Friendship ──
data class Friend(
    val id: Int = 0,
    val userName: String = "",
    val email: String = "",
    val phoneNumber: String = "",
    val profilePhotoUrl: String? = null,
    val lastLatitude: Double? = null,
    val lastLongitude: Double? = null,
    val personalNodeId: Int? = null
)

data class FriendRequest(
    val id: Int = 0,
    val senderId: Int = 0,
    val senderName: String = "",
    val senderEmail: String = "",
    val senderPhotoUrl: String? = null
)

data class SendFriendRequest(
    val emailOrPhone: String? = null,
    val targetUserId: Int? = null
)

// ── Payment ──
data class PayOrderRequest(
    val orderId: Int,
    val payProduct: Boolean,
    val payDelivery: Boolean,
    val paymentMethod: String = "wallet",
    val stripeCardToken: String? = null
)

data class PayOrderResponse(
    val success: Boolean = false,
    val transactionId: String = "",
    val paymentMethod: String = "",
    val amount: Double = 0.0,
    val currency: String = "",
    val processedAt: String = "",
    val errorMessage: String? = null,
    val orderId: Int? = null,
    val productPaid: Boolean = false,
    val deliveryPaid: Boolean = false
)

// ── Wallet ──
data class WalletBalanceResponse(
    val success: Boolean = false,
    val balance: Double = 0.0
)

data class WalletWithdrawResponse(
    val success: Boolean = false,
    val message: String = "",
    val newBalance: Double = 0.0,
    val withdrawnAmount: Double = 0.0
)

// ── Delivery Price Estimate ──
data class EstimateDeliveryPriceResponse(
    val deliveryPrice: Double = 0.0
)

// ── Profile Update Response (backend returns { message, profile }) ──
data class ProfileUpdateResponse(
    val message: String = "",
    val profile: User? = null
)
