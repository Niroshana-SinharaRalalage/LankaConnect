/**
 * Marketplace items data - sourced from LankaConnectUSA Facebook page posts.
 * Each item links to the corresponding Facebook post.
 *
 * To add/edit items: update this file and redeploy.
 * Images are stored in /public/images/marketplace/
 *
 * NOTE: Supplier/vendor names are intentionally hidden from customers.
 */

export interface MarketplaceItem {
  id: string;
  productName: string;
  image: string;
  fbPostUrl: string;
  priceRange: string;
}

/** Common description displayed on the marketplace page for all items */
export const MARKETPLACE_COMMON_DESCRIPTION = {
  title: '2026 Sinhala & Tamil New Year Handmade Batik and Printed Collection',
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
    id: 'batik-saree-red',
    productName: 'Batik Saree - Red Floral',
    image: '/images/marketplace/smartbatik.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1CK7fmhWH8/',
    priceRange: '$34.99 - $45.00',
  },
  {
    id: 'couple-set-red-classic',
    productName: 'Couple Set - Red Classic Batik',
    image: '/images/marketplace/ceylone-batik.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1Fh1pc2gJA/',
    priceRange: '$19.90 - $25.90',
  },
  {
    id: 'family-kit-white-red',
    productName: 'Family Kit - White & Red Printed',
    image: '/images/marketplace/rs-batik.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1ANEsPY3nd/',
    priceRange: '$24.90 - $39.90',
  },
  {
    id: 'family-kit-red-floral',
    productName: 'Family Kit - Red Floral Batik',
    image: '/images/marketplace/orina-batik.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1AfiZYwPQg/',
    priceRange: '$19.90 - $39.90',
  },
  {
    id: 'couple-set-blue-white',
    productName: 'Couple Set - Blue & White Batik',
    image: '/images/marketplace/sorabora.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1GP514N4QV/',
    priceRange: '$29.90 - $39.90',
  },
  {
    id: 'family-kit-maroon-white',
    productName: 'Family Kit - Maroon & White',
    image: '/images/marketplace/st-fashions.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/17wyRiU2Ds/',
    priceRange: '$29.90 - $39.90',
  },
  {
    id: 'couple-set-red-poinsettia',
    productName: 'Couple Set - Red Poinsettia',
    image: '/images/marketplace/seam-and-color.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1CBQmR1zfH/',
    priceRange: '$24.90 - $39.90',
  },
  {
    id: 'crop-top-lungi-teal',
    productName: 'Crop Top & Lungi - Teal Batik',
    image: '/images/marketplace/seam-and-color-2.jpg',
    fbPostUrl: 'https://www.facebook.com/share/p/1DazmTWwpa/',
    priceRange: '$24.90 - $39.90',
  },
];
