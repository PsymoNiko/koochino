(function () {
  'use strict';

  function isPrintRoute() {
    try {
      return /\/StoreOwner\/Stores\/Print\//i.test(location.pathname || '');
    } catch (e) {
      return false;
    }
  }

  function markPrintPage() {
    if (!isPrintRoute()) return;
    document.documentElement.classList.add('store-print-page');
    if (document.body) document.body.classList.add('store-print-page');
    var banner = document.getElementById('pwa-install-banner');
    if (banner) {
      banner.classList.remove('pwa-banner-visible');
      banner.setAttribute('aria-hidden', 'true');
      banner.style.display = 'none';
    }
    var guide = document.getElementById('pwa-install-guide');
    if (guide) {
      guide.setAttribute('aria-hidden', 'true');
      guide.style.display = 'none';
    }
  }

  function loadScript(src) {
    return new Promise(function (resolve, reject) {
      var s = document.createElement('script');
      s.src = src;
      s.async = true;
      s.onload = function () { resolve(); };
      s.onerror = function () { reject(new Error('load ' + src)); };
      document.head.appendChild(s);
    });
  }

  function ensurePdfLibs() {
    var p = Promise.resolve();
    if (typeof html2canvas !== 'function') {
      p = p.then(function () { return loadScript('/js/vendor/html2canvas.min.js'); })
        .catch(function () { return loadScript('https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js'); });
    }
    if (!(window.jspdf && window.jspdf.jsPDF) && typeof window.jsPDF !== 'function') {
      p = p.then(function () { return loadScript('/js/vendor/jspdf.umd.min.js'); })
        .catch(function () { return loadScript('https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js'); });
    }
    return p;
  }

  function getJsPdf() {
    if (window.jspdf && window.jspdf.jsPDF) return window.jspdf.jsPDF;
    if (typeof window.jsPDF === 'function') return window.jsPDF;
    return null;
  }

  function renderCanvas() {
    var el = document.querySelector('.print-page');
    if (!el) throw new Error('طرح چاپ پیدا نشد.');
    return ensurePdfLibs().then(function () {
      return html2canvas(el, {
        scale: 2,
        useCORS: true,
        allowTaint: true,
        backgroundColor: '#ffffff',
        logging: false
      });
    });
  }

  function canvasToPngBlob(canvas) {
    return new Promise(function (resolve, reject) {
      canvas.toBlob(function (blob) {
        if (blob) resolve(blob); else reject(new Error('ساخت تصویر ناموفق بود.'));
      }, 'image/png');
    });
  }

  function canvasToPdfBlob(canvas) {
    var JsPDF = getJsPdf();
    if (!JsPDF) throw new Error('کتابخانه PDF بارگذاری نشد.');
    var pdf = new JsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4', compress: true });
    var img = canvas.toDataURL('image/jpeg', 0.92);
    pdf.addImage(img, 'JPEG', 0, 0, 210, 297);
    return pdf.output('blob');
  }

  function baseFileName(fileName) {
    return (fileName || 'koochino-print-a4').replace(/\.(pdf|png)$/i, '');
  }

  function downloadBlob(blob, fileName) {
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(function () { URL.revokeObjectURL(url); }, 4000);
  }

  window.koochinoStorePrint = {
    mark: markPrintPage,
    print: function () {
      markPrintPage();
      window.print();
    },
    downloadPdf: function (fileName) {
      markPrintPage();
      return renderCanvas().then(function (canvas) {
        var blob = canvasToPdfBlob(canvas);
        downloadBlob(blob, fileName || 'koochino-print-a4.pdf');
      });
    },
    canShareFiles: function () {
      try {
        return !!(navigator.canShare && navigator.share);
      } catch (e) {
        return false;
      }
    },
    share: function (fileName, format) {
      markPrintPage();
      var name = baseFileName(fileName);
      return renderCanvas().then(function (canvas) {
        var isPdf = (format || 'pdf').toLowerCase() === 'pdf';
        var blob = isPdf ? canvasToPdfBlob(canvas) : canvasToPngBlob(canvas);
        return Promise.resolve(blob).then(function (b) {
          var fullName = name + (isPdf ? '.pdf' : '.png');
          var file = new File([b], fullName, { type: isPdf ? 'application/pdf' : 'image/png' });
          var canUseShare = false;
          try { canUseShare = navigator.canShare && navigator.canShare({ files: [file] }); } catch (e) { canUseShare = false; }
          if (canUseShare) {
            return navigator.share({
              files: [file],
              title: 'کوچینو',
              text: 'پوستر معرفی کوچینو'
            }).catch(function (err) {
              if (err && err.name === 'AbortError') return;
              downloadBlob(b, fullName);
            });
          }
          downloadBlob(b, fullName);
        });
      });
    }
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', markPrintPage);
  } else {
    markPrintPage();
  }
})();
