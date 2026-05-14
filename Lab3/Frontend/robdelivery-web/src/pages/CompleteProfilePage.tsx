import React, { useState, useCallback, useEffect, useRef } from 'react';
import { motion } from 'framer-motion';
import { useNavigate, useLocation, Link } from 'react-router-dom';
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
import toast from 'react-hot-toast';
import { authAPI } from '../lib/api';
import { useAuthStore } from '../store/authStore';
import { SketchCard, SketchButton, SketchInput, SketchDivider } from '../components/common/SketchComponents';

const GOOGLE_MAPS_API_KEY = import.meta.env.VITE_GOOGLE_MAPS_KEY || '';

// Reusable LocationPicker (same as RegisterPage)
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

  const updateAddress = useCallback((lat: number, lng: number) => {
    const geocoder = new google.maps.Geocoder();
    geocoder.geocode({ location: { lat, lng } }, (results, status) => {
      if (status === 'OK' && results?.[0]) {
        onAddressChange(results[0].formatted_address);
      }
    });
  }, [onAddressChange]);

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

  const handleMapClick = (e: any) => {
    const lat = e.detail.latLng.lat;
    const lng = e.detail.latLng.lng;
    setMarkerPos({ lat, lng });
    onLocationChange(lat, lng);
    updateAddress(lat, lng);
  };

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
        {userPos && (
          <AdvancedMarker position={userPos} zIndex={10}>
             <div className="w-3 h-3 bg-blue-500 rounded-full border-2 border-white shadow-sm" />
          </AdvancedMarker>
        )}

        <AdvancedMarker position={markerPos} zIndex={20}>
           <div className="bg-primary-container text-surface p-1 rounded-full sketch-border-thin shadow-lg animate-bounce">
              <span className="material-symbols-outlined text-sm block">person_pin_circle</span>
           </div>
        </AdvancedMarker>
      </Map>
    </>
  );
};

interface GoogleRouteState {
  googleId: string;
  email: string;
  userName: string;
}

const CompleteProfilePage = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const setAuth = useAuthStore((state) => state.setAuth);

  // Get Google data from route state
  const googleState = location.state as GoogleRouteState | null;

  const [formData, setFormData] = useState({
    phoneNumber: '',
    latitude: 50.4501,
    longitude: 30.5234,
    address: '',
  });

  // If no Google state → redirect to register
  useEffect(() => {
    if (!googleState?.googleId) {
      toast.error('No Google account data. Please register first.');
      navigate('/register');
    }
  }, [googleState, navigate]);

  const completeMutation = useMutation({
    mutationFn: authAPI.completeGoogleRegistration,
    onSuccess: async (response) => {
      const { status, token } = response.data;

      if (status === 'Success' && token) {
        localStorage.setItem('token', token);
        try {
          const profileRes = await authAPI.getCurrentUser();
          setAuth(profileRes.data, token);
          toast.success('Registration complete! Welcome aboard!');
          navigate('/dashboard');
        } catch {
          toast.error('Failed to load profile');
        }
      }
    },
    onError: (error: any) => {
      console.error('Complete registration error:', error.response?.data);
      const message = error.response?.data?.message || 'Failed to complete registration';
      toast.error(message);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!googleState) return;

    completeMutation.mutate({
      googleId: googleState.googleId,
      email: googleState.email,
      userName: googleState.userName,
      phoneNumber: formData.phoneNumber,
      latitude: formData.latitude,
      longitude: formData.longitude,
      address: formData.address || undefined,
    });
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

  if (!googleState) return null;

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="w-full max-w-lg"
      >
        <SketchCard rotate shadow className="relative overflow-hidden">
          <div className="absolute top-2 right-4 text-outline-variant opacity-20">
            <span className="material-symbols-outlined text-6xl transform rotate-6">assignment_ind</span>
          </div>

          <div className="text-center mb-8">
            <div className="flex items-center justify-center gap-3 mb-4">
              <span className="material-symbols-outlined text-4xl text-primary-container">badge</span>
              <h1 className="text-3xl font-black text-primary-container">Almost There!</h1>
            </div>
            <p className="font-body-lg text-on-surface-variant italic">
              We need a few more details to set up your delivery base.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="flex flex-col gap-5">
            {/* Pre-filled Google info (read-only) */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
              <SketchInput
                label="Name (from Google)"
                name="userName"
                type="text"
                value={googleState.userName || ''}
                readOnly
                disabled
              />
              <SketchInput
                label="Email (from Google)"
                name="email"
                type="email"
                value={googleState.email || ''}
                readOnly
                disabled
              />
            </div>

            {/* User must fill these */}
            <SketchInput
              label="Phone Number"
              name="phoneNumber"
              type="tel"
              placeholder="+380..."
              required
              value={formData.phoneNumber}
              onChange={handleChange}
              disabled={completeMutation.isPending}
            />

            <div className="flex flex-col gap-2">
               <label className="font-label-md text-sm text-primary-container/70">
                 Delivery Location (Click on Map)
               </label>
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
              disabled={completeMutation.isPending}
            />

            <SketchButton
              type="submit"
              className="w-full mt-4"
              icon={completeMutation.isPending ? 'sync' : 'check_circle'}
            >
              {completeMutation.isPending ? 'Setting up...' : 'Complete Registration'}
            </SketchButton>
          </form>

          <SketchDivider className="my-8" />

          <div className="text-center">
            <Link to="/register" className="font-label-md text-xs uppercase tracking-widest text-primary-container/60 hover:text-primary-container transition-colors">
              ← Back to Registration
            </Link>
          </div>
        </SketchCard>
      </motion.div>
    </div>
  );
};

export default CompleteProfilePage;
