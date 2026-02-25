/**
 * Marketplace items data - sourced from LankaConnectUSA Facebook page posts.
 * Each item links to the corresponding Facebook post.
 *
 * To add/edit items: update this file and redeploy.
 * Images are stored in /public/images/marketplace/
 */

export interface MarketplaceItem {
  id: string;
  vendor: string;
  image: string;
  fbPostUrl: string;
  priceRange: string;
}

/** Common description displayed on the marketplace page for all items */
export const MARKETPLACE_COMMON_DESCRIPTION = {
  title: 'Handmade Batik Family Kit',
  preOrderDeadline: 'March 01, 2026',
  contact: {
    whatsapp: '(234) 359-9194',
    facebook: 'https://www.facebook.com/LankaConnectUSA/',
  },
  details: [
    'Sizes and Colors can be customized. Can be made into family kits.',
    'Adults Sizes: S, M, L, XL, 2XL, 3XL',
    'Boy/Girl Ages: 3-5, 6-8, 9-11, 12-14',
  ],
  priceList: [
    { label: 'Gents Shirt', price: '$19.90 (S-XL), $25.90 (2XL-3XL)' },
    { label: 'Gents Sarong', price: '$19.90 (S-XL), $25.90 (2XL-3XL)' },
    { label: 'Ladies Blouse/Crop Top', price: '$19.90 (S-XL), $25.90 (2XL-3XL)' },
    { label: 'Ladies Lungi', price: '$19.90 (S-XL), $25.90 (2XL-3XL)' },
    { label: 'Boys Shirt', price: '$14.90 (3-10yrs), $19.90 (11-14yrs)' },
    { label: 'Boys Sarong', price: '$14.90 (3-10yrs), $19.90 (11-14yrs)' },
    { label: 'Girls Crop Top', price: '$14.90 (3-10yrs), $19.90 (11-14yrs)' },
    { label: 'Girls Lungi', price: '$14.90 (3-10yrs), $19.90 (11-14yrs)' },
  ],
};

/** Individual marketplace items - one per Facebook post */
export const MARKETPLACE_ITEMS: MarketplaceItem[] = [
  {
    id: 'smartbatik',
    vendor: 'SmartBatik',
    image: '/images/marketplace/smartbatik.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1CK7fmhWH8/',
    priceRange: '$34.99 - $45.00',
  },
  {
    id: 'ceylone-batik',
    vendor: 'Ceylone Batik',
    image: '/images/marketplace/ceylone-batik.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1Fh1pc2gJA/',
    priceRange: '$19.90 - $25.90',
  },
  {
    id: 'rs-batik',
    vendor: 'RS Batik',
    image: '/images/marketplace/rs-batik.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1ANEsPY3nd/',
    priceRange: '$24.90 - $39.90',
  },
  {
    id: 'orina-batik',
    vendor: 'Orina Batik and Fashion',
    image: '/images/marketplace/orina-batik.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1AfiZYwPQg/',
    priceRange: '$19.90 - $39.90',
  },
  {
    id: 'sorabora',
    vendor: 'Sorabora',
    image: '/images/marketplace/sorabora.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1GP514N4QV/',
    priceRange: '$29.90 - $39.90',
  },
  {
    id: 'st-fashions',
    vendor: 'ST Fashions',
    image: '/images/marketplace/st-fashions.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/17wyRiU2Ds/',
    priceRange: '$29.90 - $39.90',
  },
  {
    id: 'seam-and-color',
    vendor: 'Seam And Color',
    image: '/images/marketplace/seam-and-color.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1CBQmR1zfH/',
    priceRange: '$24.90 - $39.90',
  },
  {
    id: 'seam-and-color-2',
    vendor: 'Seam And Color',
    image: '/images/marketplace/seam-and-color-2.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1DazmTWwpa/',
    priceRange: '$24.90 - $39.90',
  },
];
