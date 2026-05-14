package com.example.robandroid.data.remote

import com.example.robandroid.data.model.*
import okhttp3.MultipartBody
import okhttp3.RequestBody
import retrofit2.Response
import retrofit2.http.*

interface ApiService {

    // ════════════════════ Auth ════════════════════
    @POST("Auth/login")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>

    @POST("Auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>

    @POST("Auth/complete-google-registration")
    suspend fun completeGoogleRegistration(@Body request: CompleteGoogleRegRequest): Response<AuthResponse>

    @GET("User/profile")
    suspend fun getCurrentUser(): Response<User>

    @Multipart
    @PUT("User/profile")
    suspend fun updateProfile(
        @Part("userName") userName: RequestBody?,
        @Part("phoneNumber") phoneNumber: RequestBody?,
        @Part("password") password: RequestBody?,
        @Part profilePhoto: MultipartBody.Part?
    ): Response<ProfileUpdateResponse>

    // ════════════════════ Users ════════════════════
    @GET("User")
    suspend fun getAllUsers(): Response<List<User>>

    @GET("User/search")
    suspend fun searchUsers(@Query("query") query: String): Response<List<User>>

    @GET("User/{id}")
    suspend fun getUserById(@Path("id") id: Int): Response<User>

    // ════════════════════ Orders ════════════════════
    @GET("Order")
    suspend fun getAllOrders(): Response<List<Order>>

    @GET("Order/{id}")
    suspend fun getOrderById(@Path("id") id: Int): Response<Order>

    @GET("Order/my-orders")
    suspend fun getMyOrders(): Response<List<Order>>

    @Multipart
    @POST("Order")
    suspend fun createOrder(
        @Part("name") name: RequestBody,
        @Part("description") description: RequestBody,
        @Part("weight") weight: RequestBody,
        @Part("productPrice") productPrice: RequestBody,
        @Part("isProductPaid") isProductPaid: RequestBody,
        @Part("recipientId") recipientId: RequestBody,
        @Part("deliveryPayer") deliveryPayer: RequestBody,
        @Part files: List<MultipartBody.Part>?
    ): Response<Order>

    @PUT("Order/{id}/status")
    suspend fun updateOrderStatus(
        @Path("id") id: Int,
        @Body request: UpdateOrderStatusRequest
    ): Response<Order>

    @POST("Order/{orderId}/assign/{robotId}")
    suspend fun assignRobot(
        @Path("orderId") orderId: Int,
        @Path("robotId") robotId: Int
    ): Response<Order>

    @POST("Order/{id}/cancel")
    suspend fun cancelOrder(@Path("id") id: Int): Response<Order>

    @POST("Order/{id}/execute")
    suspend fun executeOrder(@Path("id") id: Int): Response<Unit>

    @DELETE("Order/{id}")
    suspend fun deleteOrder(@Path("id") id: Int): Response<Unit>

    // ════════════════════ Robots ════════════════════
    @GET("Robot")
    suspend fun getAllRobots(): Response<List<Robot>>

    @GET("Robot/{id}")
    suspend fun getRobotById(@Path("id") id: Int): Response<Robot>

    @GET("Robot/available")
    suspend fun getAvailableRobots(): Response<List<Robot>>

    @GET("Robot/status/{status}")
    suspend fun getRobotsByStatus(@Path("status") status: String): Response<List<Robot>>

    @GET("Robot/type/{type}")
    suspend fun getRobotsByType(@Path("type") type: String): Response<List<Robot>>

    // ════════════════════ Nodes ════════════════════
    @GET("Node")
    suspend fun getAllNodes(): Response<List<Node>>

    @GET("Node/{id}")
    suspend fun getNodeById(@Path("id") id: Int): Response<Node>

    @GET("Node/type/{type}")
    suspend fun getNodesByType(@Path("type") type: String): Response<List<Node>>

    // ════════════════════ Admin ════════════════════
    @GET("Admin/stats")
    suspend fun getAdminStats(): Response<AdminStats>

    @GET("Admin/analytics/robot-efficiency")
    suspend fun getRobotEfficiency(): Response<List<RobotEfficiency>>

    // ════════════════════ Map ════════════════════
    @GET("Map/data")
    suspend fun getMapData(): Response<MapData>

    // ════════════════════ Friendship ════════════════════
    @GET("Friendship/list")
    suspend fun getFriends(): Response<List<Friend>>

    @GET("Friendship/requests/pending")
    suspend fun getPendingRequests(): Response<List<FriendRequest>>

    @POST("Friendship/request")
    suspend fun sendFriendRequest(@Body request: SendFriendRequest): Response<Unit>

    @POST("Friendship/request/{requestId}/accept")
    suspend fun acceptFriendRequest(@Path("requestId") requestId: Int): Response<Unit>

    @POST("Friendship/request/{requestId}/reject")
    suspend fun rejectFriendRequest(@Path("requestId") requestId: Int): Response<Unit>

    @DELETE("Friendship/{friendId}")
    suspend fun removeFriend(@Path("friendId") friendId: Int): Response<Unit>

    // ════════════════════ Payments ════════════════════
    @POST("Payments/pay-order")
    suspend fun payOrder(@Body request: PayOrderRequest): Response<PayOrderResponse>

    // ════════════════════ Wallet ════════════════════
    @GET("Wallet/balance")
    suspend fun getWalletBalance(): Response<WalletBalanceResponse>

    @POST("Wallet/withdraw")
    suspend fun withdrawWallet(): Response<WalletWithdrawResponse>

    // ════════════════════ Order Estimate ════════════════════
    @GET("Order/estimate-price")
    suspend fun estimateDeliveryPrice(@Query("weight") weight: Double): Response<EstimateDeliveryPriceResponse>
}
