import React from 'react';
import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import Layout from '../components/Layout';
import { orderAPI, paymentAPI } from '../lib/api';
import { useAuthStore } from '../store/authStore';
import type { Order } from '../types';
import { SketchCard, SketchButton, SketchDivider } from '../components/common/SketchComponents';
import { StripePaymentModal } from '../components/common/StripePaymentModal';
import { DiagnosticModal } from '../components/common/DiagnosticModal';

const OrdersPage = () => {
  const user = useAuthStore((state) => state.user);
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<'all' | 'sent' | 'received'>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);

  const [paymentConfig, setPaymentConfig] = useState<{
    isOpen: boolean;
    orderId: number;
    payProduct: boolean;
    payDelivery: boolean;
    amountText?: string;
  } | null>(null);

  const [diagnosticLogs, setDiagnosticLogs] = useState<string[] | null>(null);

  const { data: myOrders, isLoading } = useQuery({
    queryKey: ['orders', 'my-orders'],
    queryFn: async () => {
      const response = await orderAPI.getMyOrders();
      return response.data;
    },
  });

  const sentOrders = myOrders?.filter((order) => order.senderId === user?.id) || [];
  const receivedOrders = myOrders?.filter((order) => order.recipientId === user?.id) || [];

  const cancelOrderMutation = useMutation({
    mutationFn: orderAPI.cancelOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      setSelectedOrder(null);
      alert('Order cancelled successfully');
    },
    onError: (error: any) => {
      alert(error.response?.data?.error || 'Failed to cancel order');
    },
  });

  const executeOrderMutation = useMutation({
    mutationFn: orderAPI.executeOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      setSelectedOrder(null);
      alert('Order executed successfully! Robot assigned and en route.');
    },
    onError: (error: any) => {
      const logs = error.response?.data?.debugLogs;
      if (logs && Array.isArray(logs)) {
        setDiagnosticLogs(logs);
      } else {
        alert(error.response?.data?.error || 'Failed to execute order');
      }
    },
  });

  const payOrderMutation = useMutation({
    mutationFn: paymentAPI.payOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      setSelectedOrder(null);
      setPaymentConfig(null);
      alert('Payment successful!');
    },
    onError: (error: any) => {
      alert(error.response?.data?.error || 'Failed to process payment');
      setPaymentConfig(null);
    },
  });

  const handlePaymentSuccess = (paymentMethodId: string) => {
    if (paymentConfig) {
      payOrderMutation.mutate({
        orderId: paymentConfig.orderId,
        payProduct: paymentConfig.payProduct,
        payDelivery: paymentConfig.payDelivery,
        paymentMethod: 'stripe',
        stripeCardToken: paymentMethodId
      });
    }
  };

  const getFilteredOrders = () => {
    let orders: Order[] = [];

    if (filter === 'all') {
      orders = [...(sentOrders || []), ...(receivedOrders || [])];
    } else if (filter === 'sent') {
      orders = sentOrders || [];
    } else {
      orders = receivedOrders || [];
    }

    if (statusFilter !== 'all') {
      orders = orders.filter((o) => o.status === statusFilter);
    }

    return orders.sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
  };

  const filteredOrders = getFilteredOrders();

  return (
    <Layout>
      <div className="flex flex-col gap-8">
        <section className="bg-surface-container p-6 sketch-border sketch-shadow relative transform rotate-1">
          <h1 className="font-headline-lg text-headline-lg mb-2">My Shipments</h1>
          <p className="font-body-lg text-on-surface-variant italic">Track and manage your robotic deliveries across the grid.</p>
        </section>

        {/* Filters Bar */}
        <div className="flex flex-wrap items-center justify-between gap-6 p-4 sketch-border bg-surface-container-low">
          <div className="flex flex-wrap gap-3">
            {[
              { id: 'all', label: 'All Shipments', icon: 'package_2' },
              { id: 'sent', label: 'Sent', icon: 'outbox' },
              { id: 'received', label: 'Received', icon: 'move_to_inbox' }
            ].map((f) => (
              <button
                key={f.id}
                onClick={() => setFilter(f.id as any)}
                className={`flex items-center gap-2 px-4 py-2 sketch-border-thin transition-all font-label-md text-sm ${
                  filter === f.id ? 'bg-primary-container text-surface rotate-1' : 'bg-surface text-primary-container hover:bg-surface-variant'
                }`}
              >
                <span className="material-symbols-outlined text-sm">{f.icon}</span>
                {f.label}
              </button>
            ))}
          </div>

          <select
            className="p-2 bg-surface sketch-border-thin font-label-md text-sm focus:outline-none focus:sketch-border"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="all">All Status</option>
            <option value="AwaitingPayment">Awaiting Payment</option>
            <option value="AwaitingConfirmation">Awaiting Confirmation</option>
            <option value="Pending">Pending</option>
            <option value="Processing">Processing</option>
            <option value="EnRoute">En Route</option>
            <option value="Delivered">Delivered</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        {isLoading ? (
           <div className="text-center py-24">
              <span className="material-symbols-outlined animate-spin text-4xl">sync</span>
              <p className="mt-4 font-label-md">Fetching ledger data...</p>
           </div>
        ) : filteredOrders.length === 0 ? (
          <SketchCard className="text-center py-20 bg-surface-container-low" rotate>
            <span className="material-symbols-outlined text-6xl opacity-20 mb-4">search_off</span>
            <h3 className="font-headline-md text-xl">No shipments found</h3>
            <p className="font-body-md text-on-surface-variant">Try adjusting your filters or launch a new robot.</p>
          </SketchCard>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-8">
            {filteredOrders.map((order, index) => (
              <SketchCard 
                key={order.id} 
                rotate={index % 2 === 1}
                className="group cursor-pointer hover:bg-surface-container transition-colors"
              >
                <div onClick={() => setSelectedOrder(order)}>
                  <div className="flex justify-between items-start mb-6">
                    <div className="bg-secondary-fixed text-on-secondary-fixed px-3 py-1 sketch-border-thin flex items-center gap-2 text-[10px] font-black uppercase">
                      <span className="material-symbols-outlined text-sm">
                        {order.senderId === user?.id ? 'outbox' : 'move_to_inbox'}
                      </span>
                      {order.senderId === user?.id ? 'Sent' : 'Received'}
                    </div>
                    <div className="bg-tertiary-fixed-dim text-tertiary-container px-2 py-1 sketch-border-thin text-[10px] font-black transform rotate-6">
                      {order.status}
                    </div>
                  </div>

                  <h3 className="font-headline-md text-xl font-bold mb-2">{order.name}</h3>
                  <p className="font-body-md text-sm text-on-surface-variant mb-6 line-clamp-2 italic">"{order.description}"</p>

                  <div className="flex flex-col gap-3 mb-6">
                    <div className="flex items-center gap-2 text-xs font-label-md">
                      <span className="material-symbols-outlined text-sm">person</span>
                      {order.senderId === user?.id ? order.recipientName : order.senderName}
                    </div>
                    <div className="flex items-center gap-2 text-xs font-label-md">
                      <span className="material-symbols-outlined text-sm">calendar_month</span>
                      {new Date(order.createdAt).toLocaleDateString()}
                    </div>
                  </div>

                  <SketchDivider />

                  <div className="flex justify-between items-center mt-4">
                    <div className="flex flex-col">
                      <div className="flex items-center gap-1 font-black text-lg">
                        <span className="material-symbols-outlined text-sm">payments</span>
                        ${
                          order.senderId === user?.id 
                            ? (order.deliveryPayerName === 'Sender' ? order.deliveryPrice : 0).toFixed(2)
                            : (order.productPrice + (order.deliveryPayerName === 'Recipient' ? order.deliveryPrice : 0)).toFixed(2)
                        } Your Cost
                      </div>
                      <div className="text-xs font-label-md text-on-surface-variant">
                        Order Value: ${(order.productPrice + order.deliveryPrice).toFixed(2)} (Product: ${order.productPrice.toFixed(2)} | Delivery: ${order.deliveryPrice.toFixed(2)})
                      </div>
                    </div>
                    {order.robotName && (
                      <div className="flex items-center gap-1 text-[10px] bg-surface-variant px-2 py-1 sketch-border-thin">
                        <span className="material-symbols-outlined text-xs">smart_toy</span>
                        {order.robotName}
                      </div>
                    )}
                  </div>
                </div>

                {/* Sender paying for Delivery indicator */}
                {order.senderId === user?.id && order.status === 'AwaitingPayment' && (
                  <div className="mt-4 p-3 bg-error-container text-on-error-container sketch-border-thin transform -rotate-1">
                    <p className="font-sketch-bold text-sm">Pay ${order.deliveryPrice.toFixed(2)} for delivery to proceed.</p>
                  </div>
                )}

                {/* Recipient paying indicator */}
                {order.recipientId === user?.id && order.status === 'AwaitingConfirmation' && (
                  <div className="mt-4 p-3 bg-error-container text-on-error-container sketch-border-thin transform rotate-1">
                    <p className="font-sketch-bold text-sm">
                      Pay ${(order.productPrice + (order.deliveryPayerName === 'Recipient' ? order.deliveryPrice : 0)).toFixed(2)} 
                      {' '}({order.deliveryPayerName === 'Recipient' ? 'Product + Delivery' : 'Product only'}) to receive this order.
                    </p>
                  </div>
                )}

                {order.senderId === user?.id && order.status === 'Pending' && (
                  <div className="flex gap-4 mt-6">
                    <SketchButton
                      className="flex-1 py-2 text-xs"
                      icon={executeOrderMutation.isPending ? 'sync' : 'play_arrow'}
                      onClick={() => {
                        if (window.confirm('Execute this order? Optimal robot assignment will begin.')) {
                          executeOrderMutation.mutate(order.id);
                        }
                      }}
                      disabled={executeOrderMutation.isPending}
                    >
                      Execute
                    </SketchButton>
                    <SketchButton
                      variant="error"
                      className="flex-1 py-2 text-xs"
                      onClick={() => {
                        if (window.confirm('Abort this mission?')) {
                          cancelOrderMutation.mutate(order.id);
                        }
                      }}
                      disabled={cancelOrderMutation.isPending}
                    >
                      Cancel
                    </SketchButton>
                  </div>
                )}

                {order.senderId === user?.id && order.status === 'AwaitingPayment' && (
                  <div className="flex gap-4 mt-6">
                    <SketchButton
                      className="flex-1 py-2 text-xs bg-secondary text-on-secondary"
                      icon={payOrderMutation.isPending ? 'sync' : 'payments'}
                      onClick={(e) => {
                        e.stopPropagation();
                        setPaymentConfig({
                          isOpen: true,
                          orderId: order.id,
                          payProduct: false,
                          payDelivery: true,
                          amountText: `$${order.deliveryPrice.toFixed(2)} (Delivery)`
                        });
                      }}
                      disabled={payOrderMutation.isPending}
                    >
                      Pay Delivery
                    </SketchButton>
                  </div>
                )}

                {order.recipientId === user?.id && order.status === 'AwaitingConfirmation' && (
                  <div className="flex gap-4 mt-6">
                    <SketchButton
                      className="flex-1 py-2 text-xs bg-secondary text-on-secondary"
                      icon={payOrderMutation.isPending ? 'sync' : 'task_alt'}
                      onClick={(e) => {
                        e.stopPropagation();
                        const amount = order.productPrice + (order.deliveryPayer === 'Recipient' ? order.deliveryPrice : 0);
                        const breakdown = order.deliveryPayer === 'Recipient' ? 'Product + Delivery' : 'Product';
                        setPaymentConfig({
                          isOpen: true,
                          orderId: order.id,
                          payProduct: true,
                          payDelivery: order.deliveryPayerName === 'Recipient',
                          amountText: `$${amount.toFixed(2)} (${breakdown})`
                        });
                      }}
                      disabled={payOrderMutation.isPending}
                    >
                      Confirm & Pay
                    </SketchButton>
                  </div>
                )}
              </SketchCard>
            ))}
          </div>
        )}

        <AnimatePresence>
          {selectedOrder && (
            <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-primary-container/20 backdrop-blur-sm" onClick={() => setSelectedOrder(null)}>
              <motion.div
                initial={{ scale: 0.9, opacity: 0, rotate: -2 }}
                animate={{ scale: 1, opacity: 1, rotate: 0 }}
                exit={{ scale: 0.9, opacity: 0, rotate: 2 }}
                className="w-full max-w-2xl"
                onClick={(e) => e.stopPropagation()}
              >
                <SketchCard shadow rotate className="bg-surface relative">
                   <button 
                    className="absolute top-4 right-4 w-10 h-10 sketch-border-thin hover:rotate-12 transition-transform flex items-center justify-center bg-surface"
                    onClick={() => setSelectedOrder(null)}
                  >
                    <span className="material-symbols-outlined">close</span>
                  </button>

                  <h2 className="font-headline-md text-2xl mb-8">Shipment Details</h2>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                    <div className="space-y-4">
                      <h4 className="font-label-md font-black uppercase text-xs tracking-widest text-primary-container/40">Manifest</h4>
                      <div className="flex justify-between border-b-2 border-primary-container/5 pb-2">
                        <span className="font-body-md text-sm">Item Name</span>
                        <span className="font-bold">{selectedOrder.name}</span>
                      </div>
                      <div className="flex justify-between border-b-2 border-primary-container/5 pb-2">
                        <span className="font-body-md text-sm">Description</span>
                        <span className="font-bold text-right">{selectedOrder.description}</span>
                      </div>
                      <div className="flex justify-between border-b-2 border-primary-container/5 pb-2">
                        <span className="font-body-md text-sm">Weight</span>
                        <span className="font-bold">{selectedOrder.weight} kg</span>
                      </div>
                    </div>

                    <div className="space-y-4">
                      <h4 className="font-label-md font-black uppercase text-xs tracking-widest text-primary-container/40">Route</h4>
                      <div className="flex justify-between border-b-2 border-primary-container/5 pb-2">
                        <span className="font-body-md text-sm">Origin</span>
                        <span className="font-bold">{selectedOrder.pickupNodeName}</span>
                      </div>
                      <div className="flex justify-between border-b-2 border-primary-container/5 pb-2">
                        <span className="font-body-md text-sm">Destination</span>
                        <span className="font-bold">{selectedOrder.dropoffNodeName}</span>
                      </div>
                       <div className="flex justify-between border-b-2 border-primary-container/5 pb-2">
                        <span className="font-body-md text-sm">Recipient</span>
                        <span className="font-bold">{selectedOrder.recipientName}</span>
                      </div>
                    </div>
                  </div>

                  <SketchDivider className="my-8" />

                  <div className="flex flex-col md:flex-row justify-between items-start md:items-end gap-6">
                    <div className="flex flex-col gap-2">
                       <h4 className="font-label-md font-black uppercase text-[10px] text-primary-container/40 tracking-widest">Pricing Breakdown</h4>
                       <div className="grid grid-cols-2 gap-x-8 gap-y-1 text-sm font-label-md">
                          <span className="text-on-surface-variant">Product Price:</span>
                          <span className="font-bold text-right">${selectedOrder.productPrice?.toFixed(2) || '0.00'}</span>
                          <span className="text-on-surface-variant">Delivery Cost:</span>
                          <span className="font-bold text-right">${selectedOrder.deliveryPrice?.toFixed(2) || '0.00'}</span>
                          <span className="text-on-surface-variant mt-1 font-bold">Total Value:</span>
                          <span className="font-black text-xl mt-1 text-right">${((selectedOrder.productPrice || 0) + (selectedOrder.deliveryPrice || 0)).toFixed(2)}</span>
                       </div>
                    </div>
                    <SketchButton variant="secondary" onClick={() => setSelectedOrder(null)}>Close Manifest</SketchButton>
                  </div>
                </SketchCard>
              </motion.div>
            </div>
          )}
        </AnimatePresence>

        {paymentConfig && (
          <StripePaymentModal
            isOpen={paymentConfig.isOpen}
            onClose={() => setPaymentConfig(null)}
            onSuccess={handlePaymentSuccess}
            isLoading={payOrderMutation.isPending}
            amountText={paymentConfig.amountText}
          />
        )}

        <DiagnosticModal
          isOpen={!!diagnosticLogs}
          onClose={() => setDiagnosticLogs(null)}
          logs={diagnosticLogs || []}
        />
      </div>
    </Layout>
  );
};

export default OrdersPage;
