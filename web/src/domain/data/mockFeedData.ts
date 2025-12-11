import { FeedItem, createFeedItem } from '../models/FeedItem';

/**
 * Mock Feed Data for LankaConnect
 * Realistic Sri Lankan American community content across Ohio metro areas
 */

export const mockFeedItems: FeedItem[] = [
  // EVENTS (6 items)
  createFeedItem({
    id: 'event-001',
    type: 'event',
    author: { name: 'Sri Lankan Association of Cleveland', initials: 'SA' },
    timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000), // 2 hours ago
    location: 'Cleveland, OH',
    title: 'Sinhala & Tamil New Year Celebration 2025',
    description: 'Join us for the biggest New Year celebration in Ohio! Traditional games (kana mutti, placing the head on the pillow), authentic Sri Lankan cuisine, cultural performances, and a special program for children. Free admission for kids under 12.',
    actions: [
      { type: 'like', icon: '👍', label: 'Like', count: 245, active: false },
      { type: 'comment', icon: '💬', label: 'Comment', count: 32 },
      { type: 'share', icon: '📤', label: 'Share', count: 18 }
    ],
    metadata: {
      type: 'event',
      date: 'April 13, 2025',
      time: '11:00 AM - 6:00 PM',
      venue: 'Cleveland Cultural Gardens',
      interestedCount: 245,
      commentCount: 32
    }
  }),

  createFeedItem({
    id: 'event-002',
    type: 'event',
    author: { name: 'Kamal Perera', initials: 'KP', avatar: 'https://i.pravatar.cc/150?img=12' },
    timestamp: new Date(Date.now() - 5 * 60 * 60 * 1000), // 5 hours ago
    location: 'Columbus, OH',
    title: 'Vesak Day Lantern Making Workshop',
    description: 'Learn to create traditional Vesak lanterns (kuudu) with master craftsman from Colombo. All materials provided. Perfect for families! We\'ll also have a short meditation session and serve milk rice.',
    actions: [
      { type: 'like', icon: '👍', label: 'Like', count: 89, active: true },
      { type: 'comment', icon: '💬', label: 'Comment', count: 15 },
      { type: 'share', icon: '📤', label: 'Share', count: 7 }
    ],
    metadata: {
      type: 'event',
      date: 'May 3, 2025',
      time: '2:00 PM - 5:00 PM',
      venue: 'Columbus Buddhist Center',
      interestedCount: 89,
      commentCount: 15
    }
  }),

  createFeedItem({
    id: 'event-003',
    type: 'event',
    author: { name: 'Ohio Cricket League', initials: 'OC' },
    timestamp: new Date(Date.now() - 12 * 60 * 60 * 1000), // 12 hours ago
    location: 'Cincinnati, OH',
    title: 'Sri Lanka vs India - Cricket Viewing Party',
    description: 'Watch the T20 World Cup match live on big screen! Kottu and hoppers will be served. Wear your team colors! Limited seating - RSVP required.',
    actions: [
      { type: 'like', icon: '👍', label: 'Like', count: 156, active: false },
      { type: 'comment', icon: '💬', label: 'Comment', count: 43 },
      { type: 'share', icon: '📤', label: 'Share', count: 12 }
    ],
    metadata: {
      type: 'event',
      date: 'June 15, 2025',
      time: '7:30 PM - 11:00 PM',
      venue: 'Cincinnati Sports Bar & Grill',
      interestedCount: 156,
      commentCount: 43
    }
  }),

  createFeedItem({
    id: 'event-004',
    type: 'event',
    author: { name: 'Nadeesha Silva', initials: 'NS', avatar: 'https://i.pravatar.cc/150?img=45' },
    timestamp: new Date(Date.now() - 24 * 60 * 60 * 1000), // 1 day ago
    location: 'Akron, OH',
    title: 'Baila Night - Live Music & Dancing',
    description: 'Get ready to dance! Live Baila band from Toronto featuring classic hits and modern fusion. Full bar and Sri Lankan appetizers available. Age 21+',
    actions: [
      { type: 'like', icon: '👍', label: 'Like', count: 203, active: true },
      { type: 'comment', icon: '💬', label: 'Comment', count: 28 },
      { type: 'share', icon: '📤', label: 'Share', count: 34 }
    ],
    metadata: {
      type: 'event',
      date: 'March 29, 2025',
      time: '8:00 PM - 1:00 AM',
      venue: 'Akron Convention Center',
      interestedCount: 203,
      commentCount: 28
    }
  }),

  createFeedItem({
    id: 'event-005',
    type: 'event',
    author: { name: 'Lanka Heritage Foundation', initials: 'LH' },
    timestamp: new Date(Date.now() - 36 * 60 * 60 * 1000), // 1.5 days ago
    location: 'Aurora, OH',
    title: 'Kandyan Dance Performance & Workshop',
    description: 'Professional Kandyan dancers from Sri Lanka! Watch a mesmerizing performance followed by an interactive workshop. Learn basic moves and the history of this ancient art form.',
    actions: [
      { type: 'like', icon: '👍', label: 'Like', count: 134, active: false },
      { type: 'comment', icon: '💬', label: 'Comment', count: 19 },
      { type: 'share', icon: '📤', label: 'Share', count: 15 }
    ],
    metadata: {
      type: 'event',
      date: 'April 20, 2025',
      time: '3:00 PM - 6:00 PM',
      venue: 'Aurora Community Center',
      interestedCount: 134,
      commentCount: 19
    }
  }),

  createFeedItem({
    id: 'event-006',
    type: 'event',
    author: { name: 'Rohan Fernando', initials: 'RF', avatar: 'https://i.pravatar.cc/150?img=33' },
    timestamp: new Date(Date.now() - 48 * 60 * 60 * 1000), // 2 days ago
    location: 'Westlake, OH',
    title: 'Sri Lankan Food Festival 2025',
    description: 'Taste of Sri Lanka! 15+ food vendors serving everything from string hoppers to watalappan. Live cooking demonstrations, cultural performances, and a kids play area. Rain or shine!',
    actions: [
      { type: 'like', icon: '👍', label: 'Like', count: 412, active: false },
      { type: 'comment', icon: '💬', label: 'Comment', count: 67 },
      { type: 'share', icon: '📤', label: 'Share', count: 89 }
    ],
    metadata: {
      type: 'event',
      date: 'May 25, 2025',
      time: '10:00 AM - 8:00 PM',
      venue: 'Westlake Recreation Center',
      interestedCount: 412,
      commentCount: 67
    }
  }),

  // BUSINESSES (5 items)
  createFeedItem({
    id: 'business-001',
    type: 'business',
    author: { name: 'Ceylon Spice Kitchen', initials: 'CS' },
    timestamp: new Date(Date.now() - 3 * 60 * 60 * 1000), // 3 hours ago
    location: 'Cleveland, OH',
    title: 'Grand Opening - Authentic Sri Lankan Restaurant',
    description: 'Finally! Authentic hoppers, kottu, and lamprais in Cleveland. Family recipes passed down three generations. Dine-in, takeout, and catering available. 20% off this weekend with code CEYLON20.',
    actions: [
      { type: 'like', icon: '❤️', label: 'Like', count: 289, active: true },
      { type: 'comment', icon: '💬', label: 'Comment', count: 54 },
      { type: 'share', icon: '📤', label: 'Share', count: 42 }
    ],
    metadata: {
      type: 'business',
      category: 'Restaurant',
      rating: 4.8,
      reviewCount: 23,
      priceRange: '$$',
      hours: 'Tue-Sun 11AM-9PM',
      likesCount: 289,
      commentsCount: 54
    }
  }),

  createFeedItem({
    id: 'business-002',
    type: 'business',
    author: { name: 'Lanka Grocers', initials: 'LG' },
    timestamp: new Date(Date.now() - 8 * 60 * 60 * 1000), // 8 hours ago
    location: 'Columbus, OH',
    title: 'Fresh Stock Arrived - MDK, Maliban & More!',
    description: 'New shipment just landed! Fresh curry leaves, pandan leaves, goraka, MDK instant noodles, Maliban biscuits, Lion lager, and frozen fish from Sri Lanka. Also carrying fresh vegetables for your New Year cooking.',
    actions: [
      { type: 'like', icon: '❤️', label: 'Like', count: 167, active: false },
      { type: 'comment', icon: '💬', label: 'Comment', count: 28 },
      { type: 'share', icon: '📤', label: 'Share', count: 19 }
    ],
    metadata: {
      type: 'business',
      category: 'Grocery Store',
      rating: 4.9,
      reviewCount: 156,
      priceRange: '$$',
      hours: 'Mon-Sat 9AM-8PM',
      likesCount: 167,
      commentsCount: 28
    }
  }),

  createFeedItem({
    id: 'business-003',
    type: 'business',
    author: { name: 'Thilini\'s Catering', initials: 'TC', avatar: 'https://i.pravatar.cc/150?img=28' },
    timestamp: new Date(Date.now() - 18 * 60 * 60 * 1000), // 18 hours ago
    location: 'Cincinnati, OH',
    title: 'Catering for All Occasions',
    description: 'Authentic Sri Lankan catering for weddings, parties, and corporate events. Specializing in traditional rice & curry, lamprais boxes, and custom menus. Book your New Year party now! Free tasting for events over 50 people.',
    actions: [
      { type: 'like', icon: '❤️', label: 'Like', count: 98, active: false },
      { type: 'comment', icon: '💬', label: 'Comment', count: 12 },
      { type: 'share', icon: '📤', label: 'Share', count: 8 }
    ],
    metadata: {
      type: 'business',
      category: 'Catering',
      rating: 5.0,
      reviewCount: 47,
      priceRange: '$$$',
      hours: 'By Appointment',
      likesCount: 98,
      commentsCount: 12
    }
  }),

  createFeedItem({
    id: 'business-004',
    type: 'business',
    author: { name: 'Sinhala School of Ohio', initials: 'SS' },
    timestamp: new Date(Date.now() - 30 * 60 * 60 * 1000), // 1.25 days ago
    location: 'Akron, OH',
    title: 'Summer Classes Now Enrolling',
    description: 'Teach your kids Sinhala! Professional teachers, small class sizes, fun activities. Beginner to advanced levels. Also offering adult conversational classes. Summer session starts June 1st. Early bird discount until March 31st!',
    actions: [
      { type: 'like', icon: '❤️', label: 'Like', count: 145, active: true },
      { type: 'comment', icon: '💬', label: 'Comment', count: 31 },
      { type: 'share', icon: '📤', label: 'Share', count: 25 }
    ],
    metadata: {
      type: 'business',
      category: 'Education',
      rating: 4.9,
      reviewCount: 89,
      priceRange: '$$',
      hours: 'Sat-Sun 9AM-1PM',
      likesCount: 145,
      commentsCount: 31
    }
  }),

  createFeedItem({
    id: 'business-005',
    type: 'business',
    author: { name: 'Island Cuts Barbershop', initials: 'IC' },
    timestamp: new Date(Date.now() - 40 * 60 * 60 * 1000), // 1.67 days ago
    location: 'Dublin, OH',
    title: 'Sri Lankan Barber - All Styles',
    description: 'Walk-ins welcome! Experienced barber from Colombo. Traditional cuts, fades, beard grooming. We understand Asian hair texture. Open 7 days. Special: $15 haircuts on Tuesdays.',
    actions: [
      { type: 'like', icon: '❤️', label: 'Like', count: 76, active: false },
      { type: 'comment', icon: '💬', label: 'Comment', count: 9 },
      { type: 'share', icon: '📤', label: 'Share', count: 5 }
    ],
    metadata: {
      type: 'business',
      category: 'Personal Care',
      rating: 4.7,
      reviewCount: 34,
      priceRange: '$',
      hours: 'Mon-Sun 9AM-7PM',
      likesCount: 76,
      commentsCount: 9
    }
  }),

  // FORUMS (5 items)
  createFeedItem({
    id: 'forum-001',
    type: 'forum',
    author: { name: 'Priya Wickramasinghe', initials: 'PW', avatar: 'https://i.pravatar.cc/150?img=47' },
    timestamp: new Date(Date.now() - 4 * 60 * 60 * 1000), // 4 hours ago
    location: 'Cleveland, OH',
    title: 'Looking for roommate near Case Western',
    description: 'Clean, quiet professional seeking roommate to share 2BR apartment. Prefer vegetarian, non-smoking Sri Lankan. $650/month + utilities. Move-in ready April 1st. Close to RTA and grocery stores.',
    actions: [
      { type: 'like', icon: '👏', label: 'Helpful', count: 23, active: false },
      { type: 'comment', icon: '💬', label: 'Reply', count: 18 },
      { type: 'share', icon: '📤', label: 'Share', count: 6 }
    ],
    metadata: {
      type: 'forum',
      forumName: 'Housing & Roommates',
      category: 'Housing',
      replies: 18,
      views: 234,
      lastActive: new Date(Date.now() - 1 * 60 * 60 * 1000),
      repliesCount: 18,
      helpfulCount: 23
    }
  }),

  createFeedItem({
    id: 'forum-002',
    type: 'forum',
    author: { name: 'Dinesh Kumar', initials: 'DK', avatar: 'https://i.pravatar.cc/150?img=56' },
    timestamp: new Date(Date.now() - 10 * 60 * 60 * 1000), // 10 hours ago
    location: 'Columbus, OH',
    title: 'Software Engineers - Job Opportunities',
    description: 'My company is hiring! Looking for Java developers with 3+ years experience. H1B sponsorship available. Great benefits and work-life balance. DM me for details. Happy to refer qualified candidates.',
    actions: [
      { type: 'like', icon: '👏', label: 'Helpful', count: 156, active: true },
      { type: 'comment', icon: '💬', label: 'Reply', count: 42 },
      { type: 'share', icon: '📤', label: 'Share', count: 38 }
    ],
    metadata: {
      type: 'forum',
      forumName: 'Jobs & Careers',
      category: 'Jobs',
      replies: 42,
      views: 1247,
      lastActive: new Date(Date.now() - 30 * 60 * 1000),
      repliesCount: 42,
      helpfulCount: 156
    }
  }),

  createFeedItem({
    id: 'forum-003',
    type: 'forum',
    author: { name: 'Sanduni Jayawardena', initials: 'SJ', avatar: 'https://i.pravatar.cc/150?img=38' },
    timestamp: new Date(Date.now() - 20 * 60 * 60 * 1000), // 20 hours ago
    location: 'Cincinnati, OH',
    title: 'Best place to buy spices in bulk?',
    description: 'Moving to Cincinnati next month. Where do you all buy Sri Lankan groceries? Looking for curry powder, goraka, pandan, dried fish, etc. Also any recommendations for good dosa places?',
    actions: [
      { type: 'like', icon: '👏', label: 'Helpful', count: 34, active: false },
      { type: 'comment', icon: '💬', label: 'Reply', count: 27 },
      { type: 'share', icon: '📤', label: 'Share', count: 4 }
    ],
    metadata: {
      type: 'forum',
      forumName: 'General Discussion',
      category: 'General Discussion',
      replies: 27,
      views: 456,
      lastActive: new Date(Date.now() - 2 * 60 * 60 * 1000),
      repliesCount: 27,
      helpfulCount: 34
    }
  }),

  createFeedItem({
    id: 'forum-004',
    type: 'forum',
    author: { name: 'Rajith Fernando', initials: 'RF', avatar: 'https://i.pravatar.cc/150?img=14' },
    timestamp: new Date(Date.now() - 32 * 60 * 60 * 1000), // 1.33 days ago
    location: 'Akron, OH',
    title: 'Learning Sinhala - App Recommendations?',
    description: 'My kids (8 and 10) understand Sinhala but can\'t read/write. Any good apps or online resources? We tried a few but they\'re either too advanced or too childish. Also open to tutors in the Akron area.',
    actions: [
      { type: 'like', icon: '👏', label: 'Helpful', count: 67, active: false },
      { type: 'comment', icon: '💬', label: 'Reply', count: 34 },
      { type: 'share', icon: '📤', label: 'Share', count: 12 }
    ],
    metadata: {
      type: 'forum',
      forumName: 'Language & Education',
      category: 'Language Learning',
      replies: 34,
      views: 589,
      lastActive: new Date(Date.now() - 5 * 60 * 60 * 1000),
      repliesCount: 34,
      helpfulCount: 67
    }
  }),

  createFeedItem({
    id: 'forum-005',
    type: 'forum',
    author: { name: 'Chamika Perera', initials: 'CP', avatar: 'https://i.pravatar.cc/150?img=22' },
    timestamp: new Date(Date.now() - 45 * 60 * 60 * 1000), // 1.875 days ago
    location: 'Westlake, OH',
    title: 'Shipping items to Sri Lanka - Best service?',
    description: 'Need to send care packages to family in Colombo. What shipping service do you all use? Looking for reliable and affordable. Also, any restrictions on food items like chocolates and snacks?',
    actions: [
      { type: 'like', icon: '👏', label: 'Helpful', count: 45, active: true },
      { type: 'comment', icon: '💬', label: 'Reply', count: 29 },
      { type: 'share', icon: '📤', label: 'Share', count: 8 }
    ],
    metadata: {
      type: 'forum',
      forumName: 'General Discussion',
      category: 'General Discussion',
      replies: 29,
      views: 378,
      lastActive: new Date(Date.now() - 12 * 60 * 60 * 1000),
      repliesCount: 29,
      helpfulCount: 45
    }
  }),

  // CULTURE (4 items)
  createFeedItem({
    id: 'culture-001',
    type: 'culture',
    author: { name: 'Lanka Heritage Foundation', initials: 'LH' },
    timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000), // 6 hours ago
    location: 'Cleveland, OH',
    title: 'The Art of Sri Lankan Mask Making',
    description: 'Discover the ancient tradition of Kolam masks from Southern Sri Lanka. These intricate wooden masks represent demons, animals, and folklore characters. Used in traditional healing ceremonies and performances. Swipe to see master craftsman at work. 🎭',
    actions: [
      { type: 'like', icon: '❤️', label: 'Love', count: 198, active: true },
      { type: 'comment', icon: '💬', label: 'Comment', count: 24 },
      { type: 'share', icon: '📤', label: 'Share', count: 31 }
    ],
    metadata: {
      type: 'culture',
      category: 'Traditional Arts',
      images: 5,
      videoLength: '3:24',
      tags: ['masks', 'art', 'tradition', 'kolam'],
      resourcesCount: 5,
      repliesCount: 24
    }
  }),

  createFeedItem({
    id: 'culture-002',
    type: 'culture',
    author: { name: 'Nimal Rodrigo', initials: 'NR', avatar: 'https://i.pravatar.cc/150?img=51' },
    timestamp: new Date(Date.now() - 15 * 60 * 60 * 1000), // 15 hours ago
    location: 'Columbus, OH',
    title: 'Teaching Kids About Poya Days',
    description: 'Beautiful way to explain the significance of Full Moon Poya days to American-born kids. Each month commemorates important events in Buddha\'s life. Includes simple activities you can do at home. Perfect for families! 🌕',
    actions: [
      { type: 'like', icon: '❤️', label: 'Love', count: 124, active: false },
      { type: 'comment', icon: '💬', label: 'Comment', count: 16 },
      { type: 'share', icon: '📤', label: 'Share', count: 28 }
    ],
    metadata: {
      type: 'culture',
      category: 'Religion & Spirituality',
      images: 3,
      tags: ['Buddhism', 'poya', 'education', 'kids'],
      resourcesCount: 3,
      repliesCount: 16
    }
  }),

  createFeedItem({
    id: 'culture-003',
    type: 'culture',
    author: { name: 'Ayesha Mohamed', initials: 'AM', avatar: 'https://i.pravatar.cc/150?img=41' },
    timestamp: new Date(Date.now() - 28 * 60 * 60 * 1000), // 1.17 days ago
    location: 'Cincinnati, OH',
    title: 'Ramazan Traditions in Sri Lankan Muslim Community',
    description: 'Sharing our beautiful Ramazan traditions! From wattalappam and faluda to special prayers at mosque. The blend of Sri Lankan and Islamic culture makes it unique. Photos from last year\'s Eid celebration in Colombo. ☪️',
    actions: [
      { type: 'like', icon: '❤️', label: 'Love', count: 167, active: false },
      { type: 'comment', icon: '💬', label: 'Comment', count: 22 },
      { type: 'share', icon: '📤', label: 'Share', count: 19 }
    ],
    metadata: {
      type: 'culture',
      category: 'Religion & Spirituality',
      images: 8,
      tags: ['Islam', 'Ramazan', 'food', 'community'],
      resourcesCount: 8,
      repliesCount: 22
    }
  }),

  createFeedItem({
    id: 'culture-004',
    type: 'culture',
    author: { name: 'Lakshmi Subramaniam', initials: 'LS', avatar: 'https://i.pravatar.cc/150?img=26' },
    timestamp: new Date(Date.now() - 42 * 60 * 60 * 1000), // 1.75 days ago
    location: 'Aurora, OH',
    title: 'Tamil Heritage Month - Bharatanatyam Workshop',
    description: 'Celebrating Tamil culture through classical dance! Learn basic Bharatanatyam steps and mudras. This ancient dance form tells stories through precise hand gestures and expressions. Video shows our students\' performance. 💃',
    actions: [
      { type: 'like', icon: '❤️', label: 'Love', count: 143, active: true },
      { type: 'comment', icon: '💬', label: 'Comment', count: 18 },
      { type: 'share', icon: '📤', label: 'Share', count: 22 }
    ],
    metadata: {
      type: 'culture',
      category: 'Traditional Arts',
      images: 4,
      videoLength: '5:12',
      tags: ['dance', 'Tamil', 'Bharatanatyam', 'culture'],
      resourcesCount: 4,
      repliesCount: 18
    }
  })
];

// Filtered exports by type
export const mockEventItems = mockFeedItems.filter(item => item.type === 'event');
export const mockBusinessItems = mockFeedItems.filter(item => item.type === 'business');
export const mockForumItems = mockFeedItems.filter(item => item.type === 'forum');
export const mockCultureItems = mockFeedItems.filter(item => item.type === 'culture');

// Helper function to get items by location
export const getItemsByLocation = (location: string): FeedItem[] => {
  return mockFeedItems.filter(item => item.location === location);
};

// Helper function to get recent items (last 24 hours)
export const getRecentItems = (): FeedItem[] => {
  const oneDayAgo = Date.now() - 24 * 60 * 60 * 1000;
  return mockFeedItems.filter(item => item.timestamp.getTime() > oneDayAgo);
};

// Helper function to get popular items (high engagement)
export const getPopularItems = (minLikes: number = 150): FeedItem[] => {
  return mockFeedItems.filter(item => {
    const likeAction = item.actions.find(action => action.type === 'like');
    return likeAction && likeAction.count !== undefined && likeAction.count >= minLikes;
  });
};

// Export statistics
export const feedStats = {
  total: mockFeedItems.length,
  byType: {
    event: mockEventItems.length,
    business: mockBusinessItems.length,
    forum: mockForumItems.length,
    culture: mockCultureItems.length
  },
  byLocation: {
    'Cleveland, OH': getItemsByLocation('Cleveland, OH').length,
    'Columbus, OH': getItemsByLocation('Columbus, OH').length,
    'Cincinnati, OH': getItemsByLocation('Cincinnati, OH').length,
    'Akron, OH': getItemsByLocation('Akron, OH').length,
    'Aurora, OH': getItemsByLocation('Aurora, OH').length,
    'Westlake, OH': getItemsByLocation('Westlake, OH').length,
    'Dublin, OH': getItemsByLocation('Dublin, OH').length
  }
};
