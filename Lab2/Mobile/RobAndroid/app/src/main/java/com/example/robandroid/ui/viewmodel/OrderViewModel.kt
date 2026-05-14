package com.example.robandroid.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.robandroid.data.model.Order
import com.example.robandroid.data.model.PayOrderRequest
import com.example.robandroid.data.remote.RetrofitClient
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody

data class OrderUiState(
    val orders: List<Order> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null,
    val createSuccess: Boolean = false,
    val paySuccess: Boolean = false,
    val payError: String? = null,
    val estimatedDeliveryPrice: Double = 0.0,
)

class OrderViewModel : ViewModel() {

    private val _uiState = MutableStateFlow(OrderUiState())
    val uiState: StateFlow<OrderUiState> = _uiState.asStateFlow()

    private val api = RetrofitClient.apiService

    fun loadMyOrders() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val response = api.getMyOrders()
                if (response.isSuccessful) {
                    _uiState.update { it.copy(orders = response.body() ?: emptyList(), isLoading = false) }
                } else {
                    _uiState.update { it.copy(isLoading = false, error = "Failed to load orders") }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = e.message) }
            }
        }
    }

    fun createOrder(
        recipientId: Int,
        name: String,
        description: String,
        weight: Double,
        productPrice: Double,
        isProductPaid: Boolean,
        deliveryPayer: Int = 0,
    ) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null, createSuccess = false) }
            try {
                val response = api.createOrder(
                    name = name.toRequestBody("text/plain".toMediaTypeOrNull()),
                    description = description.toRequestBody("text/plain".toMediaTypeOrNull()),
                    weight = weight.toString().toRequestBody("text/plain".toMediaTypeOrNull()),
                    productPrice = productPrice.toString().toRequestBody("text/plain".toMediaTypeOrNull()),
                    isProductPaid = isProductPaid.toString().toRequestBody("text/plain".toMediaTypeOrNull()),
                    recipientId = recipientId.toString().toRequestBody("text/plain".toMediaTypeOrNull()),
                    deliveryPayer = deliveryPayer.toString().toRequestBody("text/plain".toMediaTypeOrNull()),
                    files = null
                )
                if (response.isSuccessful) {
                    _uiState.update { it.copy(isLoading = false, createSuccess = true) }
                    loadMyOrders()
                } else {
                    _uiState.update { it.copy(isLoading = false, error = "Failed to create order") }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = e.message) }
            }
        }
    }

    fun cancelOrder(id: Int) {
        viewModelScope.launch {
            try {
                val response = api.cancelOrder(id)
                if (response.isSuccessful) loadMyOrders()
            } catch (_: Exception) { }
        }
    }

    fun executeOrder(id: Int) {
        viewModelScope.launch {
            _uiState.update { it.copy(error = null) }
            try {
                val response = api.executeOrder(id)
                if (response.isSuccessful) {
                    loadMyOrders()
                } else {
                    _uiState.update { it.copy(error = "Failed to execute order") }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(error = e.message) }
            }
        }
    }

    fun payOrder(orderId: Int, payProduct: Boolean, payDelivery: Boolean) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, payError = null, paySuccess = false) }
            try {
                val request = PayOrderRequest(
                    orderId = orderId,
                    payProduct = payProduct,
                    payDelivery = payDelivery,
                    paymentMethod = "stripe",
                    stripeCardToken = "pm_card_visa" // Stripe test token; replace with real Stripe SDK in production
                )
                val response = api.payOrder(request)
                if (response.isSuccessful && response.body()?.success == true) {
                    _uiState.update { it.copy(isLoading = false, paySuccess = true) }
                    loadMyOrders()
                } else {
                    val errorMsg = response.body()?.errorMessage ?: "Payment failed"
                    _uiState.update { it.copy(isLoading = false, payError = errorMsg) }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, payError = e.message) }
            }
        }
    }

    fun estimateDeliveryPrice(weight: Double) {
        if (weight <= 0) {
            _uiState.update { it.copy(estimatedDeliveryPrice = 0.0) }
            return
        }
        viewModelScope.launch {
            try {
                val response = api.estimateDeliveryPrice(weight)
                if (response.isSuccessful) {
                    _uiState.update {
                        it.copy(estimatedDeliveryPrice = response.body()?.deliveryPrice ?: 0.0)
                    }
                }
            } catch (_: Exception) {
                // Fallback: keep current estimate
            }
        }
    }

    fun clearCreateSuccess() {
        _uiState.update { it.copy(createSuccess = false) }
    }

    fun clearPaySuccess() {
        _uiState.update { it.copy(paySuccess = false, payError = null) }
    }

    fun clearError() {
        _uiState.update { it.copy(error = null) }
    }
}
