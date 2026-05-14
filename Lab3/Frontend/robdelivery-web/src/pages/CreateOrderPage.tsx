import React from 'react';
import { useState } from 'react';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useLocation } from 'react-router-dom';
import Layout from '../components/Layout';
import { orderAPI, userAPI, paymentAPI } from '../lib/api';
import { SketchCard, SketchButton, SketchInput, SketchDivider } from '../components/common/SketchComponents';
import { StripePaymentModal } from '../components/common/StripePaymentModal';
import toast from 'react-hot-toast';
import { useEffect } from 'react';

const CreateOrderPage = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRecipient, setSelectedRecipient] = useState<any>(null);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    weight: '',
    productPrice: '',
    isProductPaid: false,
    deliveryPayer: 0,
  });
  const [createdOrderId, setCreatedOrderId] = useState<number | null>(null);
  const [actualDeliveryPrice, setActualDeliveryPrice] = useState<number>(0);
  const [isPaymentModalOpen, setIsPaymentModalOpen] = useState(false);

  // Handle pre-filled recipient from query params
  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const recipientId = params.get('recipientId');
    if (recipientId && !selectedRecipient) {
      userAPI.getUserById(parseInt(recipientId)).then(response => {
        setSelectedRecipient(response.data);
      }).catch(err => console.error("Failed to fetch recipient", err));
    }
  }, [location.search]);

  const { data: users } = useQuery({
    queryKey: ['users', 'search', searchQuery],
    queryFn: async () => {
      if (!searchQuery) return [];
      const response = await userAPI.searchUsers(searchQuery);
      return response.data;
    },
    enabled: searchQuery.length > 0,
  });

  const payOrderMutation = useMutation({
    mutationFn: paymentAPI.payOrder,
    onSuccess: () => {
      toast.success('Delivery paid successfully via Stripe!');
      navigate('/orders');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to process delivery payment');
      navigate('/orders'); // Navigate anyway, so user can retry on orders page
    },
  });

  const createOrderMutation = useMutation({
    mutationFn: orderAPI.createOrder,
    onSuccess: (response: any) => {
      if (formData.deliveryPayer === 0) {
        // Sender pays for delivery, open payment modal
        setCreatedOrderId(response.data.id);
        setActualDeliveryPrice(response.data.deliveryPrice);
        setIsPaymentModalOpen(true);
      } else {
        toast.success('Order created successfully!');
        navigate('/orders');
      }
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to create order');
    },
  });

  const handlePaymentSuccess = (paymentMethodId: string) => {
    if (createdOrderId) {
      payOrderMutation.mutate({ 
        orderId: createdOrderId, 
        payProduct: false, 
        payDelivery: true, 
        paymentMethod: 'stripe',
        stripeCardToken: paymentMethodId 
      });
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRecipient) {
      alert('Please select a recipient');
      return;
    }

    createOrderMutation.mutate({
      recipientId: selectedRecipient.id,
      name: formData.name,
      description: formData.description,
      weight: parseFloat(formData.weight),
      productPrice: parseFloat(formData.productPrice),
      isProductPaid: formData.isProductPaid,
      deliveryPayer: formData.deliveryPayer,
      files: selectedFiles,
    });
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      setSelectedFiles(Array.from(e.target.files));
    }
  };

  const removeFile = (index: number) => {
    setSelectedFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const { data: estimatedDeliveryPrice } = useQuery({
    queryKey: ['estimateDelivery', formData.weight],
    queryFn: async () => {
      if (!formData.weight || parseFloat(formData.weight) <= 0) return '0.00';
      const response = await orderAPI.estimateDeliveryPrice(parseFloat(formData.weight));
      return response.data.deliveryPrice.toFixed(2);
    },
    initialData: '0.00'
  });

  return (
    <Layout>
      <div className="max-w-4xl mx-auto flex flex-col gap-8">
        <section className="bg-surface-container p-6 sketch-border sketch-shadow relative transform -rotate-1">
          <h1 className="font-headline-lg text-headline-lg mb-2">New Shipment</h1>
          <p className="font-body-lg text-on-surface-variant italic">Manifest your delivery details and choose your recipient.</p>
        </section>

        <form onSubmit={handleSubmit} className="flex flex-col gap-12">
          {/* Recipient Section */}
          <SketchCard rotate>
            <div className="flex items-center gap-3 mb-8">
               <span className="material-symbols-outlined text-3xl text-primary-container">person_search</span>
               <h2 className="font-headline-md text-2xl">Recipient Information</h2>
            </div>

            <div className="relative mb-6">
              <SketchInput
                label="Search Directory"
                placeholder="Name, phone, or courier address..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                icon="search"
              />
              
              {users && users.length > 0 && searchQuery && (
                <div className="absolute z-10 left-0 right-0 top-full mt-2 bg-surface sketch-border sketch-shadow max-h-60 overflow-y-auto">
                  {users.map((user) => (
                    <div
                      key={user.id}
                      className="p-4 hover:bg-surface-container cursor-pointer flex items-center gap-4 border-b border-primary-container/5 last:border-0"
                      onClick={() => {
                        setSelectedRecipient(user);
                        setSearchQuery('');
                      }}
                    >
                      <div className="w-10 h-10 flex items-center justify-center sketch-border-thin bg-surface-container font-black">
                        {user.userName.charAt(0).toUpperCase()}
                      </div>
                      <div>
                        <p className="font-label-md font-bold text-sm">{user.userName}</p>
                        <p className="font-body-md text-xs text-on-surface-variant">{user.phoneNumber} • {user.address}</p>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {selectedRecipient && (
              <div className="p-4 bg-tertiary-fixed-dim sketch-border flex items-center justify-between transform rotate-1">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 flex items-center justify-center sketch-border-thin bg-surface font-black text-lg">
                    {selectedRecipient.userName.charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <p className="font-label-md font-black">{selectedRecipient.userName}</p>
                    <p className="font-body-md text-xs italic">{selectedRecipient.address}</p>
                  </div>
                </div>
                <SketchButton variant="secondary" className="text-xs py-1 px-3" onClick={() => setSelectedRecipient(null)}>
                  Change
                </SketchButton>
              </div>
            )}
          </SketchCard>

          {/* Package Details Section */}
          <SketchCard rotate={false}>
            <div className="flex items-center gap-3 mb-8">
               <span className="material-symbols-outlined text-3xl text-primary-container">inventory_2</span>
               <h2 className="font-headline-md text-2xl">Package Manifest</h2>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              <SketchInput
                label="Item Name"
                placeholder="Electronics, vintage scrolls, etc."
                required
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              />
              <SketchInput
                label="Weight (kg)"
                type="number"
                step="0.1"
                placeholder="0.0"
                required
                value={formData.weight}
                onChange={(e) => setFormData({ ...formData, weight: e.target.value })}
              />
            </div>

            <div className="mt-8">
               <label className="block font-label-md text-sm mb-2">Description</label>
               <textarea
                  className="w-full p-4 bg-surface sketch-border focus:outline-none focus:sketch-shadow transition-all font-body-md min-h-[120px] resize-none"
                  placeholder="Elaborate on the contents for the delivery protocol..."
                  required
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
               />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 mt-8">
               <SketchInput
                label="Product Price ($)"
                type="number"
                step="0.01"
                placeholder="0.00"
                required
                value={formData.productPrice}
                onChange={(e) => setFormData({ ...formData, productPrice: e.target.value })}
              />
              <div className="flex items-center mt-8">
                 <label className="flex items-center gap-4 cursor-pointer group">
                    <div className="relative">
                       <input 
                          type="checkbox" 
                          className="peer sr-only"
                          checked={formData.isProductPaid}
                          onChange={(e) => setFormData({ ...formData, isProductPaid: e.target.checked })}
                       />
                       <div className="w-6 h-6 sketch-border-thin bg-surface peer-checked:bg-primary-container transition-colors" />
                       <span className="material-symbols-outlined absolute top-0 left-0 text-surface scale-0 peer-checked:scale-100 transition-transform">check</span>
                    </div>
                    <span className="font-label-md text-sm">Product is already paid</span>
                 </label>
              </div>
            </div>

            <div className="mt-8">
              <label className="block font-label-md text-sm mb-2">Who Pays for Delivery?</label>
              <div className="flex gap-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="deliveryPayer"
                    value={0}
                    checked={formData.deliveryPayer === 0}
                    onChange={() => setFormData({ ...formData, deliveryPayer: 0 })}
                    className="accent-primary w-4 h-4"
                  />
                  <span className="font-label-md">I will pay (Sender)</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="deliveryPayer"
                    value={1}
                    checked={formData.deliveryPayer === 1}
                    onChange={() => setFormData({ ...formData, deliveryPayer: 1 })}
                    className="accent-primary w-4 h-4"
                  />
                  <span className="font-label-md">Recipient will pay</span>
                </label>
              </div>
            </div>

            <div className="mt-8">
              <label className="block font-label-md text-sm mb-2">Package Documentation (Optional)</label>
              <div className="flex flex-col gap-4">
                 <div className="relative group">
                    <input
                      type="file"
                      multiple
                      accept="image/*"
                      onChange={handleFileChange}
                      className="absolute inset-0 opacity-0 cursor-pointer z-10"
                    />
                    <div className="w-full p-6 sketch-border-thin bg-surface-container-low text-center group-hover:bg-surface-container transition-colors flex flex-col items-center gap-2">
                       <span className="material-symbols-outlined text-4xl opacity-40">cloud_upload</span>
                       <span className="font-body-md text-sm italic">Click or drag images to upload</span>
                    </div>
                 </div>

                {selectedFiles.length > 0 && (
                  <div className="flex flex-wrap gap-3">
                    {selectedFiles.map((file, index) => (
                      <div key={index} className="flex items-center gap-2 px-3 py-1 bg-secondary-fixed sketch-border-thin text-[10px] font-black uppercase">
                        <span className="material-symbols-outlined text-xs">image</span>
                        <span className="truncate max-w-[150px]">{file.name}</span>
                        <button type="button" onClick={() => removeFile(index)} className="hover:rotate-12 transition-transform">
                          <span className="material-symbols-outlined text-xs">close</span>
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </SketchCard>

          {/* Pricing & Submission */}
          <div className="flex flex-col md:flex-row items-center gap-8 justify-between p-8 bg-surface-container sketch-border sketch-shadow transform rotate-1">
             <div className="flex flex-wrap gap-12">
                <div className="flex flex-col">
                   <span className="font-label-md text-[10px] uppercase text-primary-container/40 font-black">Est. Delivery</span>
                   <span className="text-2xl font-black">${estimatedDeliveryPrice}</span>
                </div>
                 <div className="flex flex-col">
                   <span className="font-label-md text-[10px] uppercase text-primary-container/40 font-black">Total Due</span>
                   <span className="text-3xl font-black">
                      ${(
                        (parseFloat(estimatedDeliveryPrice) || 0) +
                        (formData.isProductPaid ? 0 : (parseFloat(formData.productPrice) || 0))
                      ).toFixed(2)}
                   </span>
                </div>
             </div>

             <SketchButton
                type="submit"
                className="w-full md:w-auto px-12 py-4 text-xl"
                icon={createOrderMutation.isPending || payOrderMutation.isPending ? 'sync' : 'rocket_launch'}
                disabled={createOrderMutation.isPending || payOrderMutation.isPending}
              >
                {createOrderMutation.isPending || payOrderMutation.isPending ? 'Manifesting...' : 'Launch Order'}
             </SketchButton>
          </div>
        </form>

        <StripePaymentModal
          isOpen={isPaymentModalOpen}
          onClose={() => {
            setIsPaymentModalOpen(false);
            navigate('/orders'); // If they cancel, they can pay later
          }}
          onSuccess={handlePaymentSuccess}
          isLoading={payOrderMutation.isPending}
          title="Pay for Delivery"
          amountText={`$${actualDeliveryPrice.toFixed(2)} (Delivery)`}
        />
      </div>
    </Layout>
  );
};

export default CreateOrderPage;
