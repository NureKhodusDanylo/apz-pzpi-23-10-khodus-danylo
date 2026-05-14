package com.example.robandroid.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import com.example.robandroid.ui.screens.*
import com.example.robandroid.ui.viewmodel.AuthViewModel
import com.example.robandroid.ui.viewmodel.FriendsViewModel
import com.example.robandroid.ui.viewmodel.OrderViewModel
import com.example.robandroid.ui.viewmodel.WalletViewModel

object Routes {
    const val LOGIN = "login"
    const val REGISTER = "register"
    const val DASHBOARD = "dashboard"
    const val ORDERS = "orders"
    const val CREATE_ORDER = "create_order"
    const val FRIENDS = "friends"
    const val PROFILE = "profile"
    const val MAP = "map"
    const val WALLET = "wallet"
    const val COMPLETE_PROFILE = "complete_profile"
}

@Composable
fun AppNavigation(
    navController: NavHostController,
    authViewModel: AuthViewModel,
    orderViewModel: OrderViewModel,
    friendsViewModel: FriendsViewModel,
    walletViewModel: WalletViewModel,
    modifier: Modifier = Modifier
) {
    val authState by authViewModel.uiState.collectAsState()
    val startDestination = if (authState.isAuthenticated) Routes.DASHBOARD else Routes.LOGIN

    NavHost(
        navController = navController,
        startDestination = startDestination,
        modifier = modifier
    ) {
        // ── Auth ──
        composable(Routes.LOGIN) {
            val state by authViewModel.uiState.collectAsState()
            LaunchedEffect(state.needsAdditionalInfo) {
                if (state.needsAdditionalInfo) {
                    navController.navigate(Routes.COMPLETE_PROFILE)
                }
            }

            LoginScreen(
                authViewModel = authViewModel,
                onNavigateToRegister = {
                    navController.navigate(Routes.REGISTER) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
                    }
                },
                onLoginSuccess = {
                    navController.navigate(Routes.DASHBOARD) {
                        popUpTo(0) { inclusive = true }
                    }
                },
            )
        }

        composable(Routes.REGISTER) {
            val state by authViewModel.uiState.collectAsState()
            LaunchedEffect(state.needsAdditionalInfo) {
                if (state.needsAdditionalInfo) {
                    navController.navigate(Routes.COMPLETE_PROFILE)
                }
            }

            RegisterScreen(
                authViewModel = authViewModel,
                onNavigateToLogin = {
                    navController.navigate(Routes.LOGIN) {
                        popUpTo(Routes.REGISTER) { inclusive = true }
                    }
                },
                onRegisterSuccess = {
                    navController.navigate(Routes.DASHBOARD) {
                        popUpTo(0) { inclusive = true }
                    }
                },
            )
        }

        composable(Routes.COMPLETE_PROFILE) {
            CompleteProfileScreen(
                authViewModel = authViewModel,
                onSuccess = {
                    navController.navigate(Routes.DASHBOARD) {
                        popUpTo(0) { inclusive = true }
                    }
                }
            )
        }

        // ── Main screens (require auth) ──
        composable(Routes.DASHBOARD) {
            DashboardScreen(
                user = authState.user,
                orderViewModel = orderViewModel,
                onNavigateToOrders = { 
                    navController.navigate(Routes.ORDERS) {
                        popUpTo(navController.graph.startDestinationId) { saveState = true }
                        launchSingleTop = true
                        restoreState = true
                    }
                },
                onNavigateToCreateOrder = { 
                    navController.navigate(Routes.CREATE_ORDER) {
                        launchSingleTop = true
                    }
                },
            )
        }

        composable(Routes.ORDERS) {
            OrdersScreen(
                orderViewModel = orderViewModel,
                userId = authState.user?.id ?: 0,
                onNavigateToCreateOrder = { navController.navigate(Routes.CREATE_ORDER) },
            )
        }

        composable(Routes.CREATE_ORDER) {
            CreateOrderScreen(
                orderViewModel = orderViewModel,
                friendsViewModel = friendsViewModel,
                onOrderCreated = { navController.popBackStack() },
            )
        }

        composable(Routes.FRIENDS) {
            FriendsScreen(
                friendsViewModel = friendsViewModel,
                currentUserId = authState.user?.id ?: 0,
            )
        }

        composable(Routes.WALLET) {
            WalletScreen(
                walletViewModel = walletViewModel,
            )
        }

        composable(Routes.PROFILE) {
            ProfileScreen(
                authViewModel = authViewModel,
                onLogout = {
                    navController.navigate(Routes.LOGIN) {
                        popUpTo(0) { inclusive = true }
                    }
                },
            )
        }

        composable(Routes.MAP) {
            MapScreen()
        }
    }
}
