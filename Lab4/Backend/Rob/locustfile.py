import time
from locust import HttpUser, task, between

class RobDeliveryUser(HttpUser):
    # Время ожидания между задачами от 1 до 5 секунд
    wait_time = between(1, 5)
    
    token = ""

    def on_start(self):
        """ Вызывается при старте теста для каждого пользователя """
        self.login()

    def login(self):
        # Эндпоинт логина с префиксом api/
        response = self.client.post("/api/Auth/login", json={
            "email": "cafe_555@example.com", 
            "password": "Cafe123!"       
        })
        if response.status_code == 200:
            # В вашем бэкенде токен возвращается в поле "token"
            self.token = response.json().get("token")
        else:
            print(f"Login failed: {response.status_code} - {response.text}")

    @task(3)
    def view_orders(self):
        """ Просмотр своих заказов """
        if not self.token: return
        headers = {"Authorization": f"Bearer {self.token}"}
        self.client.get("/api/Order/my-orders", headers=headers)

    @task(1)
    def view_profile(self):
        """ Просмотр своего профиля """
        if not self.token: return
        headers = {"Authorization": f"Bearer {self.token}"}
        self.client.get("/api/User/profile", headers=headers)

    @task(2)
    def check_available_robots(self):
        """ Просмотр доступных роботов (доступно всем пользователям) """
        if not self.token: return
        headers = {"Authorization": f"Bearer {self.token}"}
        self.client.get("/api/Robot/available", headers=headers)
