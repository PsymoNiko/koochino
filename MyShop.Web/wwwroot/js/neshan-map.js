// نقشه نشان (Neshan Leaflet SDK) با پشتیبان OpenStreetMap
// کلید: window.neshanApiKey — از appsettings Maps:NeshanApiKey
// SDK رسمی: static.neshan.org/.../neshan-sdk/v1.0.8/index.js

function useNeshan() {
    var provider = (window.mapProvider || '').toLowerCase();
    var key = window.neshanApiKey || '';
    return !!key && provider === 'neshan';
}

function neshanSdkReady() {
    return useNeshan() && window.neshanSdkLoaded === true && window.L && window.L.Map;
}

function destroyMap(mapId) {
    try {
        if (window.neshanMaps && window.neshanMaps[mapId]) {
            try { window.neshanMaps[mapId].remove(); } catch (e) {}
            delete window.neshanMaps[mapId];
        }
        if (window.neshanMapPickers) delete window.neshanMapPickers[mapId];
        if (window.neshanMapMarkers) delete window.neshanMapMarkers[mapId];
        var el = document.getElementById(mapId);
        if (el) {
            el._leaflet_id = null;
            el.innerHTML = '';
        }
    } catch (e) {
        console.warn('destroyMap', e);
    }
}

function createBaseMap(mapId, centerLat, centerLng, zoom) {
    destroyMap(mapId);
    var lat = centerLat != null ? centerLat : 35.6892;
    var lng = centerLng != null ? centerLng : 51.3890;
    var z = zoom != null ? zoom : 12;

    if (neshanSdkReady()) {
        return new L.Map(mapId, {
            key: window.neshanApiKey,
            maptype: 'neshan',
            poi: true,
            traffic: false,
            center: [lat, lng],
            zoom: z
        });
    }

    if (!window.L) {
        throw new Error('موتور نقشه بارگذاری نشده است');
    }

    var map = L.map(mapId, { center: [lat, lng], zoom: z });
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 19
    }).addTo(map);
    return map;
}

function waitForMapContainer(mapId, maxWaitMs, done) {
    var start = Date.now();
    maxWaitMs = maxWaitMs || 4000;
    function check() {
        if (document.getElementById(mapId)) {
            done(true);
            return;
        }
        if (Date.now() - start < maxWaitMs) {
            setTimeout(check, 50);
        } else {
            console.error('Map container not found:', mapId);
            done(false);
        }
    }
    check();
}

function whenMapEngineReady(done, maxWaitMs) {
    var start = Date.now();
    maxWaitMs = maxWaitMs || 10000;
    function check() {
        if (window.L && (window.L.map || window.L.Map)) {
            if (!useNeshan() || window.neshanSdkLoaded === true || window.neshanSdkLoaded === false) {
                done(true);
                return;
            }
        }
        if (Date.now() - start < maxWaitMs) {
            setTimeout(check, 40);
        } else {
            done(!!(window.L && (window.L.map || window.L.Map)));
        }
    }
    check();
}

function storeMap(mapId, map) {
    if (!window.neshanMaps) window.neshanMaps = {};
    window.neshanMaps[mapId] = map;
    setTimeout(function () {
        try { map.invalidateSize(); } catch (e) {}
    }, 150);
    setTimeout(function () {
        try { map.invalidateSize(); } catch (e) {}
    }, 600);
}

function escapeHtml(text) {
    return String(text == null ? '' : text)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

function storePopupHtml(store) {
    var name = escapeHtml(store.name || ('مغازه ' + (store.storeId || '')));
    var html = '<div class="neshan-store-popup"><b>' + name + '</b>';
    if (store.distance) html += '<br><small>' + escapeHtml(store.distance) + '</small>';
    if (store.address) html += '<br><small>' + escapeHtml(store.address) + '</small>';
    if (store.phone) html += '<br><small>☎ ' + escapeHtml(store.phone) + '</small>';
    if (store.status) html += '<br><small>' + escapeHtml(store.status) + '</small>';
    var url = store.clickUrl || (store.storeId ? ('/store/' + store.storeId) : '');
    if (url) {
        html += '<br><a class="neshan-store-link" href="' + escapeHtml(url) + '">مشاهده فروشگاه</a>';
    }
    html += '</div>';
    return html;
}

window.isNeshanMapEngineReady = function () {
    return !!(window.L && (window.L.map || window.L.Map));
};

window.initNeshanMap = function (mapId, latitude, longitude, storeName) {
    waitForMapContainer(mapId, 4000, function (ready) {
        if (!ready) return;
        whenMapEngineReady(function (engineOk) {
            if (!engineOk) {
                console.error('Error initializing Neshan map: engine not ready');
                return;
            }
            try {
                var map = createBaseMap(mapId, latitude, longitude, 15);
                var marker = L.marker([latitude, longitude]).addTo(map);
                if (storeName) {
                    marker.bindPopup('<b>' + escapeHtml(storeName) + '</b><br><small>' +
                        latitude.toFixed(6) + ', ' + longitude.toFixed(6) + '</small>').openPopup();
                }
                storeMap(mapId, map);
            } catch (error) {
                console.error('Error initializing Neshan map:', error);
            }
        });
    });
};

window.initNeshanMapMultiMarkers = function (mapId, storesArray, centerLat, centerLng) {
    waitForMapContainer(mapId, 4000, function (ready) {
        if (!ready) return;
        whenMapEngineReady(function (engineOk) {
            if (!engineOk) {
                console.error('Error initNeshanMapMultiMarkers: engine not ready');
                return;
            }
            try {
                var lat = centerLat != null ? centerLat : 35.6892;
                var lng = centerLng != null ? centerLng : 51.3890;
                var zoom = 10;
                var bounds = null;
                if (storesArray && storesArray.length > 0) {
                    var first = storesArray[0];
                    lat = first.lat;
                    lng = first.lng;
                    bounds = L.latLngBounds([[first.lat, first.lng], [first.lat, first.lng]]);
                    for (var i = 0; i < storesArray.length; i++) {
                        bounds.extend([storesArray[i].lat, storesArray[i].lng]);
                    }
                }
                if (centerLat != null && centerLng != null) {
                    lat = centerLat;
                    lng = centerLng;
                }

                var map = createBaseMap(mapId, lat, lng, zoom);
                if (!window.neshanMapMarkers) window.neshanMapMarkers = {};
                window.neshanMapMarkers[mapId] = {};

                if (centerLat != null && centerLng != null) {
                    L.circleMarker([centerLat, centerLng], {
                        radius: 8,
                        color: '#2563eb',
                        fillColor: '#3b82f6',
                        fillOpacity: 0.9
                    }).addTo(map).bindPopup('موقعیت شما');
                }

                if (storesArray && storesArray.length > 0) {
                    for (var j = 0; j < storesArray.length; j++) {
                        var store = storesArray[j];
                        var m = L.marker([store.lat, store.lng]).addTo(map);
                        m.bindPopup(storePopupHtml(store));
                        var sid = store.storeId != null ? String(store.storeId) : String(j);
                        window.neshanMapMarkers[mapId][sid] = m;
                    }
                    if (bounds && bounds.isValid()) {
                        if (centerLat != null && centerLng != null) bounds.extend([centerLat, centerLng]);
                        map.fitBounds(bounds, { padding: [30, 30], maxZoom: 15 });
                    }
                }
                storeMap(mapId, map);
            } catch (err) {
                console.error('Error initNeshanMapMultiMarkers:', err);
            }
        });
    });
};

window.focusNeshanMapStore = function (mapId, storeId, lat, lng) {
    try {
        var map = window.neshanMaps && window.neshanMaps[mapId];
        if (!map) return false;
        if (lat != null && lng != null) {
            map.setView([lat, lng], 16);
        }
        var markers = window.neshanMapMarkers && window.neshanMapMarkers[mapId];
        var marker = markers && markers[String(storeId)];
        if (marker && marker.openPopup) marker.openPopup();
        return true;
    } catch (err) {
        console.error('focusNeshanMapStore:', err);
        return false;
    }
};

window.removeNeshanMap = function (mapId) {
    try {
        destroyMap(mapId);
        return true;
    } catch (error) {
        console.error('Error removing Neshan map:', error);
        return false;
    }
};

window.initNeshanMapPicker = function (mapId, initialLat, initialLng, callback) {
    waitForMapContainer(mapId, 3000, function (ready) {
        if (!ready) return;
        whenMapEngineReady(function (engineOk) {
            if (!engineOk) {
                console.error('Error initializing Neshan map picker: engine not ready');
                return;
            }
            try {
                var centerLat = 35.6892;
                var centerLng = 51.3890;
                var zoom = 10;
                if (initialLat && initialLng && initialLat > 0 && initialLng > 0) {
                    centerLat = initialLat;
                    centerLng = initialLng;
                    zoom = 15;
                }

                var map = createBaseMap(mapId, centerLat, centerLng, zoom);
                var marker = null;
                if (initialLat && initialLng && initialLat > 0 && initialLng > 0) {
                    marker = L.marker([initialLat, initialLng]).addTo(map);
                }

                function setMarkerAndNotify(lat, lng, silent) {
                    if (marker) {
                        marker.setLatLng([lat, lng]);
                    } else {
                        marker = L.marker([lat, lng]).addTo(map);
                    }
                    map.setView([lat, lng], 15);
                    if (silent) return;
                    if (callback && typeof callback === 'object' && callback.invokeMethodAsync) {
                        callback.invokeMethodAsync('OnLocationSelected', lat, lng).catch(function (err) {
                            console.error('Error calling DotNet callback:', err);
                        });
                    } else if (typeof callback === 'function') {
                        callback(lat, lng);
                    }
                }

                map.on('click', function (e) {
                    setMarkerAndNotify(e.latlng.lat, e.latlng.lng);
                });

                storeMap(mapId, map);
                if (!window.neshanMapPickers) window.neshanMapPickers = {};
                window.neshanMapPickers[mapId] = { setMarker: setMarkerAndNotify };
            } catch (error) {
                console.error('Error initializing Neshan map picker:', error);
            }
        });
    });
};

window.setNeshanMapMarkerPosition = function (mapId, lat, lng) {
    try {
        var p = window.neshanMapPickers && window.neshanMapPickers[mapId];
        if (p && p.setMarker) {
            p.setMarker(lat, lng, true);
        }
    } catch (err) {
        console.error('setNeshanMapMarkerPosition:', err);
    }
};

window.flyNeshanMapTo = function (mapId, lat, lng, zoom) {
    try {
        var map = window.neshanMaps && window.neshanMaps[mapId];
        if (!map || !lat || !lng) return;
        var z = zoom || 12;
        if (typeof map.flyTo === 'function') {
            map.flyTo([lat, lng], z, { duration: 0.8 });
        } else {
            map.setView([lat, lng], z);
        }
        var p = window.neshanMapPickers && window.neshanMapPickers[mapId];
        if (p && p.setMarker) {
            p.setMarker(lat, lng, true);
        }
    } catch (err) {
        console.error('flyNeshanMapTo:', err);
    }
};

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
