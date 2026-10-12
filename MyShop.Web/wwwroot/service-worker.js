// Service Worker for PWA
  const CACHE_NAME = 'koochino-pwa-v6';
const urlsToCache = [
  '/offline.html',
  '/css/site.css',
  '/css/style.css',
  '/css/font.css',
  '/css/dashboard.css',
  '/img/weblogo.png',
  '_content/MudBlazor/MudBlazor.min.css',
  '_content/MudBlazor/MudBlazor.min.js'
];
// NOTE: Do NOT cache Blazor Server files (_framework/, _blazor/) as they need real-time connection

// Install event - cache resources
self.addEventListener('install', event => {
  console.log('[ServiceWorker] Install');
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('[ServiceWorker] Caching app shell');
        return cache.addAll(urlsToCache);
      })
      .catch(err => {
        console.log('[ServiceWorker] Cache failed:', err);
      })
  );
  self.skipWaiting();
});

// Activate event - clean up old caches
self.addEventListener('activate', event => {
  console.log('[ServiceWorker] Activate');
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cacheName => {
          if (cacheName !== CACHE_NAME) {
            console.log('[ServiceWorker] Removing old cache', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    })
  );
  return self.clients.claim();
});

// Fetch event - serve from cache, fallback to network
self.addEventListener('fetch', event => {
  const requestUrl = event.request.url;
  
  // IMPORTANT: Skip ALL Blazor Server and SignalR requests immediately
  // These must NOT be intercepted by Service Worker
  if (requestUrl.includes('/_blazor') || 
      requestUrl.includes('/_framework/') ||
      requestUrl.includes('/hubs/') ||
      requestUrl.includes('blazor.server.js') ||
      requestUrl.includes('SignalR') ||
      requestUrl.includes('signalr') ||
      requestUrl.includes('negotiate') ||
      requestUrl.includes('browserLinkSignalR') ||
      requestUrl.includes('browserLink') ||
      requestUrl.includes('localhost:49990')) {
    // Don't intercept - let browser handle it directly
    return;
  }

  if (requestUrl.includes('/commerce/payment') ||
      requestUrl.includes('/checkout')) {
    return;
  }

  // Skip non-GET requests
  if (event.request.method !== 'GET') {
    return;
  }

  // Skip WebSocket connections
  if (requestUrl.startsWith('ws://') || requestUrl.startsWith('wss://')) {
    return;
  }

  // Skip cross-origin requests
  try {
    const url = new URL(requestUrl);
    if (url.origin !== self.location.origin) {
      return;
    }
  } catch (e) {
    // Invalid URL, skip it
    return;
  }

  const isStaticAsset = requestUrl.match(/\.(css|js|png|jpg|jpeg|gif|svg|ico|woff|woff2|ttf|eot)$/i);
  const isPageRequest = event.request.mode === 'navigate';

  // برای درخواست صفحه (navigate): اول شبکه، در صورت قطع اینترنت صفحه آفلاین
  if (isPageRequest) {
    event.respondWith(
      fetch(event.request)
        .then(function (response) {
          return response;
        })
        .catch(function () {
          return caches.match('/offline.html').then(function (cached) {
            return cached || new Response('Offline', { status: 503, statusText: 'Service Unavailable' });
          });
        })
    );
    return;
  }

  // برای استاتیک: اول کش، بعد شبکه
  event.respondWith(
    caches.match(event.request)
      .then(response => {
        if (response) return response;
        return fetch(event.request)
          .then(response => {
            if (!response || response.status !== 200 || response.type !== 'basic') {
              return response;
            }
            const responseToCache = response.clone();
            caches.open(CACHE_NAME)
              .then(cache => cache.put(event.request, responseToCache))
              .catch(() => {});
            return response;
          })
          .catch(() => new Response('Network error', { status: 503 }));
      })
      .catch(() => new Response('Cache error', { status: 503 }))
  );
});
