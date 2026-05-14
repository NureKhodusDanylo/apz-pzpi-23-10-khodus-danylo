package com.example.robandroid.ui.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CutCornerShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import coil.compose.AsyncImage
import com.example.robandroid.ui.theme.*

// ── Sketchy border shape (irregular cut corners mimic hand-drawn) ──
val SketchShape = CutCornerShape(
    topStart = 4.dp, topEnd = 8.dp,
    bottomEnd = 3.dp, bottomStart = 6.dp
)

val SketchShapeThin = CutCornerShape(
    topStart = 3.dp, topEnd = 5.dp,
    bottomEnd = 2.dp, bottomStart = 4.dp
)

// ═══════════════════════ SketchCard ═══════════════════════
@Composable
fun SketchCard(
    modifier: Modifier = Modifier,
    rotationDegrees: Float = 0f,
    showShadow: Boolean = true,
    onClick: (() -> Unit)? = null,
    content: @Composable ColumnScope.() -> Unit
) {
    val shadowMod = if (showShadow) {
        Modifier.offset(x = 3.dp, y = 4.dp)
    } else Modifier

    Box(modifier = modifier) {
        // Shadow layer
        if (showShadow) {
            Box(
                modifier = Modifier
                    .matchParentSize()
                    .offset(x = 3.dp, y = 4.dp)
                    .background(PrimaryContainer, SketchShape)
            )
        }

        // Main card
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .rotate(rotationDegrees)
                .border(BorderStroke(2.dp, PrimaryContainer), SketchShape)
                .background(SurfaceContainer, SketchShape)
                .clip(SketchShape)
                .then(if (onClick != null) Modifier.clickable { onClick() } else Modifier)
                .padding(16.dp),
            content = content
        )
    }
}

// ═══════════════════════ SketchButton ═══════════════════════
enum class SketchButtonVariant { Primary, Secondary, Error }

@Composable
fun SketchButton(
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    variant: SketchButtonVariant = SketchButtonVariant.Primary,
    icon: ImageVector? = null,
    enabled: Boolean = true,
    isLoading: Boolean = false,
    content: @Composable RowScope.() -> Unit
) {
    val (bgColor, textColor, borderColor) = when (variant) {
        SketchButtonVariant.Primary -> Triple(PrimaryContainer, OnPrimaryContainer, PrimaryContainer)
        SketchButtonVariant.Secondary -> Triple(Surface, PrimaryContainer, PrimaryContainer)
        SketchButtonVariant.Error -> Triple(ErrorContainer, OnErrorContainer, Error)
    }

    Button(
        onClick = onClick,
        enabled = enabled && !isLoading,
        shape = SketchShape,
        colors = ButtonDefaults.buttonColors(
            containerColor = bgColor,
            contentColor = textColor,
            disabledContainerColor = bgColor.copy(alpha = 0.5f),
            disabledContentColor = textColor.copy(alpha = 0.5f),
        ),
        border = BorderStroke(2.dp, borderColor),
        contentPadding = PaddingValues(horizontal = 24.dp, vertical = 12.dp),
        modifier = modifier
    ) {
        if (isLoading) {
            CircularProgressIndicator(
                modifier = Modifier.size(18.dp),
                color = textColor,
                strokeWidth = 2.dp
            )
            Spacer(Modifier.width(8.dp))
        } else if (icon != null) {
            Icon(icon, contentDescription = null, modifier = Modifier.size(20.dp))
            Spacer(Modifier.width(8.dp))
        }
        content()
    }
}

// ═══════════════════════ SketchTextField ═══════════════════════
@Composable
fun SketchTextField(
    value: String,
    onValueChange: (String) -> Unit,
    modifier: Modifier = Modifier,
    label: String? = null,
    placeholder: String = "",
    isPassword: Boolean = false,
    keyboardType: KeyboardType = KeyboardType.Text,
    enabled: Boolean = true,
    readOnly: Boolean = false,
    singleLine: Boolean = true,
    error: String? = null,
    minLines: Int = 1,
) {
    Column(modifier = modifier.fillMaxWidth()) {
        if (label != null) {
            Text(
                text = label,
                style = MaterialTheme.typography.labelMedium,
                color = PrimaryContainer.copy(alpha = 0.7f),
                modifier = Modifier.padding(bottom = 4.dp)
            )
        }

        OutlinedTextField(
            value = value,
            onValueChange = onValueChange,
            placeholder = {
                Text(
                    placeholder,
                    style = MaterialTheme.typography.bodyMedium,
                    color = OnSurfaceVariant.copy(alpha = 0.5f)
                )
            },
            visualTransformation = if (isPassword) PasswordVisualTransformation() else VisualTransformation.None,
            keyboardOptions = KeyboardOptions(keyboardType = keyboardType),
            enabled = enabled,
            readOnly = readOnly,
            singleLine = singleLine,
            minLines = minLines,
            isError = error != null,
            shape = SketchShapeThin,
            colors = OutlinedTextFieldDefaults.colors(
                focusedBorderColor = PrimaryContainer,
                unfocusedBorderColor = PrimaryContainer.copy(alpha = 0.3f),
                focusedContainerColor = Surface,
                unfocusedContainerColor = Surface,
                cursorColor = PrimaryContainer,
            ),
            modifier = Modifier.fillMaxWidth()
        )

        if (error != null) {
            Text(
                text = error,
                style = MaterialTheme.typography.labelSmall,
                color = Error,
                modifier = Modifier.padding(start = 4.dp, top = 2.dp)
            )
        }
    }
}

// ═══════════════════════ SketchDivider ═══════════════════════
@Composable
fun SketchDivider(
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .fillMaxWidth()
            .height(2.dp)
            .background(
                brush = Brush.horizontalGradient(
                    colors = listOf(
                        Color.Transparent,
                        PrimaryContainer,
                        PrimaryContainer,
                        Color.Transparent
                    ),
                    startX = 0f,
                    endX = Float.POSITIVE_INFINITY
                )
            )
            .padding(vertical = 16.dp)
    )
}

// ═══════════════════════ SketchAvatar ═══════════════════════
@Composable
fun SketchAvatar(
    imageUrl: String?,
    modifier: Modifier = Modifier,
    size: Dp = 80.dp,
    rotationDegrees: Float = 0f,
    contentDescription: String? = null,
) {
    val context = androidx.compose.ui.platform.LocalContext.current
    
    // Use an image loader that shares our authenticated OkHttpClient
    val imageLoader = remember(context) {
        coil.ImageLoader.Builder(context)
            .okHttpClient(com.example.robandroid.data.remote.RetrofitClient.okHttpClient)
            .build()
    }

    // Centralized URL normalization logic
    val finalUrl = remember(imageUrl) {
        if (imageUrl.isNullOrBlank()) null
        else {
            val normalized = imageUrl.replace('\\', '/').trimStart('/')
            if (normalized.startsWith("http")) normalized
            else "${com.example.robandroid.data.remote.RetrofitClient.BASE_URL}/$normalized"
        }
    }

    Box(
        modifier = modifier
            .size(size)
            .rotate(rotationDegrees)
            .border(BorderStroke(2.dp, PrimaryContainer), SketchShape)
            .background(SurfaceVariant, SketchShape)
            .clip(SketchShape),
        contentAlignment = Alignment.Center
    ) {
        if (!finalUrl.isNullOrEmpty()) {
            coil.compose.AsyncImage(
                model = finalUrl,
                imageLoader = imageLoader,
                contentDescription = contentDescription,
                contentScale = ContentScale.Crop,
                modifier = Modifier.fillMaxSize()
            )
        } else {
            // Fallback: show first letter or icon
            Text(
                text = "?",
                style = MaterialTheme.typography.headlineMedium,
                color = PrimaryContainer
            )
        }
    }
}

// ═══════════════════════ SketchBadge ═══════════════════════
@Composable
fun SketchBadge(
    text: String,
    modifier: Modifier = Modifier,
    backgroundColor: Color = TertiaryFixedDim,
    textColor: Color = TertiaryContainer,
    rotationDegrees: Float = 6f,
) {
    Text(
        text = text,
        style = MaterialTheme.typography.labelSmall,
        color = textColor,
        modifier = modifier
            .rotate(rotationDegrees)
            .border(BorderStroke(1.dp, PrimaryContainer), SketchShapeThin)
            .background(backgroundColor, SketchShapeThin)
            .padding(horizontal = 8.dp, vertical = 4.dp)
    )
}
