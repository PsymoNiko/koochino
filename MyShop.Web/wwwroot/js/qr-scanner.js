/**
 * اسکن QR / بارکد با دوربین — html5-qrcode + callback به Blazor
 * options.formats: 'qr' | 'barcode' | 'all' (پیش‌فرض qr برای سازگاری با ثبت سفارش مشتری)
 */
window._qrScannerInstances = window._qrScannerInstances || {};

function koochResolveFormats(mode) {
    if (typeof Html5QrcodeSupportedFormats === 'undefined') return undefined;
    var F = Html5QrcodeSupportedFormats;
    if (mode === 'barcode') {
        return [F.EAN_13, F.EAN_8, F.CODE_128, F.CODE_39, F.UPC_A, F.UPC_E, F.ITF, F.CODABAR];
    }
    if (mode === 'all') {
        return [F.QR_CODE, F.EAN_13, F.EAN_8, F.CODE_128, F.CODE_39, F.UPC_A, F.UPC_E, F.ITF, F.CODABAR];
    }
    // پیش‌فرض: فقط QR (مسیر مشتری / فروشنده)
    return [F.QR_CODE];
}

window.koochInitQrScanner = function (containerId, dotNetRef, callbackMethodName, options) {
    if (typeof Html5Qrcode === 'undefined') {
        return Promise.reject(new Error('Html5Qrcode library not loaded.'));
    }
    var container = document.getElementById(containerId);
    if (!container) {
        return Promise.reject(new Error('Container not found: ' + containerId));
    }
    if (window._qrScannerInstances[containerId]) {
        try { window._qrScannerInstances[containerId].stop(); } catch (e) {}
        delete window._qrScannerInstances[containerId];
    }

    options = options || {};
    var mode = (options.formats || 'qr').toLowerCase();
    var formats = koochResolveFormats(mode);
    var isBarcode = mode === 'barcode' || mode === 'all';
    var qrbox = isBarcode
        ? { width: 280, height: 120 }
        : { width: 250, height: 250 };

    var config = { fps: 10, qrbox: qrbox };
    if (formats && formats.length) {
        config.formatsToSupport = formats;
    }

    var scanner = new Html5Qrcode(containerId, formats ? { formatsToSupport: formats } : undefined);
    window._qrScannerInstances[containerId] = scanner;
    return scanner.start(
        { facingMode: 'environment' },
        config,
        function (decodedText) {
            dotNetRef.invokeMethodAsync(callbackMethodName, decodedText).catch(function () {});
        },
        function () {}
    );
};

window.koochStopQrScanner = function (containerId) {
    var scanner = window._qrScannerInstances[containerId];
    if (!scanner) return Promise.resolve();
    return scanner.stop().then(function () {
        delete window._qrScannerInstances[containerId];
    }).catch(function () {
        delete window._qrScannerInstances[containerId];
    });
};
