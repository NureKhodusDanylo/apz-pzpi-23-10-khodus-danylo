package com.example.robandroid.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.robandroid.data.remote.RetrofitClient
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class WalletUiState(
    val balance: Double = 0.0,
    val isLoading: Boolean = false,
    val error: String? = null,
    val withdrawSuccess: Boolean = false,
    val withdrawnAmount: Double = 0.0,
)

class WalletViewModel : ViewModel() {

    private val _uiState = MutableStateFlow(WalletUiState())
    val uiState: StateFlow<WalletUiState> = _uiState.asStateFlow()

    private val api = RetrofitClient.apiService

    fun loadBalance() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val response = api.getWalletBalance()
                if (response.isSuccessful) {
                    _uiState.update {
                        it.copy(
                            balance = response.body()?.balance ?: 0.0,
                            isLoading = false
                        )
                    }
                } else {
                    _uiState.update { it.copy(isLoading = false, error = "Failed to load balance") }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = e.message) }
            }
        }
    }

    fun withdraw() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null, withdrawSuccess = false) }
            try {
                val response = api.withdrawWallet()
                if (response.isSuccessful && response.body()?.success == true) {
                    val body = response.body()!!
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            withdrawSuccess = true,
                            withdrawnAmount = body.withdrawnAmount,
                            balance = body.newBalance
                        )
                    }
                } else {
                    val errorMsg = response.body()?.message ?: "Withdrawal failed"
                    _uiState.update { it.copy(isLoading = false, error = errorMsg) }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = e.message) }
            }
        }
    }

    fun clearWithdrawSuccess() {
        _uiState.update { it.copy(withdrawSuccess = false) }
    }

    fun clearError() {
        _uiState.update { it.copy(error = null) }
    }
}
