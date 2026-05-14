package com.example.robandroid.data.remote

import com.example.robandroid.data.local.TokenManager
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.runBlocking
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object RetrofitClient {

    const val BASE_URL = "https://danildrotik-rob.hf.space"
    private const val API_BASE_URL = "$BASE_URL/api/"

    private var tokenManager: TokenManager? = null

    fun init(tokenManager: TokenManager) {
        this.tokenManager = tokenManager
    }

    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BODY
    }

    private val headerInterceptor = Interceptor { chain ->
        val original = chain.request()
        val requestBuilder = original.newBuilder()
            .header("Bypass-Tunnel-Reminder", "true")

        // Don't override Content-Type for multipart requests
        if (original.body?.contentType()?.type != "multipart") {
            requestBuilder.header("Content-Type", "application/json")
        }

        // Add auth token if available
        tokenManager?.let { tm ->
            val token = runBlocking { tm.token.firstOrNull() }
            if (!token.isNullOrEmpty()) {
                requestBuilder.header("Authorization", "Bearer $token")
            }
        }

        chain.proceed(requestBuilder.build())
    }

    val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(headerInterceptor)
        .addInterceptor(loggingInterceptor)
        .connectTimeout(60, TimeUnit.SECONDS)
        .readTimeout(60, TimeUnit.SECONDS)
        .writeTimeout(60, TimeUnit.SECONDS)
        .build()

    // Lazy init so tokenManager is set before OkHttp is created
    val apiService: ApiService by lazy {
        val retrofit = Retrofit.Builder()
            .baseUrl(API_BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()

        retrofit.create(ApiService::class.java)
    }
}
