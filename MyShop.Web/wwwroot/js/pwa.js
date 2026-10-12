/**
 * PWA Install Prompt - کوچینو
 * منطق نصب مثل نسخهٔ قدیمی: prompt() هم‌زمان با کلیک کاربر
 * + رفع فاصلهٔ پایین کروم در حالت مرورگر
 */
(function () {
  'use strict';

  var STORAGE_KEY_DISMISSED = 'koochino_pwa_dismissed';
  var STORAGE_KEY_INSTALLED = 'koochino_pwa_installed';
  var STORAGE_KEY_TWA = 'koochino_twa';
  var STORAGE_KEY_TWA_LOGGED = 'koochino_twa_logged';
  var BANNER_DELAY_MS = 2000;

  var deferredPrompt = null;
  var bannerEl = null;

  function getBanner() {
    return document.getElementById('pwa-install-banner');
  }

  function getGuide() {
    return document.getElementById('pwa-install-guide');
  }

  function isStandalone() {
    return window.matchMedia('(display-mode: standalone)').matches ||
      window.navigator.standalone === true ||
      (document.referrer || '').indexOf('android-app://') === 0;
  }

  function isTwaContext() {
    try {
      if (new URLSearchParams(window.location.search).get('source') === 'twa') return true;
      if (sessionStorage.getItem(STORAGE_KEY_TWA) === '1') return true;
      if ((document.referrer || '').indexOf('android-app://') === 0) return true;
    } catch (e) {}
    return false;
  }

  function markTwaContext() {
    try {
      if (new URLSearchParams(window.location.search).get('source') === 'twa') {
        sessionStorage.setItem(STORAGE_KEY_TWA, '1');
      }
    } catch (e) {}
  }

  /** فقط موبایل و تبلت — در دسکتاپ بنر نصب نمایش داده نمی‌شود */
  function isMobileOrTablet() {
    var w = window.innerWidth || 0;
    var ua = navigator.userAgent || '';
    if (w <= 1024) return true;
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(ua);
  }

  function isIOS() {
    return /iPad|iPhone|iPod/.test(navigator.userAgent || '') ||
      (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
  }

  function isFirefox() {
    return /Firefox|FxiOS/i.test(navigator.userAgent || '');
  }

  function isAndroid() {
    return /Android/i.test(navigator.userAgent || '');
  }

  function wasDismissed() {
    try {
      return localStorage.getItem(STORAGE_KEY_DISMISSED) === '1';
    } catch (e) {
      return false;
    }
  }

  function setDismissed() {
    try {
      localStorage.setItem(STORAGE_KEY_DISMISSED, '1');
    } catch (e) {}
  }

  function showBanner() {
    if (!isMobileOrTablet()) return;
    if (isTwaContext()) return;
    bannerEl = getBanner();
    if (!bannerEl || isStandalone() || wasDismissed()) return;
    bannerEl.classList.add('pwa-banner-visible');
    bannerEl.setAttribute('aria-hidden', 'false');
  }

  function hideBanner() {
    bannerEl = getBanner();
    if (bannerEl) {
      bannerEl.classList.remove('pwa-banner-visible');
      bannerEl.setAttribute('aria-hidden', 'true');
    }
  }

  function showGuide(html) {
    var box = getGuide();
    if (!box) {
      try {
        var d = document.createElement('div');
        d.innerHTML = html || '';
        window.alert((d.textContent || '').replace(/\s+/g, ' ').trim());
      } catch (e) {}
      return;
    }
    var body = box.querySelector('.pwa-guide-body');
    if (body) body.innerHTML = html;
    box.classList.add('pwa-guide-visible');
    box.setAttribute('aria-hidden', 'false');
  }

  function hideGuide() {
    var box = getGuide();
    if (!box) return;
    box.classList.remove('pwa-guide-visible');
    box.setAttribute('aria-hidden', 'true');
  }

  /** همان الگوی قدیمی که دیالوگ نصب کروم را باز می‌کرد */
  function onInstallClick() {
    if (!deferredPrompt) {
      hideBanner();
      return;
    }
    var promptEvent = deferredPrompt;
    promptEvent.prompt();
    promptEvent.userChoice.then(function (choice) {
      if (choice && choice.outcome === 'accepted') {
        try {
          localStorage.setItem(STORAGE_KEY_INSTALLED, '1');
        } catch (e) {}
        hideBanner();
        hideGuide();
      }
      deferredPrompt = null;
      window.__koochinoCanInstall = false;
    }).catch(function () {
      deferredPrompt = null;
      window.__koochinoCanInstall = false;
    });
  }

  function onDismissClick() {
    setDismissed();
    hideBanner();
  }

  function openManualHelp() {
    hideBanner();
    if (isStandalone()) {
      showGuide('<p>اپ کوچینو از قبل نصب شده است. از آیکون صفحهٔ اصلی گوشی بازش کنید.</p>');
      return;
    }
    if (isIOS()) {
      showGuide(
        '<p><b>نصب در آیفون:</b></p>' +
        '<ol>' +
        '<li>دکمه <b>Share</b> (مربع با فلش) پایین سافاری را بزنید</li>' +
        '<li>گزینه <b>Add to Home Screen</b> را انتخاب کنید</li>' +
        '<li>روی <b>Add</b> بزنید</li>' +
        '</ol>'
      );
      return;
    }
    if (isFirefox()) {
      showGuide(
        '<p><b>نصب در فایرفاکس اندروید:</b></p>' +
        '<ol>' +
        '<li>منوی فایرفاکس (<b>⋮</b>) را باز کنید</li>' +
        '<li>گزینه <b>Install</b> یا <b>Add to Home screen</b> را بزنید</li>' +
        '<li>تأیید کنید تا آیکون روی صفحه اصلی ساخته شود</li>' +
        '</ol>' +
        '<p style="margin-top:8px;color:#6b7280;font-size:0.9rem;">فایرفاکس دیالوگ نصب خودکار مثل کروم ندارد.</p>'
      );
      return;
    }
    // کروم بدون رویداد نصب: فقط راهنمای کوتاه (نه جایگزین دیالوگ)
    showGuide(
      '<p>نصب هنوز آماده نیست. چند ثانیه در سایت بمانید و دوباره «نصب» را بزنید.</p>' +
      '<p style="margin-top:8px;">یا از منوی کروم (<b>⋮</b>) گزینه <b>Install app</b> / «نصب برنامه» را انتخاب کنید.</p>'
    );
  }

  function getDeviceType() {
    var ua = navigator.userAgent || '';
    if (/Android/i.test(ua)) return 'Android';
    if (/iPhone|iPad|iPod/i.test(ua)) return 'iOS';
    if (/Windows/i.test(ua)) return 'Windows';
    if (/Mac/i.test(ua)) return 'Mac';
    if (/Linux/i.test(ua)) return 'Linux';
    return 'Other';
  }

  function getInstallType() {
    return isTwaContext() ? 'twa' : 'pwa';
  }

  function sendPwaInstallLog(installType) {
    try {
      var type = installType || getInstallType();
      var payload = {
        platform: navigator.platform || '',
        language: navigator.language || '',
        timezone: typeof Intl !== 'undefined' && Intl.DateTimeFormat ? Intl.DateTimeFormat().resolvedOptions().timeZone : '',
        deviceType: getDeviceType(),
        screenWidth: screen && screen.width,
        screenHeight: screen && screen.height,
        hardwareConcurrency: navigator.hardwareConcurrency,
        deviceMemory: navigator.deviceMemory,
        extraInfo: JSON.stringify({
          installType: type,
          vendor: navigator.vendor || '',
          maxTouchPoints: navigator.maxTouchPoints,
          cookieEnabled: navigator.cookieEnabled
        })
      };
      fetch('/api/pwa/installed', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      }).catch(function () {});
    } catch (e) {}
  }

  /** رفع فاصله خالی زیر منوی پایین در Chrome اندروید */
  function fixChromeBottomGap() {
    if (isStandalone()) {
      document.documentElement.setAttribute('data-standalone', '1');
      return;
    }
    document.documentElement.setAttribute('data-standalone', '0');

    var meta = document.querySelector('meta[name="viewport"]');
    if (meta) {
      meta.setAttribute('content', 'width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=yes');
    }

    // این جبران فقط برای گپ شناخته‌شدهٔ کروم اندروید لازم است؛ روی iOS/دسکتاپ
    // اجرا نشود چون position:fixed + safe-area-inset از قبل درست کار می‌کند
    // و ری‌پوزیشن کردن با جاوااسکریپت روی اسکرول باعث لرزش منو می‌شود.
    if (!isAndroid()) return;

    var lastBottomInset = null;
    var rafId = null;

    function applyPinNav() {
      rafId = null;
      var nav = document.querySelector('.bottom-nav, .koo-bottomnav');
      if (!nav) return;
      var bottomInset = 0;
      if (window.visualViewport) {
        var vv = window.visualViewport;
        bottomInset = Math.round(window.innerHeight - vv.height - vv.offsetTop);
        if (bottomInset < 0) bottomInset = 0;
        if (bottomInset > 80) bottomInset = 0;
      }
      if (bottomInset === lastBottomInset) return;
      lastBottomInset = bottomInset;
      nav.style.paddingBottom = '0px';
      nav.style.marginBottom = '0px';
      nav.style.bottom = bottomInset + 'px';
    }

    // فقط روی resize واقعی (باز/بسته‌شدن نوار آدرس یا صفحه‌کلید) — نه روی هر
    // تیک اسکرول، که علت اصلی لرزش منو هنگام اسکرول بود
    function schedulePinNav() {
      if (rafId != null) return;
      rafId = window.requestAnimationFrame(applyPinNav);
    }

    schedulePinNav();
    if (window.visualViewport) {
      window.visualViewport.addEventListener('resize', schedulePinNav);
    }
    window.addEventListener('resize', schedulePinNav);
  }

  function logTwaOpenOnce() {
    if (!isTwaContext()) return;
    try {
      if (sessionStorage.getItem(STORAGE_KEY_TWA_LOGGED) === '1') return;
      sessionStorage.setItem(STORAGE_KEY_TWA_LOGGED, '1');
      sendPwaInstallLog('twa');
    } catch (e) {}
  }

  function init() {
    markTwaContext();
    fixChromeBottomGap();
    logTwaOpenOnce();

    if (isStandalone() || isTwaContext()) return;
    if (/\/StoreOwner\/Stores\/Print\//i.test(location.pathname || '')) return;

    bannerEl = getBanner();
    var btnInstall = document.getElementById('pwa-install-btn');
    var btnDismiss = document.getElementById('pwa-dismiss-btn');
    var guideClose = document.getElementById('pwa-guide-close');
    var guide = getGuide();

    if (btnInstall) btnInstall.addEventListener('click', onInstallClick);
    if (btnDismiss) btnDismiss.addEventListener('click', onDismissClick);
    if (guideClose) guideClose.addEventListener('click', hideGuide);
    if (guide) {
      guide.addEventListener('click', function (e) {
        if (e.target === guide) hideGuide();
      });
    }

    window.addEventListener('beforeinstallprompt', function (e) {
      // مثل قدیم: فقط روی موبایل/تبلت رویداد را نگه می‌داریم
      if (!isMobileOrTablet()) return;
      e.preventDefault();
      deferredPrompt = e;
      window.__koochinoCanInstall = true;
      if (!wasDismissed()) {
        setTimeout(showBanner, BANNER_DELAY_MS);
      }
    });

    window.addEventListener('appinstalled', function () {
      try {
        localStorage.setItem(STORAGE_KEY_INSTALLED, '1');
      } catch (e) {}
      deferredPrompt = null;
      window.__koochinoCanInstall = false;
      hideBanner();
      hideGuide();
      sendPwaInstallLog('pwa');
    });

    // iOS: بنر راهنما (مثل قدیم)
    if (!deferredPrompt && !wasDismissed() && isMobileOrTablet() && isIOS()) {
      setTimeout(showBanner, BANNER_DELAY_MS);
    }
  }

  /**
   * مثل قدیم: اگر رویداد نصب آماده است → دیالوگ کروم
   * وگرنه بنر (کروم) یا راهنما (فایرفاکس/iOS)
   * باید با onclick مستقیم HTML صدا زده شود (نه await بلazor)
   */
  window.koochinoPwaInstall = function () {
    if (isTwaContext()) {
      openManualHelp();
      return false;
    }
    if (isStandalone()) {
      openManualHelp();
      return false;
    }
    if (deferredPrompt) {
      onInstallClick();
      return false;
    }
    // فایرفاکس / iOS دیالوگ بومی ندارند
    if (isFirefox() || isIOS()) {
      openManualHelp();
      return false;
    }
    // کروم بدون رویداد: مثل قدیم فقط بنر — نه پیام راهنما به‌جای نصب
    showBanner();
    return false;
  };
  window.installPWA = window.koochinoPwaInstall;

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
