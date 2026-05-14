import React, { useState, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import Layout from '../components/Layout';
import { useAuthStore } from '../store/authStore';
import { SketchCard, SketchDivider, SketchAvatar } from '../components/common/SketchComponents';
import { authAPI, BASE_URL } from '../lib/api';
import { toast } from 'react-hot-toast';

const ProfilePage = () => {
  const user = useAuthStore((state) => state.user);
  const updateUser = useAuthStore((state) => state.updateUser);
  
  const [isEditing, setIsEditing] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [formData, setFormData] = useState({
    userName: user?.userName || '',
    phoneNumber: user?.phoneNumber || '',
    password: '',
  });
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setSelectedFile(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setPreviewUrl(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      const submitData = new FormData();
      if (formData.userName) submitData.append('userName', formData.userName);
      if (formData.phoneNumber) submitData.append('phoneNumber', formData.phoneNumber);
      if (formData.password) submitData.append('password', formData.password);
      if (selectedFile) submitData.append('profilePhoto', selectedFile);

      const response = await authAPI.updateProfile(submitData);
      
      // The response.data should contain the updated user profile
      // In UserController.cs, it returns Ok(new { message = "...", profile = updatedProfile })
      // But authAPI.updateProfile is typed as api.put<User>
      // Let's check the response structure. Based on UserController, it's { message: string, profile: User }
      const updatedUser = (response.data as any).profile || response.data;
      
      updateUser(updatedUser);
      setIsEditing(false);
      setFormData((prev) => ({ ...prev, password: '' }));
      setSelectedFile(null);
      setPreviewUrl(null);
      toast.success('Profile updated successfully!');
    } catch (error) {
      console.error('Failed to update profile:', error);
      toast.error('Failed to update profile. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const profilePhotoUrl = user?.profilePhotoUrl 
    ? (user.profilePhotoUrl.startsWith('http') 
        ? user.profilePhotoUrl 
        : `${BASE_URL}/${user.profilePhotoUrl.replace(/\\/g, '/')}`.replace(/([^:]\/)\/+/g, "$1"))
    : null;

  return (
    <Layout>
      <div className="max-w-4xl mx-auto flex flex-col gap-8">
        <section className="bg-surface-container p-6 sketch-border sketch-shadow relative transform rotate-1">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="font-headline-lg text-headline-lg mb-2">User Profile</h1>
              <p className="font-body-lg text-on-surface-variant italic">Manifest of your credentials and credentials in the delivery network.</p>
            </div>
            {!isEditing && (
              <button 
                onClick={() => setIsEditing(true)}
                className="sketch-button bg-primary-container text-surface px-6 py-2 flex items-center gap-2 transform -rotate-1 hover:rotate-0 transition-transform"
              >
                <span className="material-symbols-outlined">edit</span>
                Edit Profile
              </button>
            )}
          </div>
        </section>

        <SketchCard shadow rotate className="p-12">
          {isEditing ? (
            <form onSubmit={handleSubmit} className="space-y-12">
              <div className="flex flex-col md:flex-row items-center gap-12">
                <div 
                  className="relative group cursor-pointer"
                  onClick={() => fileInputRef.current?.click()}
                >
                  <SketchAvatar 
                    src={previewUrl || profilePhotoUrl} 
                    alt={user?.userName}
                    size="lg"
                    rotate={-3}
                    className="transition-transform group-hover:scale-105"
                  >
                    <div className="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                      <span className="material-symbols-outlined text-white text-3xl">photo_camera</span>
                    </div>
                  </SketchAvatar>
                  <input 
                    type="file" 
                    ref={fileInputRef} 
                    onChange={handleFileChange} 
                    className="hidden" 
                    accept="image/*"
                  />
                  <p className="text-[10px] text-center mt-2 font-black uppercase opacity-60">Change Photo</p>
                </div>
                <div className="flex-1 w-full space-y-6">
                  <div>
                    <label className="block font-label-md text-[10px] uppercase font-black opacity-40 mb-1">Identification (Username)</label>
                    <input
                      type="text"
                      name="userName"
                      value={formData.userName}
                      onChange={handleInputChange}
                      className="w-full bg-surface-container-low sketch-border-thin p-3 font-body-lg focus:outline-none focus:ring-2 focus:ring-primary-container/20"
                      required
                    />
                  </div>
                  <div>
                    <label className="block font-label-md text-[10px] uppercase font-black opacity-40 mb-1">Direct Frequency (Phone)</label>
                    <input
                      type="text"
                      name="phoneNumber"
                      value={formData.phoneNumber}
                      onChange={handleInputChange}
                      className="w-full bg-surface-container-low sketch-border-thin p-3 font-body-lg focus:outline-none focus:ring-2 focus:ring-primary-container/20"
                    />
                  </div>
                  <div>
                    <label className="block font-label-md text-[10px] uppercase font-black opacity-40 mb-1">New Security Protocol (Password - Leave empty to keep current)</label>
                    <input
                      type="password"
                      name="password"
                      value={formData.password}
                      onChange={handleInputChange}
                      placeholder="••••••••"
                      className="w-full bg-surface-container-low sketch-border-thin p-3 font-body-lg focus:outline-none focus:ring-2 focus:ring-primary-container/20"
                    />
                  </div>
                </div>
              </div>

              <div className="flex justify-end gap-4 mt-12">
                <button
                  type="button"
                  onClick={() => {
                    setIsEditing(false);
                    setPreviewUrl(null);
                    setSelectedFile(null);
                    setFormData({
                      userName: user?.userName || '',
                      phoneNumber: user?.phoneNumber || '',
                      password: '',
                    });
                  }}
                  className="sketch-button-secondary px-8 py-3 transform rotate-1"
                  disabled={isLoading}
                >
                  Abord Mission
                </button>
                <button
                  type="submit"
                  className="sketch-button bg-primary-container text-surface px-12 py-3 flex items-center gap-2 transform -rotate-1"
                  disabled={isLoading}
                >
                  {isLoading ? (
                    <motion.div
                      animate={{ rotate: 360 }}
                      transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                    >
                      <span className="material-symbols-outlined">sync</span>
                    </motion.div>
                  ) : (
                    <span className="material-symbols-outlined">save</span>
                  )}
                  {isLoading ? 'Encrypting...' : 'Update Protocol'}
                </button>
              </div>
            </form>
          ) : (
            <>
              <div className="flex flex-col md:flex-row items-center gap-12 mb-12">
                <SketchAvatar 
                  src={profilePhotoUrl} 
                  alt={user?.userName}
                  size="lg"
                  rotate={-3}
                />
                <div className="text-center md:text-left">
                  <h2 className="font-headline-lg text-4xl mb-2">{user?.userName}</h2>
                  <div className="inline-flex items-center gap-2 bg-secondary-fixed text-on-secondary-fixed px-4 py-1 sketch-border-thin text-xs font-black uppercase tracking-widest transform rotate-2">
                    <span className="material-symbols-outlined text-sm">verified_user</span>
                    {user?.role} Access Level
                  </div>
                </div>
              </div>

              <SketchDivider className="my-12" />

              <div className="grid grid-cols-1 md:grid-cols-2 gap-12">
                <div className="space-y-8">
                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 sketch-border-thin flex items-center justify-center bg-surface-container-low">
                      <span className="material-symbols-outlined text-primary-container">person</span>
                    </div>
                    <div>
                      <p className="font-label-md text-[10px] uppercase font-black opacity-40">Identification</p>
                      <p className="font-body-lg font-bold">{user?.userName}</p>
                    </div>
                  </div>

                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 sketch-border-thin flex items-center justify-center bg-surface-container-low">
                      <span className="material-symbols-outlined text-primary-container">mail</span>
                    </div>
                    <div>
                      <p className="font-label-md text-[10px] uppercase font-black opacity-40">Communication Line</p>
                      <p className="font-body-lg font-bold">{user?.email}</p>
                    </div>
                  </div>
                </div>

                <div className="space-y-8">
                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 sketch-border-thin flex items-center justify-center bg-surface-container-low">
                      <span className="material-symbols-outlined text-primary-container">phone</span>
                    </div>
                    <div>
                      <p className="font-label-md text-[10px] uppercase font-black opacity-40">Direct Frequency</p>
                      <p className="font-body-lg font-bold">{user?.phoneNumber || 'N/A'}</p>
                    </div>
                  </div>

                  <div className="flex items-center gap-4">
                    <div className="w-10 h-10 sketch-border-thin flex items-center justify-center bg-surface-container-low">
                      <span className="material-symbols-outlined text-primary-container">location_on</span>
                    </div>
                    <div>
                      <p className="font-label-md text-[10px] uppercase font-black opacity-40">Grid Coordinates</p>
                      <p className="font-body-lg font-bold italic">"{user?.address}"</p>
                    </div>
                  </div>
                </div>
              </div>

              <div className="mt-20 p-6 bg-surface-container-low sketch-border transform rotate-1">
                <div className="flex items-center gap-3 mb-2">
                  <span className="material-symbols-outlined text-warning">security</span>
                  <h4 className="font-headline-md text-lg">System Integrity</h4>
                </div>
                <p className="font-body-md text-sm italic text-on-surface-variant">
                  Your account is currently synced with the central robotic dispatch. 
                  Any changes to your coordinates must be verified via the protocol.
                </p>
              </div>
            </>
          )}
        </SketchCard>
      </div>
    </Layout>
  );
};

export default ProfilePage;
