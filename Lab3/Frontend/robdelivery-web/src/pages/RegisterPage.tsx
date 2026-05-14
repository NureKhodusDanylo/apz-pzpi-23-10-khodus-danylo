import React, { useState, useCallback, useEffect, useRef } from 'react';
import { motion } from 'framer-motion';
import { useNavigate, Link } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { 
  APIProvider, 
  Map, 
  AdvancedMarker, 
  useMap, 
  useMapsLibrary,
  MapControl,
  ControlPosition
} from '@vis.gl/react-google-maps';
import { GoogleLogin } from '@react-oauth/google';
import toast from 'react-hot-toast';
import { authAPI } from '../lib/api';
import { useAuthStore } from '../store/authStore';
import { SketchCard, SketchButton, SketchInput, SketchDivider } from '../components/common/SketchComponents';

const GOOGLE_MAPS_API_KEY = import.meta.env.VITE_GOOGLE_MAPS_KEY || '';

// Internal component to handle map interactions
const LocationPicker = ({ 
  onLocationChange, 
  onAddressChange,
  initialPosition 
}: { 
  onLocationChange: (lat: number, lng: number) => void,
  onAddressChange: (addr: string) => void,
  initialPosition: { lat: number, lng: number }
}) => {
  const map = useMap();
  const places = useMapsLibrary('places');
  const [markerPos, setMarkerPos] = useState(initialPosition);
  const [userPos, setUserPos] = useState<{ lat: number, lng: number } | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Reverse geocoding helper
  const updateAddress = useCallback((lat: number, lng: number) => {
    const geocoder = new google.maps.Geocoder();
    geocoder.geocode({ location: { lat, lng } }, (results, status) => {
      if (status === 'OK' && results?.[0]) {
        onAddressChange(results[0].formatted_address);
      }
    });
  }, [onAddressChange]);

  // Auto-center on mount
  useEffect(() => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition((pos) => {
        const { latitude, longitude } = pos.coords;
        const newPos = { lat: latitude, lng: longitude };
        setUserPos(newPos);
        setMarkerPos(newPos);
        onLocationChange(latitude, longitude);
        updateAddress(latitude, longitude);
        if (map) {
          map.setCenter(newPos);
          map.setZoom(15);
        }
      });
    }
  }, [map, onLocationChange, updateAddress]);

  // Handle map click
  const handleMapClick = (e: any) => {
    const lat = e.detail.latLng.lat;
    const lng = e.detail.latLng.lng;
    setMarkerPos({ lat, lng });
    onLocationChange(lat, lng);
    updateAddress(lat, lng);
  };

  // Handle "My Location" button click
  const moveToMyLocation = () => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition((pos) => {
        const { latitude, longitude } = pos.coords;
        const newPos = { lat: latitude, lng: longitude };
        setUserPos(newPos);
        setMarkerPos(newPos);
        onLocationChange(latitude, longitude);
        updateAddress(latitude, longitude);
        map?.panTo(newPos);
      });
    }
  };

  // Setup Autocomplete
  useEffect(() => {
    if (!places || !inputRef.current || !map) return;

    const autocomplete = new places.Autocomplete(inputRef.current);
    autocomplete.bindTo('bounds', map);

    autocomplete.addListener('place_changed', () => {
      const place = autocomplete.getPlace();
      if (!place.geometry || !place.geometry.location) return;

      const lat = place.geometry.location.lat();
      const lng = place.geometry.location.lng();
      const newPos = { lat, lng };

      setMarkerPos(newPos);
      onLocationChange(lat, lng);
      onAddressChange(place.formatted_address || '');
      
      if (place.geometry.viewport) {
        map.fitBounds(place.geometry.viewport);
      } else {
        map.setCenter(newPos);
        map.setZoom(17);
      }
    });
  }, [places, map, onLocationChange, onAddressChange]);

  return (
    <>
      <MapControl position={ControlPosition.TOP_LEFT}>
        <div className="m-2 flex gap-2">
          <input
            ref={inputRef}
            type="text"
            placeholder="Search location..."
            className="p-2 bg-surface sketch-border-thin shadow-md w-64 text-sm focus:outline-none"
          />
          <SketchButton 
            className="h-10 w-10 !p-0 shadow-md" 
            onClick={moveToMyLocation}
            icon="my_location"
          >
            {null}
          </SketchButton>
        </div>
      </MapControl>

      <Map
        style={{ width: '100%', height: '100%' }}
        defaultCenter={initialPosition}
        defaultZoom={13}
        mapId="DEMO_MAP_ID"
        onClick={handleMapClick}
        disableDefaultUI={true}
        zoomControl={true}
      >
        {/* Current User Real Position (Small dot) */}
        {userPos && (
          <AdvancedMarker position={userPos} zIndex={10}>
             <div className="w-3 h-3 bg-blue-500 rounded-full border-2 border-white shadow-sm" />
          </AdvancedMarker>
        )}

        {/* Selected Delivery Marker (Big icon) */}
        <AdvancedMarker position={markerPos} zIndex={20}>
           <div className="bg-primary-container text-surface p-1 rounded-full sketch-border-thin shadow-lg animate-bounce">
              <span className="material-symbols-outlined text-sm block">person_pin_circle</span>
           </div>
        </AdvancedMarker>
      </Map>
    </>
  );
};

const RegisterPage = () => {
  const navigate = useNavigate();
  const setAuth = useAuthStore((state) => state.setAuth);

  const [formData, setFormData] = useState({
    userName: '',
    email: '',
    password: '',
    phoneNumber: '',
    latitude: 50.4501,
    longitude: 30.5234,
    address: '',
  });

  const handleAuthSuccess = async (token: string) => {
    localStorage.setItem('token', token);
    try {
      const profileRes = await authAPI.getCurrentUser();
      setAuth(profileRes.data, token);
      toast.success('Welcome aboard!');
      navigate('/dashboard');
    } catch {
      toast.error('Failed to load user profile');
    }
  };

  const registerMutation = useMutation({
    mutationFn: authAPI.register,
    onSuccess: async (response) => {
      const { status, token, googleId, email, userName } = response.data;

      if (status === 'NeedsAdditionalInfo') {
        // Google user needs to fill phone + location
        toast('Please complete your registration', { icon: '📋' });
        navigate('/complete-profile', {
          state: { googleId, email, userName },
        });
        return;
      }

      if (status === 'Success' && token) {
        await handleAuthSuccess(token);
      }
    },
    onError: (error: any) => {
      console.error('Registration error:', error.response?.data);
      const message = error.response?.data?.message || error.response?.data?.status || 'Registration failed. Please try again.';
      toast.error(message);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    registerMutation.mutate(formData);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  const handleLocationSelect = useCallback((lat: number, lng: number) => {
    setFormData(prev => ({ ...prev, latitude: lat, longitude: lng }));
  }, []);

  const handleAddressChange = useCallback((addr: string) => {
    setFormData(prev => ({ ...prev, address: addr }));
  }, []);

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="w-full max-w-lg"
      >
        <SketchCard rotate shadow className="relative overflow-hidden">
          <div className="absolute top-2 right-4 text-outline-variant opacity-20">
            <span className="material-symbols-outlined text-6xl transform -rotate-12">person_add</span>
          </div>

          <div className="text-center mb-8">
            <div className="flex items-center justify-center gap-3 mb-4">
              <span className="material-symbols-outlined text-4xl text-primary-container">rocket_launch</span>
              <h1 className="text-3xl font-black text-primary-container">Join RobDelivery</h1>
            </div>
            <p className="font-body-lg text-on-surface-variant italic">Setting up your home base coordinates.</p>
          </div>

          <form onSubmit={handleSubmit} className="flex flex-col gap-5">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
              <SketchInput
                label="Username"
                name="userName"
                type="text"
                placeholder="Skywalker"
                required
                value={formData.userName}
                onChange={handleChange}
                disabled={registerMutation.isPending}
              />
              <SketchInput
                label="Email Address"
                name="email"
                type="email"
                placeholder="alex@delivery.com"
                required
                value={formData.email}
                onChange={handleChange}
                disabled={registerMutation.isPending}
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
              <SketchInput
                label="Password"
                name="password"
                type="password"
                placeholder="••••••••"
                required
                value={formData.password}
                onChange={handleChange}
                disabled={registerMutation.isPending}
              />
              <SketchInput
                label="Phone Number"
                name="phoneNumber"
                type="tel"
                placeholder="+380..."
                required
                value={formData.phoneNumber}
                onChange={handleChange}
                disabled={registerMutation.isPending}
              />
            </div>

            <div className="flex flex-col gap-2">
               <label className="font-label-md text-sm text-primary-container/70">Delivery Location (Click on Map)</label>
               <div className="h-[300px] w-full sketch-border-thin overflow-hidden relative shadow-inner">
                  <APIProvider apiKey={GOOGLE_MAPS_API_KEY} libraries={['places']}>
                    <LocationPicker 
                       initialPosition={{ lat: formData.latitude, lng: formData.longitude }}
                       onLocationChange={handleLocationSelect}
                       onAddressChange={handleAddressChange}
                    />
                  </APIProvider>
               </div>
               <div className="flex justify-between items-center text-[9px] font-bold opacity-40 px-1">
                  <span>LAT: {formData.latitude.toFixed(6)}</span>
                  <span>LNG: {formData.longitude.toFixed(6)}</span>
               </div>
            </div>

            <SketchInput
              label="Detected Address"
              name="address"
              type="text"
              readOnly
              placeholder="Select point on map..."
              value={formData.address}
              disabled={registerMutation.isPending}
            />

            <SketchButton
              type="submit"
              className="w-full mt-4"
              icon={registerMutation.isPending ? 'sync' : 'how_to_reg'}
            >
              {registerMutation.isPending ? 'Launching...' : 'Create Account'}
            </SketchButton>

            <div className="flex flex-col items-center gap-4 mt-2">
              <div className="w-full flex items-center gap-2 text-on-surface-variant/40">
                <div className="h-[1px] flex-1 bg-current opacity-20" />
                <span className="text-[10px] uppercase font-bold tracking-widest">or register with</span>
                <div className="h-[1px] flex-1 bg-current opacity-20" />
              </div>
              
              <div className="w-full flex justify-center">
                <GoogleLogin
                  onSuccess={(credentialResponse) => {
                    if (credentialResponse.credential) {
                      // Only send Google token — backend will return NeedsAdditionalInfo
                      registerMutation.mutate({ 
                        ...formData,
                        googleJwtToken: credentialResponse.credential 
                      });
                    }
                  }}
                  onError={() => {
                    toast.error('Google Registration Failed');
                  }}
                  theme="outline"
                  shape="square"
                  text="signup_with"
                />
              </div>
            </div>
          </form>

          <SketchDivider className="my-8" />

          <div className="text-center">
            <p className="font-body-md text-on-surface-variant mb-4">
              Already a member?{' '}
              <Link to="/login" className="text-primary-container font-black">
                Sign In
              </Link>
            </p>
            <Link to="/" className="font-label-md text-xs uppercase tracking-widest text-primary-container/60 hover:text-primary-container transition-colors">
              ← Back to Mission Control
            </Link>
          </div>
        </SketchCard>
      </motion.div>
    </div>
  );
};

export default RegisterPage;
