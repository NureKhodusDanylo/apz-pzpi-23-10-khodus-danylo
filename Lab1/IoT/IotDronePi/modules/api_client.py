import urequests
import ujson
import sys

sys.path.append('/config')
sys.path.append('/utils')

from config import API_CONFIG, API_HEADERS, DEBUG
from helpers import log_message, resolve_dns

class ApiClient:
    """
    Unified API Client for Robot IoT System.
    Handles DNS resolution, protocols, headers, and authentication.
    """
    _instance = None

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super(ApiClient, cls).__new__(cls)
            cls._instance._initialized = False
        return cls._instance

    def __init__(self):
        if self._initialized:
            return
        
        self.domain = API_CONFIG["DOMAIN"]
        self.base_url = API_CONFIG["BASE_URL"]
        self.protocol = "https" if self.base_url.startswith("https") else "http"
        self.resolved_ip = None
        self.token = None
        self.timeout = API_CONFIG.get("REQUEST_TIMEOUT", 10)
        self._initialized = True

    def set_token(self, token):
        """Set JWT token for authentication"""
        self.token = token

    def _ensure_resolved(self):
        """Ensure domain is resolved to IP"""
        if not self.resolved_ip:
            self.resolved_ip = resolve_dns(self.domain)
        return self.resolved_ip is not None

    def _get_headers(self, custom_headers=None):
        """Prepare standard headers with auth"""
        headers = API_HEADERS.copy()
        headers["Host"] = self.domain
        
        if self.token:
            headers["Authorization"] = "Bearer {}".format(self.token)
        
        if custom_headers:
            headers.update(custom_headers)
        
        return headers

    def _request(self, method, endpoint, json=None, headers=None):
        """Base request method"""
        if not self._ensure_resolved():
            log_message("Request failed: DNS resolution error", "ERROR")
            return None

        # Clean endpoint to ensure it starts with /
        if not endpoint.startswith("/"):
            endpoint = "/" + endpoint

        url = "{}://{}{}".format(self.protocol, self.resolved_ip, endpoint)
        all_headers = self._get_headers(headers)

        if DEBUG:
            log_message("{} -> {}".format(method, endpoint), "DEBUG")

        try:
            if method == "GET":
                response = urequests.get(url, headers=all_headers, timeout=self.timeout)
            elif method == "POST":
                response = urequests.post(url, json=json, headers=all_headers, timeout=self.timeout)
            elif method == "PUT":
                response = urequests.put(url, json=json, headers=all_headers, timeout=self.timeout)
            elif method == "DELETE":
                response = urequests.delete(url, headers=all_headers, timeout=self.timeout)
            else:
                log_message("Unsupported method: " + method, "ERROR")
                return None

            return response
        except Exception as e:
            log_message("API Error ({} {}): {}".format(method, endpoint, str(e)), "ERROR")
            return None

    def get(self, endpoint, headers=None):
        return self._request("GET", endpoint, headers=headers)

    def post(self, endpoint, json=None, headers=None):
        return self._request("POST", endpoint, json=json, headers=headers)

    def put(self, endpoint, json=None, headers=None):
        return self._request("PUT", endpoint, json=json, headers=headers)

    def delete(self, endpoint, headers=None):
        return self._request("DELETE", endpoint, headers=headers)
