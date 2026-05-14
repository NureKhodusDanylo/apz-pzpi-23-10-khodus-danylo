import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { friendshipAPI, userAPI, BASE_URL } from '../lib/api';
import { SketchCard, SketchButton, SketchInput, SketchDivider, SketchAvatar } from '../components/common/SketchComponents';
import toast from 'react-hot-toast';

const FriendsPage = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<any[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!searchQuery.trim()) return;
    
    setIsSearching(true);
    try {
      const response = await userAPI.searchUsers(searchQuery);
      setSearchResults(response.data);
    } catch (err: any) {
      console.error('Search error:', err);
      const errorMessage = err.response?.data?.error || 'Search failed';
      toast.error(errorMessage);
    } finally {
      setIsSearching(false);
    }
  };

  const sendRequestMutation = useMutation({
    mutationFn: (data: { targetUserId?: number, emailOrPhone?: string }) => friendshipAPI.sendRequest({
      targetUserId: data.targetUserId,
      targetEmailOrPhone: data.emailOrPhone
    }),
    onSuccess: () => {
      toast.success('Friend request sent!');
      queryClient.invalidateQueries({ queryKey: ['friend-requests'] });
      // Remove from search results if it was there
      setSearchResults(prev => prev.filter(u => !u.id)); // Actually we should check which one was sent
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Failed to send request');
    },
  });

  const handleSendRequestById = (userId: number) => {
    sendRequestMutation.mutate({ targetUserId: userId });
  };

  const handleSendRequestByContact = (e: React.FormEvent) => {
    e.preventDefault();
    if (!searchQuery.trim()) return;
    sendRequestMutation.mutate({ emailOrPhone: searchQuery });
  };

  const { data: friends, isLoading: isLoadingFriends } = useQuery({
    queryKey: ['friends'],
    queryFn: async () => {
      const response = await friendshipAPI.getFriends();
      return response.data;
    },
  });

  const { data: pendingRequests } = useQuery({
    queryKey: ['friend-requests'],
    queryFn: async () => {
      const response = await friendshipAPI.getPendingRequests();
      return response.data;
    },
  });

  const acceptRequestMutation = useMutation({
    mutationFn: (requestId: number) => friendshipAPI.acceptRequest(requestId),
    onSuccess: () => {
      toast.success('Request accepted!');
      queryClient.invalidateQueries({ queryKey: ['friends'] });
      queryClient.invalidateQueries({ queryKey: ['friend-requests'] });
    },
  });

  const rejectRequestMutation = useMutation({
    mutationFn: (requestId: number) => friendshipAPI.rejectRequest(requestId),
    onSuccess: () => {
      toast.success('Request rejected');
      queryClient.invalidateQueries({ queryKey: ['friend-requests'] });
    },
  });

  const removeFriendMutation = useMutation({
    mutationFn: (friendId: number) => friendshipAPI.removeFriend(friendId),
    onSuccess: () => {
      toast.success('Friend removed');
      queryClient.invalidateQueries({ queryKey: ['friends'] });
    },
  });

  const getPhotoUrl = (path?: string) => {
    if (!path) return null;
    if (path.startsWith('http')) return path;
    return `${BASE_URL}/${path.replace(/\\/g, '/')}`.replace(/([^:]\/)\/+/g, "$1");
  };

  return (
    <Layout>
      <div className="max-w-4xl mx-auto space-y-8">
        <header className="flex flex-col gap-2 transform -rotate-1">
          <h1 className="font-headline-lg text-4xl">Your Fleet Network</h1>
          <p className="text-on-surface-variant font-body-lg">Manage your delivery partners and friends.</p>
        </header>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {/* Add Friend Section */}
          <div className="md:col-span-1 space-y-6">
            <SketchCard className="p-6 bg-surface-container-high" rotate={1}>
              <h2 className="font-headline-md text-xl mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined">person_search</span>
                Find Friends
              </h2>
              <form onSubmit={handleSearch} className="space-y-4">
                <div className="relative">
                  <SketchInput
                    label="Search by Name"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    placeholder="Enter name..."
                    required
                  />
                  <button 
                    type="submit"
                    className="absolute right-2 top-9 p-1 hover:bg-surface-container-highest rounded-full transition-colors"
                  >
                    <span className="material-symbols-outlined">search</span>
                  </button>
                </div>
              </form>

              {/* Search Results */}
              {isSearching ? (
                <div className="mt-6 flex justify-center py-4">
                  <span className="material-symbols-outlined animate-spin text-2xl opacity-40">sync</span>
                </div>
              ) : searchResults.length > 0 ? (
                <div className="mt-6 space-y-3">
                  <p className="text-[10px] uppercase font-bold opacity-40 px-1">Results</p>
                  {searchResults.map(user => (
                    <div key={user.id} className="flex items-center justify-between p-2 bg-surface-container rounded-lg sketch-border-thin group hover:bg-surface-container-low transition-colors">
                      <div className="overflow-hidden">
                        <p className="font-bold text-sm truncate">{user.userName}</p>
                        <p className="text-[10px] opacity-60 truncate">{user.address || 'No address'}</p>
                      </div>
                      <SketchButton 
                        size="sm" 
                        variant="secondary"
                        className="py-1 px-2 text-[10px]"
                        onClick={() => handleSendRequestById(user.id)}
                        isLoading={sendRequestMutation.isPending && sendRequestMutation.variables?.targetUserId === user.id}
                      >
                        Add
                      </SketchButton>
                    </div>
                  ))}
                  <button 
                    onClick={() => setSearchResults([])}
                    className="w-full text-center text-[10px] opacity-40 hover:opacity-100 transition-opacity"
                  >
                    Clear Results
                  </button>
                </div>
              ) : searchQuery && !isSearching && (
                 <div className="mt-6 text-center py-4 opacity-40 italic text-xs">
                    No users found matching "{searchQuery}"
                 </div>
              )}
            </SketchCard>

            {pendingRequests && pendingRequests.length > 0 && (
              <div className="space-y-4">
                <h2 className="font-headline-md text-xl px-2">Pending Requests</h2>
                {pendingRequests.map((req) => (
                  <SketchCard key={req.id} className="p-4 bg-secondary-container/10" rotate={-1}>
                    <div className="flex items-center gap-3 mb-3">
                      <SketchAvatar 
                        src={getPhotoUrl(req.senderPhotoUrl)} 
                        alt={req.senderName} 
                        size="sm" 
                      />
                      <div className="overflow-hidden">
                        <p className="font-bold truncate">{req.senderName}</p>
                        <p className="text-xs opacity-60 truncate">{req.senderEmail}</p>
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <SketchButton 
                        size="sm" 
                        className="flex-1 text-xs py-1" 
                        onClick={() => acceptRequestMutation.mutate(req.id)}
                        isLoading={acceptRequestMutation.isPending}
                      >
                        Accept
                      </SketchButton>
                      <SketchButton 
                        size="sm" 
                        variant="secondary" 
                        className="flex-1 text-xs py-1"
                        onClick={() => rejectRequestMutation.mutate(req.id)}
                        isLoading={rejectRequestMutation.isPending}
                      >
                        Reject
                      </SketchButton>
                    </div>
                  </SketchCard>
                ))}
              </div>
            )}
          </div>

          {/* Friends List Section */}
          <div className="md:col-span-2 space-y-4">
            <h2 className="font-headline-md text-2xl px-2">My Friends</h2>
            {isLoadingFriends ? (
              <div className="text-center py-12">
                <span className="material-symbols-outlined animate-spin text-4xl">sync</span>
              </div>
            ) : !friends || friends.length === 0 ? (
              <SketchCard className="p-12 text-center opacity-50 italic">
                <span className="material-symbols-outlined text-6xl mb-4">group_off</span>
                <p>No friends found. Start adding some!</p>
              </SketchCard>
            ) : (
              <div className="grid grid-cols-1 gap-4">
                {friends.map((friend, index) => (
                  <SketchCard key={friend.id} className="p-4 flex flex-col sm:flex-row items-center gap-4 hover:bg-surface-container-low transition-colors">
                    <SketchAvatar 
                      src={getPhotoUrl(friend.profilePhotoUrl)} 
                      alt={friend.userName} 
                      size="md" 
                      rotate={index % 2 === 0 ? 3 : -3}
                    />
                    <div className="flex-1 text-center sm:text-left">
                      <h3 className="font-headline-sm text-lg font-black">{friend.userName}</h3>
                      <p className="text-sm text-on-surface-variant">{friend.email}</p>
                      <p className="text-xs text-on-surface-variant/70">{friend.phoneNumber}</p>
                    </div>
                    <div className="flex gap-3">
                      <SketchButton 
                        variant="secondary" 
                        icon="send" 
                        className="text-sm"
                        onClick={() => navigate(`/orders/create?recipientId=${friend.id}`)}
                      >
                        Send Gift
                      </SketchButton>
                      <button 
                        className="p-2 text-error hover:bg-error/10 rounded-full transition-colors"
                        onClick={() => {
                          if (window.confirm(`Remove ${friend.userName} from friends?`)) {
                            removeFriendMutation.mutate(friend.id);
                          }
                        }}
                      >
                        <span className="material-symbols-outlined">person_remove</span>
                      </button>
                    </div>
                  </SketchCard>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </Layout>
  );
};

export default FriendsPage;
