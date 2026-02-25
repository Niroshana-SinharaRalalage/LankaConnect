/**
 * Marketplace items data - sourced from LankaConnectUSA Facebook page posts.
 * Each item links to the corresponding Facebook post.
 *
 * To add/edit items: update this file and redeploy.
 * Images are stored in /public/images/marketplace/
 *
 * NOTE: Supplier/vendor names are intentionally hidden from customers.
 * Images are extracted from the master document in document order,
 * matching the FB link order in the docx.
 */

export interface MarketplaceItem {
  id: string;
  productName: string;
  image: string;
  fbPostUrl: string;
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
};

/** Individual marketplace items - one per Facebook post, in document order */
export const MARKETPLACE_ITEMS: MarketplaceItem[] = [
  {
    id: 'item-01',
    productName: 'Batik Family Kits Collection',
    image: '/images/marketplace/item-01.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1CK7fmhWH8/',
  },
  {
    id: 'item-02',
    productName: 'Batik Dresses & Modern Styles',
    image: '/images/marketplace/item-02.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1Fh1pc2gJA/',
  },
  {
    id: 'item-03',
    productName: 'Batik Couple Sets',
    image: '/images/marketplace/item-03.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1ANEsPY3nd/',
  },
  {
    id: 'item-04',
    productName: 'Batik Couple Sets - Premium',
    image: '/images/marketplace/item-04.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1AfiZYwPQg/',
  },
  {
    id: 'item-05',
    productName: 'Ladies Batik Collection',
    image: '/images/marketplace/item-05.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1GP514N4QV/',
  },
  {
    id: 'item-06',
    productName: 'Printed Family Kit - White & Red',
    image: '/images/marketplace/item-06.png',
    fbPostUrl: 'https://www.facebook.com/share/p/17wyRiU2Ds/',
  },
  {
    id: 'item-07',
    productName: 'Printed Family Kit - Floral',
    image: '/images/marketplace/item-07.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1CBQmR1zfH/',
  },
  {
    id: 'item-08',
    productName: 'Batik Sarong & Family Collection',
    image: '/images/marketplace/item-08.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1DazmTWwpa/',
  },
  {
    id: 'item-09',
    productName: 'Batik Kaftan Dresses',
    image: '/images/marketplace/item-09.png',
    fbPostUrl: 'https://www.facebook.com/share/p/18Hzfbm1Fs/',
  },
  {
    id: 'item-10',
    productName: 'Batik Saree Collection',
    image: '/images/marketplace/item-10.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1DmK8KCTRM/',
  },
  {
    id: 'item-11',
    productName: 'Batik Sarees - Designer',
    image: '/images/marketplace/item-11.png',
    fbPostUrl: 'https://www.facebook.com/share/p/16PhJhmjS2/',
  },
  {
    id: 'item-12',
    productName: 'Batik Poncho Tops',
    image: '/images/marketplace/item-12.png',
    fbPostUrl: 'https://www.facebook.com/share/p/17awEHzDsW/',
  },
  {
    id: 'item-13',
    productName: 'Batik Short Dresses',
    image: '/images/marketplace/item-13.png',
    fbPostUrl: 'https://www.facebook.com/share/p/18HoNsQuzq/',
  },
  {
    id: 'item-14',
    productName: 'Batik Maxi Dresses',
    image: '/images/marketplace/item-14.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1H2dtj89ib/',
  },
  {
    id: 'item-15',
    productName: 'Batik Kaftan Collection',
    image: '/images/marketplace/item-15.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1C1nTDGgNm/',
  },
  {
    id: 'item-16',
    productName: 'Batik Saree & Osari Collection',
    image: '/images/marketplace/item-16.png',
    fbPostUrl: 'https://www.facebook.com/share/p/1DCFgGWeTo/',
  },
];
