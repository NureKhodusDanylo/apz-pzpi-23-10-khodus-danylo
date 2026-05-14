import axios from 'axios';
import type {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  CompleteGoogleRegRequest,
  User,
  Order,
  CreateOrderRequest,
  UpdateOrderStatusRequest,
  Robot,
  Node,
  AdminStats,
  RobotEfficiency,
  MapData,
  Friend,
  FriendRequest,
  SendFriendRequest,
} from '../types';

export const BASE_URL = (import.meta.env.VITE_API_URL || 'https://danildrotik-rob.hf.space').replace(/\/$/, '');
const API_BASE_URL = `${BASE_URL}/api`;

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
    'Bypass-Tunnel-Reminder': 'true',
  },
});

api.interceptors.request.use((config) => {
  config.headers['Bypass-Tunnel-Reminder'] = 'true';
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Auth APIs
export const authAPI = {
  login: (data: LoginRequest) => {
    const payload: any = { googleJwtToken: data.googleJwtToken };
    if (data.email) payload.email = data.email;
    if (data.password) payload.password = data.password;
    return api.post<AuthResponse>('/Auth/login', payload);
  },
  register: (data: RegisterRequest) => {
    const payload: any = { ...data };
    // Remove empty fields that might trigger backend validation unnecessarily
    if (!payload.password) delete payload.password;
    if (!payload.email) delete payload.email;
    if (!payload.userName) delete payload.userName;
    return api.post<AuthResponse>('/Auth/register', payload);
  },

  completeGoogleRegistration: (data: CompleteGoogleRegRequest) =>
    api.post<AuthResponse>('/Auth/complete-google-registration', data),

  getCurrentUser: () =>
    api.get<User>('/User/profile'),

  updateProfile: (formData: FormData) =>
    api.put<User>('/User/profile', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }),
};

// User APIs
export const userAPI = {
  getAllUsers: () =>
    api.get<User[]>('/User'),

  searchUsers: (query: string) =>
    api.get<User[]>(`/User/search?query=${query}`),

  getUserById: (id: number) =>
    api.get<User>(`/User/${id}`),
};

// Order APIs
export const orderAPI = {
  // Admin only - get all orders
  getAllOrders: () =>
    api.get<Order[]>('/Order'),

  // Admin only - get order by ID
  getOrderById: (id: number) =>
    api.get<Order>(`/Order/${id}`),

  // Get current user's orders (sent and received)
  getMyOrders: () =>
    api.get<Order[]>('/Order/my-orders'),

  createOrder: (data: CreateOrderRequest) => {
    const formData = new FormData();
    formData.append('name', data.name);
    formData.append('description', data.description);
    formData.append('weight', data.weight.toString());
    formData.append('productPrice', data.productPrice.toString());
    formData.append('isProductPaid', data.isProductPaid.toString());
    formData.append('recipientId', data.recipientId.toString());
    formData.append('deliveryPayer', (data.deliveryPayer || 0).toString());

    if (data.files && data.files.length > 0) {
      data.files.forEach((file) => {
        formData.append('files', file);
      });
    }

    return api.post<Order>('/Order', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  updateOrderStatus: (id: number, data: UpdateOrderStatusRequest) =>
    api.put<Order>(`/Order/${id}/status`, data),

  assignRobot: (orderId: number, robotId: number) =>
    api.post<Order>(`/Order/${orderId}/assign/${robotId}`),

  cancelOrder: (id: number) =>
    api.post<Order>(`/Order/${id}/cancel`),

  executeOrder: (id: number) =>
    api.post(`/Order/${id}/execute`),

  deleteOrder: (id: number) =>
    api.delete(`/Order/${id}`),

  estimateDeliveryPrice: (weight: number) =>
    api.get<{deliveryPrice: number}>(`/Order/estimate-price?weight=${weight}`),
};

// Payment APIs
export const paymentAPI = {
  processPayment: (data: any) =>
    api.post('/Payments/process', data),

  payOrder: (data: { orderId: number, payProduct: boolean, payDelivery: boolean, paymentMethod: string, stripeCardToken?: string }) =>
    api.post<{success: boolean, message: string}>('/Payments/pay-order', data),
};

// Wallet APIs
export const walletAPI = {
  getBalance: () =>
    api.get<{success: boolean, balance: number}>('/Wallet/balance'),
  withdraw: () =>
    api.post<{success: boolean, message: string, newBalance: number, withdrawnAmount: number}>('/Wallet/withdraw'),
};

// Robot APIs
export const robotAPI = {
  getAllRobots: () =>
    api.get<Robot[]>('/Robot'),

  getRobotById: (id: number) =>
    api.get<Robot>(`/Robot/${id}`),

  getAvailableRobots: () =>
    api.get<Robot[]>('/Robot/available'),

  getRobotsByStatus: (status: string) =>
    api.get<Robot[]>(`/Robot/status/${status}`),

  getRobotsByType: (type: string) =>
    api.get<Robot[]>(`/Robot/type/${type}`),

  createRobot: (data: Partial<Robot>) =>
    api.post<Robot>('/Robot', data),

  updateRobot: (id: number, data: Partial<Robot>) =>
    api.put<Robot>(`/Robot/${id}`, data),

  deleteRobot: (id: number) =>
    api.delete(`/Robot/${id}`),
  
  testRouting: (data: { robotId: number, pickupNodeId: number, dropoffNodeId: number, packageWeight: number }) =>
    api.post<any>('/Robot/test-routing', data),
};

// Node APIs
export const nodeAPI = {
  getAllNodes: () =>
    api.get<Node[]>('/Node'),

  getNodeById: (id: number) =>
    api.get<Node>(`/Node/${id}`),

  getNodesByType: (type: string) =>
    api.get<Node[]>(`/Node/type/${type}`),

  createNode: (data: Partial<Node>) =>
    api.post<Node>('/Node', data),

  updateNode: (id: number, data: Partial<Node>) =>
    api.put<Node>(`/Node/${id}`, data),

  deleteNode: (id: number) =>
    api.delete(`/Node/${id}`),
};

// Admin APIs
export const adminAPI = {
  getStats: () =>
    api.get<AdminStats>('/Admin/stats'),

  getRobotEfficiency: () =>
    api.get<RobotEfficiency[]>('/Admin/analytics/robot-efficiency'),

  exportDeliveryHistory: () =>
    api.get('/Admin/export/delivery-history', { responseType: 'blob' }),

  createBackup: () =>
    api.post('/Admin/backup'),
};

// Map APIs
export const mapAPI = {
  getMapData: () =>
    api.get<MapData>('/Map/data'),
};

// Friendship APIs
export const friendshipAPI = {
  getFriends: () =>
    api.get<Friend[]>('/Friendship/list'),

  getPendingRequests: () =>
    api.get<FriendRequest[]>('/Friendship/requests/pending'),

  sendRequest: (data: SendFriendRequest) =>
    api.post('/Friendship/request', data),

  acceptRequest: (requestId: number) =>
    api.post(`/Friendship/request/${requestId}/accept`),

  rejectRequest: (requestId: number) =>
    api.post(`/Friendship/request/${requestId}/reject`),

  removeFriend: (friendId: number) =>
    api.delete(`/Friendship/${friendId}`),
};

export default api;
