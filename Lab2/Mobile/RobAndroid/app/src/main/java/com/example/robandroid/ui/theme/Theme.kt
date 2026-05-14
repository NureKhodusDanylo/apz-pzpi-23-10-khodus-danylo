package com.example.robandroid.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val SketchyColorScheme = lightColorScheme(
    primary = PrimaryContainer,
    onPrimary = OnPrimaryContainer,
    primaryContainer = PrimaryContainer,
    onPrimaryContainer = OnPrimaryContainer,
    secondary = SecondaryFixed,
    onSecondary = OnSecondaryFixed,
    secondaryContainer = SecondaryFixed,
    onSecondaryContainer = OnSecondaryFixed,
    tertiary = TertiaryFixedDim,
    onTertiary = TertiaryContainer,
    tertiaryContainer = TertiaryContainer,
    onTertiaryContainer = TertiaryFixedDim,
    error = Error,
    onError = OnPrimaryContainer,
    errorContainer = ErrorContainer,
    onErrorContainer = OnErrorContainer,
    surface = Surface,
    onSurface = OnSurface,
    surfaceVariant = SurfaceVariant,
    onSurfaceVariant = OnSurfaceVariant,
    surfaceContainerLow = SurfaceContainerLow,
    surfaceContainer = SurfaceContainer,
    surfaceContainerHigh = SurfaceContainerHigh,
    background = Background,
    onBackground = OnSurface,
    outline = SketchBorder,
    outlineVariant = OutlineVariant,
)

@Composable
fun RobAndroidTheme(
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = SketchyColorScheme,
        typography = Typography,
        content = content
    )
}