import { useNavigate } from 'react-router-dom';
import React, { useEffect, useState, useCallback, Component, ErrorInfo, ReactNode, useMemo } from 'react';
import { APIProvider, Map, AdvancedMarker, useMap, useApiIsLoaded } from '@vis.gl/react-google-maps';
import * as signalR from '@microsoft/signalr';
import { mapAPI, friendshipAPI, BASE_URL } from '../lib/api';
import type { RobotMapPosition, NodeMapPosition, Friend, RouteSegmentDTO } from '../types';
import Layout from '../components/Layout';
import { useAuthStore } from '../store/authStore';
import { SketchCard, SketchDivider, SketchButton } from '../components/common/SketchComponents';

// Updated API Key provided by user
const GOOGLE_MAPS_API_KEY = import.meta.env.VITE_GOOGLE_MAPS_KEY || '';
// Synced with api.ts base URL
const SIGNALR_HUB_URL = `${BASE_URL}/hubs/map`;
const DEFAULT_CENTER = { lat: 50.4501, lng: 30.5234 };

// Error Boundary to catch library crashes (like the getRootNode error)
class MapErrorBoundary extends Component<{children: ReactNode}, {hasError: boolean}> {
  constructor(props: {children: ReactNode}) {
    super(props);
    this.state = { hasError: false };
  }
  static getDerivedStateFromError() { return { hasError: true }; }
  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("Map Marker Error caught:", error, errorInfo);
  }
  render() {
    if (this.state.hasError) {
      return (
        <div className="absolute inset-0 flex items-center justify-center bg-surface-container/90 z-20 p-8 text-center">
          <div className="sketch-border bg-error text-surface p-6 max-w-sm shadow-xl">
            <h3 className="font-headline-md mb-2">Map Interface Error</h3>
            <p className="font-body-md opacity-90 italic">
              There was a problem rendering the map markers. 
              Please ensure your API Key is valid and "Advanced Markers" are enabled in the Google Cloud Console.
            </p>
            <SketchButton 
              className="mt-4 bg-surface text-on-surface"
              onClick={() => this.setState({ hasError: false })}
            >
              Try Again
            </SketchButton>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}

// Internal component for rendering routes
const RoutePolyline = ({ 
  route, 
  status,
  robotPos
}: { 
  route: RouteSegmentDTO[], 
  status: string,
  robotPos?: { lat: number, lng: number }
}) => {
  const map = useMap();
  const apiIsLoaded = useApiIsLoaded();

  useEffect(() => {
    if (!apiIsLoaded || !map || !route || route.length === 0) {
      return;
    }

    // Build path
    const path: google.maps.LatLngLiteral[] = [];
    
    // 1. Current robot position
    if (robotPos && typeof robotPos.lat === 'number' && typeof robotPos.lng === 'number') {
      path.push(robotPos);
    }

    // 2. All segment start points
    route.forEach((segment) => {
      if (typeof segment.fromLatitude === 'number' && typeof segment.fromLongitude === 'number') {
        path.push({
          lat: segment.fromLatitude,
          lng: segment.fromLongitude
        });
      }
    });
    
    // 3. Final destination
    const lastSegment = route[route.length - 1];
    if (lastSegment && typeof lastSegment.toLatitude === 'number' && typeof lastSegment.toLongitude === 'number') {
      path.push({
        lat: lastSegment.toLatitude,
        lng: lastSegment.toLongitude
      });
    }

    if (path.length < 2) return;

    // Force high contrast orange for debugging
    const strokeColor = '#f59e0b'; 

    const polyline = new window.google.maps.Polyline({
      map,
      path,
      geodesic: true,
      strokeColor: strokeColor,
      strokeOpacity: 1.0,
      strokeWeight: 6,
      zIndex: 100
    });

    return () => {
      polyline.setMap(null);
    };
  }, [map, apiIsLoaded, route, status, robotPos]);

  return null;
};

// Component to handle markers and map centering
const MapContent = ({ 
  robots, 
  nodes, 
  friends,
  userLocation,
  viewMode,
  selectedRobotId,
  user,
  navigate
}: { 
  robots: RobotMapPosition[], 
  nodes: NodeMapPosition[],
  friends: Friend[],
  userLocation: { lat: number, lng: number } | null,
  viewMode: 'general' | 'delivery',
  selectedRobotId: number | null,
  user: any,
  navigate: any
}) => {
  const map = useMap();
  const apiIsLoaded = useApiIsLoaded();
  const [selectedItem, setSelectedItem] = useState<{ type: 'robot' | 'node', data: any } | null>(null);
  const [zoom, setZoom] = useState(12);
  const [viewBounds, setViewBounds] = useState<{
    north: number,
    south: number,
    east: number,
    west: number
  } | null>(null);

  useEffect(() => {
    if (map && userLocation && viewMode === 'general') {
      map.panTo(userLocation);
    }
  }, [map, userLocation, viewMode]);

  // Track zoom and bounds changes with debouncing/optimization
  useEffect(() => {
    if (!map) return;
    
    let timeoutId: any;
    const updateMapState = () => {
      clearTimeout(timeoutId);
      timeoutId = setTimeout(() => {
        const currentZoom = map.getZoom() || 12;
        const b = map.getBounds();
        setZoom(currentZoom);
        
        if (b) {
          const ne = b.getNorthEast();
          const sw = b.getSouthWest();
          setViewBounds({
            north: ne.lat(),
            south: sw.lat(),
            east: ne.lng(),
            west: sw.lng()
          });
        }
      }, 50); // 50ms debounce for smoother panning
    };

    const listeners = [
      map.addListener('zoom_changed', updateMapState),
      map.addListener('bounds_changed', updateMapState),
      map.addListener('idle', updateMapState)
    ];

    updateMapState();
    return () => {
      listeners.forEach(l => l.remove());
      clearTimeout(timeoutId);
    };
  }, [map]);

  // High-performance numerical filtering (no LatLng objects)
  const visibleNodes = useMemo(() => {
    let targetNodes = nodes;
    
    if (viewMode === 'delivery') {
      if (selectedRobotId) {
        const robot = robots.find(r => r.id === selectedRobotId);
        if (robot) {
          targetNodes = nodes.filter(node => 
            node.id === robot.currentNodeId || node.id === robot.targetNodeId
          );
        } else {
          targetNodes = [];
        }
      } else {
        // No delivery focused - show only user's node if they have one
        if (user?.personalNodeId) {
          targetNodes = nodes.filter(node => node.id === user.personalNodeId);
        } else {
          targetNodes = [];
        }
      }
    }

    if (zoom < 13 && viewMode === 'general') return []; 
    if (!viewBounds) return targetNodes;
    
    return targetNodes.filter(node => 
      node.latitude <= viewBounds.north && 
      node.latitude >= viewBounds.south && 
      node.longitude <= viewBounds.east && 
      node.longitude >= viewBounds.west
    );
  }, [nodes, robots, zoom, viewBounds, viewMode, selectedRobotId, user]);

  const visibleRobots = useMemo(() => {
    let targetRobots = robots;

    if (viewMode === 'delivery') {
      if (selectedRobotId) {
        targetRobots = robots.filter(r => r.id === selectedRobotId);
      } else {
        targetRobots = [];
      }
    }

    if (!viewBounds) return targetRobots;
    return targetRobots.filter(robot => 
      robot.latitude && robot.longitude &&
      robot.latitude <= viewBounds.north && 
      robot.latitude >= viewBounds.south && 
      robot.longitude <= viewBounds.east && 
      robot.longitude >= viewBounds.west
    );
  }, [robots, viewBounds, viewMode, selectedRobotId]);

  useEffect(() => {
    if (map && viewMode === 'delivery' && selectedRobotId) {
      const robot = robots.find(r => r.id === selectedRobotId);
      if (robot && robot.latitude && robot.longitude) {
        map.panTo({ lat: robot.latitude, lng: robot.longitude });
        map.setZoom(15);
      }
    }
  }, [map, viewMode, selectedRobotId, robots]);

  const markerScale = useMemo(() => Math.max(0.4, Math.min(1.5, zoom / 15)), [zoom]);

  const getNodeIcon = useCallback((type: string) => {
    switch (type) {
      case 'ChargingStation': return { icon: 'ev_station', color: '#10b981' };
      case 'Depot': return { icon: 'warehouse', color: '#3b82f6' };
      case 'UserNode': return { icon: 'person_pin_circle', color: '#8b5cf6' };
      default: return { icon: 'location_on', color: '#6b7280' };
    }
  }, []);

  const getRobotIcon = useCallback((status: string) => {
    // Handle both enum-name strings and possible raw enum values (though strings are expected due to JSON settings)
    switch (status) {
      case 'Idle': return { icon: 'smart_toy', color: '#22c55e' };
      case 'Delivering': return { icon: 'local_shipping', color: '#f59e0b' };
      case 'Charging': return { icon: 'battery_charging_full', color: '#3b82f6' };
      case 'Maintenance': return { icon: 'build', color: '#ef4444' };
      default: return { icon: 'smart_toy', color: '#6b7280' };
    }
  }, []);

  if (!apiIsLoaded || !map) return null;

  return (
    <MapErrorBoundary>
      {/* User Location Marker - Priority: REQUIRED */}
      {userLocation && viewBounds && 
        userLocation.lat <= viewBounds.north && userLocation.lat >= viewBounds.south && (
        <AdvancedMarker 
          position={userLocation} 
          zIndex={100}
          collisionBehavior="REQUIRED"
        >
          <div 
            className="relative flex items-center justify-center transition-transform duration-200"
            style={{ transform: `scale(${markerScale})` }}
          >
            <div className="absolute w-6 h-6 bg-blue-500/20 rounded-full animate-ping" />
            <div className="relative bg-blue-600 text-white p-1 rounded-full sketch-border-thin shadow-lg">
              <span className="material-symbols-outlined text-[12px] block">my_location</span>
            </div>
          </div>
        </AdvancedMarker>
      )}

      {/* Node Markers - HIDE ON OVERLAP for performance */}
      {visibleNodes.map((node) => {
        const { icon, color } = getNodeIcon(node.type);
        const isPersonal = node.id === user?.personalNodeId;
        const friend = friends.find(f => f.personalNodeId === node.id);
        const isFriend = !!friend;
        
        return (
          <AdvancedMarker
            key={`node-${node.id}`}
            position={{ lat: node.latitude, lng: node.longitude }}
            onClick={() => setSelectedItem({ type: 'node', data: { ...node, friendName: friend?.userName } })}
            collisionBehavior={(isPersonal || isFriend) ? "REQUIRED" : "OPTIONAL_AND_HIDES_LOWER_PRIORITY"}
            zIndex={isPersonal ? 80 : (isFriend ? 70 : 0)}
          >
            <div 
              className="flex flex-col items-center group cursor-pointer transition-transform duration-200"
              style={{ transform: `scale(${(isPersonal || isFriend) ? markerScale * 1.15 : markerScale})` }}
            >
               <div className="relative">
                 {isPersonal && (
                   <div className="absolute -inset-1 bg-rose-500/20 rounded-full animate-ping pointer-events-none" />
                 )}
                 {isFriend && (
                   <div className="absolute -inset-1 bg-amber-500/20 rounded-full animate-pulse pointer-events-none" />
                 )}
                 <div 
                   className={`p-1 rounded-full sketch-border-thin shadow-sm transition-all group-hover:scale-125 group-hover:shadow-md ${isPersonal ? 'ring-2 ring-rose-500 ring-offset-1 ring-offset-surface' : (isFriend ? 'ring-2 ring-amber-500 ring-offset-1 ring-offset-surface' : '')}`}
                   style={{ backgroundColor: isPersonal ? '#f43f5e' : (isFriend ? '#f59e0b' : color), color: 'white' }}
                 >
                   <span className="material-symbols-outlined text-sm block">
                     {isFriend ? 'person' : icon}
                   </span>
                 </div>
               </div>

               <div className="absolute -bottom-8 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-50">
                  <div className="px-2 py-0.5 bg-surface-container-high sketch-border-thin text-[8px] font-black uppercase whitespace-nowrap shadow-xl">
                    {isFriend ? `${friend.userName}'s Hub` : node.name}
                  </div>
               </div>
            </div>
          </AdvancedMarker>
        );
      })}

      {/* Robot Markers - Higher priority than nodes */}
      {visibleRobots.map((robot) => {
        const { icon, color } = getRobotIcon(robot.statusName);
        return (
          <AdvancedMarker
            key={`robot-${robot.id}`}
            position={{ lat: robot.latitude!, lng: robot.longitude! }}
            zIndex={50}
            onClick={() => setSelectedItem({ type: 'robot', data: robot })}
            collisionBehavior="REQUIRED_AND_HIDES_OPTIONAL"
          >
            <div 
              className="flex flex-col items-center group cursor-pointer transition-transform duration-200"
              style={{ transform: `scale(${markerScale})` }}
            >
                 <div className="relative">
                    <div 
                      className="p-1 rounded-lg sketch-border-thin shadow-md transition-all group-hover:scale-110 group-hover:-translate-y-1"
                      style={{ backgroundColor: color, color: 'white' }}
                    >
                       <span className="material-symbols-outlined text-xs block">{icon}</span>
                    </div>
                    <div className="absolute -top-8 left-1/2 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-50">
                       <div className="whitespace-nowrap bg-surface-container-high px-2 py-0.5 sketch-border-thin text-[8px] font-black uppercase shadow-xl">
                          {robot.name}
                       </div>
                    </div>
                 </div>
              </div>
            </AdvancedMarker>
          );
        })}

      {/* Route Polylines */}
      {robots
        .filter(r => r.route && r.route.length > 0)
        .filter(r => viewMode !== 'delivery' || !selectedRobotId || r.id === selectedRobotId)
        .map((robot) => (
          <RoutePolyline 
            key={`route-${robot.id}`} 
            route={robot.route!} 
            status={robot.statusName}
            robotPos={(robot.latitude !== undefined && robot.longitude !== undefined) ? { lat: robot.latitude, lng: robot.longitude } : undefined}
          />
        ))}

      {/* Detail Panel */}
      {selectedItem && (
        <div className="absolute bottom-4 left-4 right-4 md:left-auto md:right-4 md:w-80 z-50 animate-in fade-in slide-in-from-bottom-4 duration-300">
           <SketchCard rotate={false} shadow className="relative !p-6 bg-surface-container-high">
              <button 
                onClick={() => setSelectedItem(null)}
                className="absolute top-4 right-4 hover:rotate-90 transition-transform"
              >
                 <span className="material-symbols-outlined text-lg">close</span>
              </button>

              <div className="flex items-start gap-4">
                    <div 
                      className="w-12 h-12 rounded-xl flex items-center justify-center text-white shrink-0"
                      style={{ backgroundColor: selectedItem.type === 'robot' ? getRobotIcon(selectedItem.data.statusName).color : getNodeIcon(selectedItem.data.type).color }}
                    >
                       <span className="material-symbols-outlined text-2xl">
                          {selectedItem.type === 'robot' ? getRobotIcon(selectedItem.data.statusName).icon : getNodeIcon(selectedItem.data.type).icon}
                       </span>
                    </div>
                 <div>
                    <h4 className="font-headline-md text-xl font-black">{selectedItem.data.name}</h4>
                    <p className="font-label-md text-[10px] uppercase font-black opacity-40">
                       {selectedItem.type === 'robot' ? `Fleet Unit #${selectedItem.data.id}` : `Infrastructure Node`}
                    </p>
                 </div>
              </div>

              <div className="space-y-4">
                 <div className="grid grid-cols-1 gap-4">
                    <a 
                       href={`https://www.google.com/maps/search/?api=1&query=${selectedItem.data.latitude},${selectedItem.data.longitude}`}
                       target="_blank"
                       rel="noopener noreferrer"
                       className="p-3 bg-surface-container-low sketch-border-thin flex flex-col items-center justify-center group hover:bg-primary-container/10 transition-colors"
                    >
                       <p className="font-label-md text-[8px] uppercase opacity-40 mb-1">Open In</p>
                       <div className="flex items-center gap-2">
                          <span className="material-symbols-outlined text-sm">map</span>
                          <span className="font-body-md font-bold text-[10px]">Google Maps</span>
                       </div>
                    </a>
                 </div>

                 {selectedItem.type === 'node' && selectedItem.data.address && (
                    <div className="p-3 bg-surface-container-low sketch-border-thin">
                       <p className="font-label-md text-[8px] uppercase opacity-40 mb-1">Manifest Address</p>
                       <p className="font-body-md text-xs italic">{selectedItem.data.address}</p>
                    </div>
                 )}

                 {selectedItem.type === 'node' && selectedItem.data.friendName && (
                   <SketchButton 
                     className="w-full mt-2"
                     icon="send"
                     onClick={() => {
                        // Find the friend ID from nodes if we had it, but better just pass it in data
                        // For now, we can search by friendName or we should have passed friend object
                        const f = friends.find(f => f.userName === selectedItem.data.friendName);
                        if (f) navigate(`/orders/create?recipientId=${f.id}`);
                     }}
                   >
                     Quick Send to {selectedItem.data.friendName}
                   </SketchButton>
                 )}
              </div>

              <SketchButton 
                className="w-full mt-6 text-xs" 
                variant="secondary"
                onClick={() => map.panTo({ lat: selectedItem.data.latitude, lng: selectedItem.data.longitude })}
              >
                 Lock On Satellite
              </SketchButton>
           </SketchCard>
        </div>
      )}
    </MapErrorBoundary>
  );
};

function MapPage() {
  const navigate = useNavigate();
  const [robots, setRobots] = useState<RobotMapPosition[]>([]);
  const [nodes, setNodes] = useState<NodeMapPosition[]>([]);
  const [friends, setFriends] = useState<Friend[]>([]);
  const [userLocation, setUserLocation] = useState<{ lat: number, lng: number } | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [hubConnection, setHubConnection] = useState<signalR.HubConnection | null>(null);
  const [viewMode, setViewMode] = useState<'general' | 'delivery'>('general');
  const [selectedRobotId, setSelectedRobotId] = useState<number | null>(null);
  const { token, user } = useAuthStore();

  const findUserLocation = useCallback(() => {
    if (!navigator.geolocation) {
      console.error('Geolocation is not supported by your browser');
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setUserLocation({
          lat: position.coords.latitude,
          lng: position.coords.longitude,
        });
      },
      (error) => {
        console.error('Error getting location:', error);
      }
    );
  }, []);

  useEffect(() => {
    loadMapData();
    findUserLocation();
  }, [findUserLocation]);

  useEffect(() => {
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(SIGNALR_HUB_URL, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    connection
      .start()
      .then(() => {
        console.log('SignalR Connected');
        setHubConnection(connection);
      })
      .catch((err) => {
        console.error('SignalR Connection Error: ', err);
        if (err.toString().includes('511')) {
           setError('API Tunnel Connection Required. Please open the API URL in a new tab to bypass protection.');
        }
      });

    connection.on('ReceiveRobotUpdate', (robotUpdate: RobotMapPosition) => {
      setRobots((prevRobots) => {
        const index = prevRobots.findIndex((r) => r.id === robotUpdate.id);
        if (index !== -1) {
          const updated = [...prevRobots];
          const oldRobot = updated[index];
          
          // Only update route if the new data contains one
          // This prevents status updates from IoT devices (which don't have route data) from clearing the path
          const hasNewRoute = robotUpdate.route !== undefined && robotUpdate.route !== null;
          
          updated[index] = { 
            ...oldRobot, 
            ...robotUpdate,
            route: hasNewRoute ? robotUpdate.route : oldRobot.route
          };
          return updated;
        } else {
          return [...prevRobots, robotUpdate];
        }
      });
    });

    connection.on('ReceiveNodeUpdate', (nodeUpdate: NodeMapPosition) => {
      setNodes((prevNodes) => {
        const index = prevNodes.findIndex((n) => n.id === nodeUpdate.id);
        if (index !== -1) {
          const updated = [...prevNodes];
          updated[index] = nodeUpdate;
          return updated;
        } else {
          return [...prevNodes, nodeUpdate];
        }
      });
    });

    return () => {
      connection.stop();
    };
  }, [token]);

  const loadMapData = async () => {
    try {
      setLoading(true);
      const [mapResponse, friendsResponse] = await Promise.all([
        mapAPI.getMapData(),
        friendshipAPI.getFriends()
      ]);
      setRobots(mapResponse.data.robots);
      setNodes(mapResponse.data.nodes);
      setFriends(friendsResponse.data);
      setError(null);
    } catch (err) {
      console.error('Error loading map data:', err);
      setError('Failed to load map data');
    } finally {
      setLoading(false);
    }
  };

  const activeDeliveries = useMemo(() => 
    robots.filter(r => r.statusName === 'Delivering'), 
    [robots]
  );

  return (
    <Layout>
      <div className="flex flex-col gap-8">
        <section className="bg-surface-container p-6 sketch-border sketch-shadow relative transform rotate-1 flex flex-col md:flex-row justify-between items-center gap-6">
          <div>
            <h1 className="font-headline-lg text-headline-lg mb-2">Network Map</h1>
            <p className="font-body-lg text-on-surface-variant italic">Live visual telemetry for all active robots and stations.</p>
          </div>
          <div className="flex gap-4">
            <SketchButton icon="my_location" variant="secondary" onClick={findUserLocation}>Where Am I?</SketchButton>
            <SketchButton icon="sync" onClick={loadMapData}>Re-Sync Map</SketchButton>
          </div>
        </section>

        {error && (
          <div className="p-4 bg-error text-surface sketch-border font-label-md flex flex-col gap-2">
            <div className="flex items-center gap-2">
               <span className="material-symbols-outlined">warning</span>
               <span>{error}</span>
            </div>
            {error.includes('Tunnel') && (
              <a 
                href={SIGNALR_HUB_URL.replace('/hubs/map', '')} 
                target="_blank" 
                rel="noopener noreferrer"
                className="font-black hover:text-white/80"
              >
                Open API Tunnel URL &rarr;
              </a>
            )}
          </div>
        )}

        {loading ? (
          <div className="text-center py-24">
              <span className="material-symbols-outlined animate-spin text-4xl">sync</span>
              <p className="mt-4 font-label-md">Calibrating satellite feed...</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 xl:grid-cols-4 gap-8">
            {/* Sidebar Controls */}
            <div className="xl:col-span-1 space-y-8">
              <SketchCard rotate={false} className="bg-primary/5">
                <h3 className="font-headline-md text-xl mb-4">Map View Mode</h3>
                <div className="grid grid-cols-2 gap-2 p-1 bg-surface-container-low sketch-border-thin">
                  <button 
                    onClick={() => { setViewMode('general'); setSelectedRobotId(null); }}
                    className={`flex flex-col items-center py-3 transition-all ${viewMode === 'general' ? 'bg-primary text-white sketch-shadow-sm scale-95' : 'hover:bg-primary/10'}`}
                  >
                    <span className="material-symbols-outlined mb-1">public</span>
                    <span className="text-[10px] font-black uppercase tracking-wider">General</span>
                  </button>
                  <button 
                    onClick={() => setViewMode('delivery')}
                    className={`flex flex-col items-center py-3 transition-all ${viewMode === 'delivery' ? 'bg-secondary text-white sketch-shadow-sm scale-95' : 'hover:bg-secondary/10'}`}
                  >
                    <span className="material-symbols-outlined mb-1">local_shipping</span>
                    <span className="text-[10px] font-black uppercase tracking-wider">Delivery</span>
                  </button>
                </div>

                {viewMode === 'delivery' && (
                  <div className="mt-6 animate-in fade-in slide-in-from-top-2 duration-300">
                    <h4 className="font-label-md text-[10px] uppercase font-black opacity-40 mb-3 tracking-widest">Select Active Drone</h4>
                    <div className="space-y-2 max-h-48 overflow-y-auto pr-2 custom-scrollbar">
                      {activeDeliveries.length > 0 ? (
                        activeDeliveries.map(robot => (
                          <button
                            key={robot.id}
                            onClick={() => setSelectedRobotId(robot.id)}
                            className={`w-full p-3 text-left sketch-border-thin transition-all flex items-center gap-3 ${selectedRobotId === robot.id ? 'bg-secondary/20 border-secondary scale-95' : 'bg-surface-container-low hover:bg-surface-container'}`}
                          >
                            <span className="material-symbols-outlined text-secondary">smart_toy</span>
                            <div className="flex-1 min-w-0">
                              <p className="text-xs font-black truncate">{robot.name}</p>
                              <p className="text-[8px] opacity-60 truncate">To: {robot.targetNodeName || 'Unknown'}</p>
                            </div>
                            {selectedRobotId === robot.id && (
                              <span className="material-symbols-outlined text-sm">check_circle</span>
                            )}
                          </button>
                        ))
                      ) : (
                        <div className="p-4 text-center sketch-border-thin bg-surface-container-low italic text-[10px] opacity-60">
                          No active deliveries detected in sector.
                        </div>
                      )}
                    </div>
                    {selectedRobotId && (
                      <SketchButton 
                        variant="secondary" 
                        className="w-full mt-4 !text-[10px]"
                        onClick={() => setSelectedRobotId(null)}
                      >
                        Reset Selection
                      </SketchButton>
                    )}
                  </div>
                )}
              </SketchCard>

              <SketchCard rotate>
                <h3 className="font-headline-md text-xl mb-4">Map Legend</h3>
                <div className="space-y-6">
                  <div>
                    <h4 className="font-label-md text-[10px] uppercase font-black opacity-40 mb-3 tracking-widest">Infrastructures</h4>
                    <div className="space-y-3">
                       <div className="flex items-center gap-3 text-xs font-bold">
                          <div className="w-6 h-6 flex items-center justify-center rounded-full sketch-border-thin bg-[#10b981] text-white">
                            <span className="material-symbols-outlined text-xs">ev_station</span>
                          </div>
                          <span>Charging Station</span>
                       </div>
                       <div className="flex items-center gap-3 text-xs font-bold">
                          <div className="w-6 h-6 flex items-center justify-center rounded-full sketch-border-thin bg-[#3b82f6] text-white">
                            <span className="material-symbols-outlined text-xs">warehouse</span>
                          </div>
                          <span>Hub Depot</span>
                       </div>
                       <div className="flex items-center gap-3 text-xs font-bold">
                          <div className="w-6 h-6 flex items-center justify-center rounded-full sketch-border-thin bg-[#8b5cf6] text-white">
                            <span className="material-symbols-outlined text-xs">person_pin_circle</span>
                          </div>
                          <span>Recipient Node</span>
                       </div>
                    </div>
                  </div>
                  <SketchDivider />
                  <div>
                    <h4 className="font-label-md text-[10px] uppercase font-black opacity-40 mb-3 tracking-widest">Fleet Status</h4>
                    <div className="space-y-3">
                       <div className="flex items-center gap-3 text-xs font-bold">
                          <div className="w-6 h-6 flex items-center justify-center rounded-lg sketch-border-thin bg-[#22c55e] text-white">
                            <span className="material-symbols-outlined text-xs">smart_toy</span>
                          </div>
                          <span>Idle / Available</span>
                       </div>
                       <div className="flex items-center gap-3 text-xs font-bold">
                          <div className="w-6 h-6 flex items-center justify-center rounded-lg sketch-border-thin bg-[#f59e0b] text-white">
                            <span className="material-symbols-outlined text-xs">local_shipping</span>
                          </div>
                          <span>Delivering Order</span>
                       </div>
                    </div>
                  </div>
                </div>
              </SketchCard>

              <SketchCard rotate={false} className="bg-surface-container-low">
                <h3 className="font-headline-md text-xl mb-4">Grid Stats</h3>
                <div className="space-y-2">
                   <div className="flex justify-between text-xs font-bold">
                      <span>Live Units:</span>
                       <span className="font-black">{robots.length}</span>
                   </div>
                   <div className="flex justify-between text-xs font-bold">
                      <span>Active Nodes:</span>
                       <span className="font-black">{nodes.length}</span>
                   </div>
                </div>
              </SketchCard>
            </div>

            {/* Map Canvas */}
            <div className="xl:col-span-3">
              <SketchCard shadow rotate={false} className="p-2 overflow-hidden bg-surface-container h-[600px] relative">
                <APIProvider apiKey={GOOGLE_MAPS_API_KEY}>
                  <Map
                    style={{ width: '100%', height: '100%' }}
                    defaultCenter={DEFAULT_CENTER}
                    defaultZoom={12}
                    mapId="DEMO_MAP_ID"
                    disableDefaultUI={true}
                  >
                    <MapContent 
                      robots={robots} 
                      nodes={nodes} 
                      friends={friends}
                      userLocation={userLocation} 
                      viewMode={viewMode}
                      selectedRobotId={selectedRobotId}
                      user={user}
                      navigate={navigate}
                    />
                  </Map>
                </APIProvider>
              </SketchCard>
            </div>
          </div>
        )}

        {/* Robot List */}
        <div className="mt-8">
           <h2 className="font-headline-md text-2xl mb-6">Fleet Telemetry Feed</h2>
           <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {robots.map((robot, i) => (
                <SketchCard key={robot.id} rotate={i % 2 === 1} className="hover:bg-surface-container transition-colors">
                   <div className="flex justify-between items-center mb-4">
                      <span className="material-symbols-outlined text-3xl">smart_toy</span>
                      <div className="px-2 py-0.5 sketch-border-thin text-[8px] font-black uppercase bg-surface-variant">
                         {robot.batteryLevel}% Energy
                      </div>
                   </div>
                   <h4 className="font-headline-md text-lg font-black">{robot.name}</h4>
                   <p className="font-label-md text-[10px] uppercase font-black opacity-40 mb-4">{robot.statusName}</p>
                   
                   <div className="space-y-1 text-[10px] font-bold italic opacity-70">
                      {robot.currentNodeName && (
                        <div className="flex items-center gap-1">
                           <span className="material-symbols-outlined text-xs">location_on</span>
                           {robot.currentNodeName}
                        </div>
                      )}
                      {robot.targetNodeName && (
                        <div className="flex items-center gap-1">
                           <span className="material-symbols-outlined text-xs">rocket</span>
                           {robot.targetNodeName}
                        </div>
                      )}
                   </div>
                </SketchCard>
              ))}
           </div>
        </div>
      </div>
    </Layout>
  );
}

export default MapPage;
