package com.example.robandroid.ui.screens

import android.Manifest
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import com.example.robandroid.data.model.*
import com.example.robandroid.data.remote.RetrofitClient
import com.example.robandroid.ui.components.*
import com.example.robandroid.ui.theme.*
import com.google.android.gms.location.LocationServices
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.model.BitmapDescriptorFactory
import com.google.android.gms.maps.model.CameraPosition
import com.google.android.gms.maps.model.LatLng
import com.google.maps.android.compose.*
import kotlinx.coroutines.launch

// Kyiv center
private val DEFAULT_CENTER = LatLng(50.4501, 30.5234)

@Composable
fun MapScreen() {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val api = RetrofitClient.apiService

    // ── State ──
    var robots by remember { mutableStateOf<List<RobotMapPosition>>(emptyList()) }
    var nodes by remember { mutableStateOf<List<NodeMapPosition>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var viewMode by remember { mutableStateOf("general") } // "general" | "delivery"
    var selectedRobotId by remember { mutableStateOf<Int?>(null) }
    var selectedItem by remember { mutableStateOf<SelectedMapItem?>(null) }
    var userLocation by remember { mutableStateOf<LatLng?>(null) }

    // ── Camera ──
    val cameraPositionState = rememberCameraPositionState {
        position = CameraPosition.fromLatLngZoom(DEFAULT_CENTER, 12f)
    }

    // ── Location Permission ──
    val locationPermissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { permissions ->
        if (permissions[Manifest.permission.ACCESS_FINE_LOCATION] == true) {
            getUserLocation(context) { location ->
                userLocation = location
                scope.launch {
                    cameraPositionState.animate(CameraUpdateFactory.newLatLngZoom(location, 14f))
                }
            }
        }
    }

    // ── Load data ──
    LaunchedEffect(Unit) {
        try {
            isLoading = true
            val response = api.getMapData()
            if (response.isSuccessful) {
                val data = response.body()!!
                robots = data.robots
                nodes = data.nodes
            } else {
                error = "Failed to load map data"
            }
        } catch (e: Exception) {
            error = e.message ?: "Network error"
        } finally {
            isLoading = false
        }

        // Request location
        if (ContextCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION)
            == PackageManager.PERMISSION_GRANTED
        ) {
            getUserLocation(context) { location -> userLocation = location }
        } else {
            locationPermissionLauncher.launch(
                arrayOf(Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION)
            )
        }
    }

    // ── Filter markers by view mode ──
    val visibleNodes = remember(nodes, viewMode, selectedRobotId) {
        if (viewMode == "delivery") {
            if (selectedRobotId != null) {
                val robot = robots.find { it.id == selectedRobotId }
                nodes.filter { it.id == robot?.currentNodeId || it.id == robot?.targetNodeId }
            } else emptyList()
        } else nodes
    }

    val visibleRobots = remember(robots, viewMode, selectedRobotId) {
        if (viewMode == "delivery") {
            if (selectedRobotId != null) robots.filter { it.id == selectedRobotId } else emptyList()
        } else robots.filter { it.latitude != null && it.longitude != null }
    }

    val activeDeliveries = remember(robots) {
        robots.filter { it.status == "Delivering" }
    }

    if (isLoading) {
        Box(Modifier.fillMaxSize().background(Background), contentAlignment = Alignment.Center) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                CircularProgressIndicator(color = PrimaryContainer)
                Spacer(Modifier.height(12.dp))
                Text("Calibrating satellite feed...", style = MaterialTheme.typography.labelMedium, color = OnSurfaceVariant)
            }
        }
        return
    }

    Column(modifier = Modifier.fillMaxSize().background(Background).statusBarsPadding()) {
        // ── Top Header ──
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 12.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Outlined.Map, null, tint = PrimaryContainer, modifier = Modifier.size(24.dp).rotate(-5f))
                    Spacer(Modifier.width(8.dp))
                    Text(
                        "Network Map",
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold,
                        color = PrimaryContainer,
                        modifier = Modifier.rotate(-0.5f),
                    )
                }
                Text(
                    "Live telemetry for robots & stations",
                    style = MaterialTheme.typography.bodySmall,
                    color = OnSurfaceVariant,
                )
            }
            IconButton(onClick = {
                if (ContextCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION)
                    == PackageManager.PERMISSION_GRANTED
                ) {
                    getUserLocation(context) { location ->
                        userLocation = location
                        scope.launch {
                            cameraPositionState.animate(CameraUpdateFactory.newLatLngZoom(location, 14f))
                        }
                    }
                }
            }) {
                Icon(Icons.Outlined.MyLocation, "My Location", tint = PrimaryContainer)
            }
            IconButton(onClick = {
                scope.launch {
                    isLoading = true
                    try {
                        val response = api.getMapData()
                        if (response.isSuccessful) {
                            val data = response.body()!!
                            robots = data.robots
                            nodes = data.nodes
                        }
                    } catch (_: Exception) {}
                    isLoading = false
                }
            }) {
                Icon(Icons.Outlined.Refresh, "Refresh", tint = PrimaryContainer)
            }
        }

        // ── View Mode Toggle ──
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            SketchButton(
                onClick = { viewMode = "general"; selectedRobotId = null },
                modifier = Modifier.weight(1f),
                variant = if (viewMode == "general") SketchButtonVariant.Primary else SketchButtonVariant.Secondary,
                icon = Icons.Outlined.Public,
            ) {
                Text("General")
            }
            SketchButton(
                onClick = { viewMode = "delivery" },
                modifier = Modifier.weight(1f),
                variant = if (viewMode == "delivery") SketchButtonVariant.Primary else SketchButtonVariant.Secondary,
                icon = Icons.Outlined.LocalShipping,
            ) {
                Text("Delivery")
            }
        }

        // ── Delivery mode: select robot ──
        if (viewMode == "delivery") {
            Spacer(Modifier.height(8.dp))
            if (activeDeliveries.isEmpty()) {
                Text(
                    "   No active deliveries detected",
                    style = MaterialTheme.typography.bodySmall,
                    color = OnSurfaceVariant,
                    modifier = Modifier.padding(horizontal = 16.dp),
                )
            } else {
                LazyRow(
                    contentPadding = PaddingValues(horizontal = 16.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    items(activeDeliveries) { robot ->
                        FilterChip(
                            selected = selectedRobotId == robot.id,
                            onClick = {
                                selectedRobotId = if (selectedRobotId == robot.id) null else robot.id
                                if (robot.latitude != null && robot.longitude != null) {
                                    scope.launch {
                                        cameraPositionState.animate(
                                            CameraUpdateFactory.newLatLngZoom(
                                                LatLng(robot.latitude, robot.longitude), 15f
                                            )
                                        )
                                    }
                                }
                            },
                            label = {
                                Column {
                                    Text(robot.name, style = MaterialTheme.typography.labelSmall, fontWeight = FontWeight.Bold)
                                    Text("→ ${robot.targetNodeName ?: "?"}", style = MaterialTheme.typography.labelSmall)
                                }
                            },
                            leadingIcon = { Icon(Icons.Outlined.SmartToy, null, modifier = Modifier.size(16.dp)) },
                            shape = SketchShapeThin,
                            colors = FilterChipDefaults.filterChipColors(
                                selectedContainerColor = PrimaryContainer,
                                selectedLabelColor = OnPrimaryContainer,
                            ),
                        )
                    }
                }
            }
        }

        Spacer(Modifier.height(8.dp))

        // ── Error ──
        if (error != null) {
            Row(
                modifier = Modifier.padding(horizontal = 16.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(Icons.Outlined.WarningAmber, null, tint = Error, modifier = Modifier.size(16.dp))
                Spacer(Modifier.width(4.dp))
                Text(
                    error!!,
                    color = Error,
                    style = MaterialTheme.typography.bodySmall,
                )
            }
            Spacer(Modifier.height(4.dp))
        }

        // ═══════════ GOOGLE MAP ═══════════
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .weight(1f)
                .padding(horizontal = 8.dp, vertical = 4.dp)
        ) {
            GoogleMap(
                modifier = Modifier.fillMaxSize(),
                cameraPositionState = cameraPositionState,
                uiSettings = MapUiSettings(
                    zoomControlsEnabled = false,
                    myLocationButtonEnabled = false,
                    compassEnabled = true,
                ),
                properties = MapProperties(
                    isMyLocationEnabled = ContextCompat.checkSelfPermission(
                        context, Manifest.permission.ACCESS_FINE_LOCATION
                    ) == PackageManager.PERMISSION_GRANTED,
                ),
            ) {
                // ── Node Markers ──
                visibleNodes.forEach { node ->
                    val hue = when (node.type) {
                        "ChargingStation" -> BitmapDescriptorFactory.HUE_GREEN
                        "Depot" -> BitmapDescriptorFactory.HUE_BLUE
                        "UserNode" -> BitmapDescriptorFactory.HUE_VIOLET
                        else -> BitmapDescriptorFactory.HUE_RED
                    }
                    Marker(
                        state = MarkerState(position = LatLng(node.latitude, node.longitude)),
                        title = node.name,
                        snippet = node.typeName.ifEmpty { node.type },
                        icon = BitmapDescriptorFactory.defaultMarker(hue),
                        onClick = {
                            selectedItem = SelectedMapItem("node", node.name, node.typeName.ifEmpty { node.type },
                                node.latitude, node.longitude, nodeType = node.type)
                            false
                        }
                    )
                }

                // ── Robot Markers ──
                visibleRobots.forEach { robot ->
                    if (robot.latitude != null && robot.longitude != null) {
                        val hue = when (robot.status) {
                            "Idle" -> BitmapDescriptorFactory.HUE_GREEN
                            "Delivering" -> BitmapDescriptorFactory.HUE_ORANGE
                            "Charging" -> BitmapDescriptorFactory.HUE_AZURE
                            "Maintenance" -> BitmapDescriptorFactory.HUE_RED
                            else -> BitmapDescriptorFactory.HUE_YELLOW
                        }
                        Marker(
                            state = MarkerState(position = LatLng(robot.latitude, robot.longitude)),
                            title = robot.name,
                            snippet = "${robot.statusName} • ${robot.batteryLevel.toInt()}%",
                            icon = BitmapDescriptorFactory.defaultMarker(hue),
                            zIndex = 10f,
                            onClick = {
                                selectedItem = SelectedMapItem("robot", robot.name,
                                    "${robot.statusName} • ${robot.batteryLevel.toInt()}% battery",
                                    robot.latitude, robot.longitude,
                                    robotStatus = robot.status, batteryLevel = robot.batteryLevel,
                                    currentNodeName = robot.currentNodeName, targetNodeName = robot.targetNodeName)
                                false
                            }
                        )
                    }
                }
            }

            // ── Detail Panel (overlay) ──
            selectedItem?.let { item ->
                SketchCard(
                    modifier = Modifier
                        .align(Alignment.BottomCenter)
                        .padding(12.dp)
                        .fillMaxWidth(),
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.Top,
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                Box(
                                    modifier = Modifier
                                        .size(36.dp)
                                        .clip(CircleShape)
                                        .background(
                                            if (item.type == "robot") {
                                                when (item.robotStatus) {
                                                    "Idle" -> Color(0xFF22C55E)
                                                    "Delivering" -> Color(0xFFF59E0B)
                                                    "Charging" -> Color(0xFF3B82F6)
                                                    else -> Color(0xFFEF4444)
                                                }
                                            } else {
                                                when (item.nodeType) {
                                                    "ChargingStation" -> Color(0xFF10B981)
                                                    "Depot" -> Color(0xFF3B82F6)
                                                    else -> Color(0xFF8B5CF6)
                                                }
                                            }
                                        ),
                                    contentAlignment = Alignment.Center,
                                ) {
                                    Icon(
                                        if (item.type == "robot") Icons.Outlined.SmartToy else Icons.Outlined.LocationOn,
                                        null,
                                        tint = Color.White,
                                        modifier = Modifier.size(20.dp),
                                    )
                                }
                                Spacer(Modifier.width(12.dp))
                                Column {
                                    Text(item.name, fontWeight = FontWeight.Bold, style = MaterialTheme.typography.titleMedium, color = PrimaryContainer)
                                    Text(item.subtitle, style = MaterialTheme.typography.bodySmall, color = OnSurfaceVariant)
                                }
                            }

                            if (item.type == "robot") {
                                Spacer(Modifier.height(8.dp))
                                if (item.currentNodeName != null) {
                                    Row(verticalAlignment = Alignment.CenterVertically) {
                                        Icon(Icons.Outlined.LocationOn, null, tint = PrimaryContainer, modifier = Modifier.size(14.dp))
                                        Spacer(Modifier.width(4.dp))
                                        Text("At: ${item.currentNodeName}", style = MaterialTheme.typography.bodySmall)
                                    }
                                }
                                if (item.targetNodeName != null) {
                                    Row(verticalAlignment = Alignment.CenterVertically) {
                                        Icon(Icons.Outlined.RocketLaunch, null, tint = PrimaryContainer, modifier = Modifier.size(14.dp))
                                        Spacer(Modifier.width(4.dp))
                                        Text("To: ${item.targetNodeName}", style = MaterialTheme.typography.bodySmall)
                                    }
                                }
                            }
                        }
                        IconButton(onClick = { selectedItem = null }) {
                            Icon(Icons.Outlined.Close, "Close", tint = OnSurfaceVariant)
                        }
                    }
                }
            }

            // ── Stats badge ──
            Row(
                modifier = Modifier
                    .align(Alignment.TopStart)
                    .padding(12.dp)
                    .rotate(-2f)
                    .border(androidx.compose.foundation.BorderStroke(1.dp, PrimaryContainer), SketchShapeThin)
                    .background(TertiaryFixedDim, SketchShapeThin)
                    .padding(horizontal = 8.dp, vertical = 4.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    imageVector = Icons.Outlined.SmartToy,
                    contentDescription = null,
                    tint = PrimaryContainer,
                    modifier = Modifier.size(14.dp)
                )
                Spacer(Modifier.width(4.dp))
                Text("${robots.size} Robots", style = MaterialTheme.typography.labelSmall)
                Spacer(Modifier.width(8.dp))
                Icon(
                    imageVector = Icons.Outlined.LocationOn,
                    contentDescription = null,
                    tint = PrimaryContainer,
                    modifier = Modifier.size(14.dp)
                )
                Spacer(Modifier.width(4.dp))
                Text("${nodes.size} Nodes", style = MaterialTheme.typography.labelSmall)
            }
        }

        // ── Fleet Telemetry (bottom list) ──
        if (robots.isNotEmpty()) {
            Text(
                "Fleet Telemetry Feed",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
                color = PrimaryContainer,
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp).rotate(-0.5f),
            )
            LazyRow(
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 0.dp),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
                modifier = Modifier.height(120.dp),
            ) {
                items(robots) { robot ->
                    SketchCard(
                        modifier = Modifier.width(160.dp),
                        showShadow = false,
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Icon(Icons.Outlined.SmartToy, null, tint = PrimaryContainer, modifier = Modifier.size(20.dp))
                            Spacer(Modifier.width(6.dp))
                            Text(robot.name, style = MaterialTheme.typography.labelMedium, fontWeight = FontWeight.Bold, maxLines = 1)
                        }
                        Spacer(Modifier.height(4.dp))
                        SketchBadge(text = robot.statusName.ifEmpty { robot.status }, rotationDegrees = 2f)
                        Spacer(Modifier.height(4.dp))
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Icon(Icons.Outlined.BatteryFull, null, tint = PrimaryContainer, modifier = Modifier.size(14.dp))
                            Spacer(Modifier.width(4.dp))
                            Text("${robot.batteryLevel.toInt()}%", style = MaterialTheme.typography.bodySmall)
                        }
                        if (robot.currentNodeName != null) {
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                Icon(Icons.Outlined.LocationOn, null, tint = PrimaryContainer, modifier = Modifier.size(14.dp))
                                Spacer(Modifier.width(4.dp))
                                Text(robot.currentNodeName, style = MaterialTheme.typography.bodySmall, maxLines = 1)
                            }
                        }
                    }
                }
            }
        }

        Spacer(Modifier.height(80.dp))
    }
}

// ── Helper data class for selected item ──
private data class SelectedMapItem(
    val type: String, // "robot" | "node"
    val name: String,
    val subtitle: String,
    val latitude: Double,
    val longitude: Double,
    val nodeType: String? = null,
    val robotStatus: String? = null,
    val batteryLevel: Double? = null,
    val currentNodeName: String? = null,
    val targetNodeName: String? = null,
)

// ── Get user location helper ──
@Suppress("MissingPermission")
private fun getUserLocation(context: android.content.Context, onLocation: (LatLng) -> Unit) {
    val fusedLocationClient = LocationServices.getFusedLocationProviderClient(context)
    fusedLocationClient.lastLocation.addOnSuccessListener { location ->
        if (location != null) {
            onLocation(LatLng(location.latitude, location.longitude))
        }
    }
}
