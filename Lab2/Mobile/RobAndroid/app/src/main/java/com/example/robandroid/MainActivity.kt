package com.example.robandroid

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.navigation.NavController
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import com.example.robandroid.data.local.TokenManager
import com.example.robandroid.data.remote.RetrofitClient
import com.example.robandroid.navigation.AppNavigation
import com.example.robandroid.navigation.Routes
import com.example.robandroid.ui.theme.RobAndroidTheme
import com.example.robandroid.ui.viewmodel.AuthViewModel
import com.example.robandroid.ui.viewmodel.FriendsViewModel
import com.example.robandroid.ui.viewmodel.OrderViewModel
import com.example.robandroid.ui.viewmodel.WalletViewModel

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Initialize token manager and Retrofit
        val tokenManager = TokenManager(applicationContext)
        RetrofitClient.init(tokenManager)

        enableEdgeToEdge()
        setContent {
            RobAndroidTheme {
                RobDeliveryApp(tokenManager = tokenManager)
            }
        }
    }
}

// ── Bottom navigation items ──
enum class BottomNavItem(
    val route: String,
    val label: String,
    val icon: ImageVector,
) {
    DASHBOARD(Routes.DASHBOARD, "Home", Icons.Outlined.Home),
    ORDERS(Routes.ORDERS, "Orders", Icons.Outlined.Inventory),
    WALLET(Routes.WALLET, "Wallet", Icons.Outlined.AccountBalanceWallet),
    FRIENDS(Routes.FRIENDS, "Friends", Icons.Outlined.People),
    MAP(Routes.MAP, "Map", Icons.Outlined.Map),
    PROFILE(Routes.PROFILE, "Profile", Icons.Outlined.Person),
}

// Routes that show the bottom bar
private val bottomBarRoutes = setOf(
    Routes.DASHBOARD, Routes.ORDERS, Routes.FRIENDS, Routes.PROFILE, Routes.MAP, Routes.WALLET
)

@Composable
fun RobDeliveryApp(tokenManager: TokenManager) {
    val navController = rememberNavController()
    val authViewModel = remember { AuthViewModel(tokenManager) }
    val orderViewModel = remember { OrderViewModel() }
    val friendsViewModel = remember { FriendsViewModel() }
    val walletViewModel = remember { WalletViewModel() }

    val authState by authViewModel.uiState.collectAsState()
    val navBackStackEntry by navController.currentBackStackEntryAsState()
    val currentRoute = navBackStackEntry?.destination?.route

    val showBottomBar = authState.isAuthenticated && currentRoute in bottomBarRoutes

    Scaffold(
        bottomBar = {
            if (showBottomBar) {
                NavigationBar(
                    containerColor = MaterialTheme.colorScheme.surfaceContainer,
                ) {
                    BottomNavItem.entries.forEach { item ->
                        NavigationBarItem(
                            selected = currentRoute == item.route,
                            onClick = {
                                navController.navigate(item.route) {
                                    // Pop up to the start destination of the graph to
                                    // avoid building up a large stack of destinations
                                    // on the back stack as users select items
                                    popUpTo(navController.graph.startDestinationId) {
                                        saveState = true
                                    }
                                    // Avoid multiple copies of the same destination when
                                    // reselecting the same item
                                    launchSingleTop = true
                                    // Restore state when reselecting a previously selected item
                                    restoreState = true
                                }
                            },
                            icon = {
                                Icon(item.icon, contentDescription = item.label)
                            },
                            label = { Text(item.label) },
                            colors = NavigationBarItemDefaults.colors(
                                selectedIconColor = MaterialTheme.colorScheme.primary,
                                selectedTextColor = MaterialTheme.colorScheme.primary,
                                indicatorColor = MaterialTheme.colorScheme.secondaryContainer,
                            ),
                        )
                    }
                }
            }
        }
    ) { innerPadding ->
        AppNavigation(
            navController = navController,
            authViewModel = authViewModel,
            orderViewModel = orderViewModel,
            friendsViewModel = friendsViewModel,
            walletViewModel = walletViewModel,
            modifier = Modifier.padding(innerPadding)
        )
    }
}