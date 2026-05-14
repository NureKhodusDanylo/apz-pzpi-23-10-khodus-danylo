package com.example.robandroid.data.local

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.example.robandroid.data.model.User
import com.google.gson.Gson
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

private val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "auth_storage")

class TokenManager(private val context: Context) {

    companion object {
        private val TOKEN_KEY = stringPreferencesKey("jwt_token")
        private val USER_KEY = stringPreferencesKey("user_json")
        private val gson = Gson()
    }

    val token: Flow<String?> = context.dataStore.data.map { prefs ->
        prefs[TOKEN_KEY]
    }

    val user: Flow<User?> = context.dataStore.data.map { prefs ->
        prefs[USER_KEY]?.let { json ->
            try {
                gson.fromJson(json, User::class.java)
            } catch (e: Exception) {
                null
            }
        }
    }

    suspend fun saveAuth(token: String, user: User) {
        context.dataStore.edit { prefs ->
            prefs[TOKEN_KEY] = token
            prefs[USER_KEY] = gson.toJson(user)
        }
    }

    suspend fun updateUser(user: User) {
        context.dataStore.edit { prefs ->
            prefs[USER_KEY] = gson.toJson(user)
        }
    }

    suspend fun clearAuth() {
        context.dataStore.edit { prefs ->
            prefs.remove(TOKEN_KEY)
            prefs.remove(USER_KEY)
        }
    }
}
