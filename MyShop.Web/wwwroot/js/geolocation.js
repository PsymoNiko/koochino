/**
 * لوکیشن (GPS) — برای استفاده از Blazor با IJSRuntime
 * نیاز به HTTPS و اجازهٔ کاربر دارد.
 */
window.koochGetUserLocation = function () {
    return new Promise(function (resolve, reject) {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation is not supported by this browser.'));
            return;
        }
        navigator.geolocation.getCurrentPosition(
            function (position) {
                resolve({
                    lat: position.coords.latitude,
                    lng: position.coords.longitude,
                    accuracy: position.coords.accuracy
                });
            },
            function (err) {
                reject(err);
            },
            { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
        );
    });
};

/** درخواست موقعیت فعلی و اعلام به Blazor (فرم فروشگاه — نقشه استاتیک نشان) */
window.requestStoreLocationAndNotify = function (dotNetRef) {
    if (!navigator.geolocation) {
        if (dotNetRef && dotNetRef.invokeMethodAsync) {
            dotNetRef.invokeMethodAsync('OnLocationError', 'مرورگر از موقعیت مکانی پشتیبانی نمی‌کند').catch(function () {});
        }
        return;
    }
    navigator.geolocation.getCurrentPosition(
        function (position) {
            var lat = position.coords.latitude;
            var lng = position.coords.longitude;
            if (dotNetRef && dotNetRef.invokeMethodAsync) {
                dotNetRef.invokeMethodAsync('OnLocationSelected', lat, lng).catch(function (err) { console.error(err); });
            }
        },
        function (err) {
            var msg = err.message || 'دسترسی به موقعیت رد شد';
            if (dotNetRef && dotNetRef.invokeMethodAsync) {
                dotNetRef.invokeMethodAsync('OnLocationError', msg).catch(function () {});
            }
        },
        { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
    );
};
