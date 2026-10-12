// چرخش دستی (drag) جعبه‌های بیضی صفحه جعبه شانس — هیچ چرخش خودکاری اینجا نیست،
// فقط واکنش به کشیدن انگشت/ماوس کاربر روی .orbit-stage، با کمی اینرسی بعد از رهاکردن.
window.luckyOrbit = (function () {
    function apply(ring, spins, items, n, rotation) {
        ring.style.transform = 'rotate(' + rotation + 'deg)';
        for (var i = 0; i < spins.length; i++) {
            spins[i].style.transform = 'rotate(' + (-rotation) + 'deg)';
        }
        for (var i = 0; i < items.length; i++) {
            var baseAngle = i * (360 / n);
            var rad = (rotation + baseAngle) * Math.PI / 180;
            var depth = (Math.sin(rad) + 1) / 2; // 0..1
            var scale = 0.58 + depth * 0.42;
            var opacity = 0.5 + depth * 0.5;
            items[i].style.scale = scale;
            items[i].style.opacity = opacity;
            items[i].style.zIndex = Math.round(depth * 100);
        }
    }

    function init(stageEl, boxCount) {
        if (!stageEl) return;
        var ring = stageEl.querySelector('.orbit-ring');
        if (!ring) return;

        // اگر قبلا روی همین المان bind شده، اول پاکسازی کن (جلوگیری از دوبار bind شدن)
        if (stageEl._luckyOrbit) {
            dispose(stageEl);
        }

        var items = Array.prototype.slice.call(ring.querySelectorAll('.orbit-item'));
        var spins = Array.prototype.slice.call(ring.querySelectorAll('.orbit-spin'));
        var n = boxCount || items.length || 1;

        var state = { rotation: 0, dragging: false, startX: 0, startRotation: 0, lastX: 0, lastT: 0, velocity: 0, raf: null, dragDistance: 0 };

        function render() { apply(ring, spins, items, n, state.rotation); }
        render();

        function cancelMomentum() {
            if (state.raf) { cancelAnimationFrame(state.raf); state.raf = null; }
        }

        function onDown(e) {
            cancelMomentum();
            state.dragging = true;
            state.dragDistance = 0;
            var x = (e.touches && e.touches[0] ? e.touches[0].clientX : e.clientX);
            state.startX = x;
            state.lastX = x;
            state.startRotation = state.rotation;
            state.lastT = performance.now();
            state.velocity = 0;
        }

        function onMove(e) {
            if (!state.dragging) return;
            var x = (e.touches && e.touches[0] ? e.touches[0].clientX : e.clientX);
            var now = performance.now();
            var dx = x - state.startX;
            state.dragDistance = Math.max(state.dragDistance, Math.abs(x - state.lastX) + (state.dragDistance ? 0 : 0));
            state.dragDistance += Math.abs(x - state.lastX);
            state.rotation = state.startRotation + dx * 0.55;
            render();
            var dt = now - state.lastT;
            if (dt > 0) state.velocity = ((x - state.lastX) * 0.55 / dt) * 16.6667;
            state.lastX = x;
            state.lastT = now;
        }

        function onUp() {
            if (!state.dragging) return;
            state.dragging = false;
            var v = state.velocity;
            function step() {
                v *= 0.95;
                state.rotation += v;
                render();
                if (Math.abs(v) > 0.05) {
                    state.raf = requestAnimationFrame(step);
                } else {
                    state.raf = null;
                }
            }
            if (Math.abs(v) > 0.05) state.raf = requestAnimationFrame(step);
        }

        function onClickCapture(e) {
            if (state.dragDistance > 6) {
                e.stopPropagation();
                e.preventDefault();
            }
            state.dragDistance = 0;
        }

        stageEl.addEventListener('pointerdown', onDown);
        window.addEventListener('pointermove', onMove);
        window.addEventListener('pointerup', onUp);
        window.addEventListener('pointercancel', onUp);
        stageEl.addEventListener('touchstart', onDown, { passive: true });
        stageEl.addEventListener('touchmove', onMove, { passive: true });
        stageEl.addEventListener('touchend', onUp);
        stageEl.addEventListener('click', onClickCapture, true);
        stageEl.style.touchAction = 'pan-y';
        stageEl.style.cursor = 'grab';

        stageEl._luckyOrbit = {
            onDown: onDown, onMove: onMove, onUp: onUp, onClickCapture: onClickCapture, cancelMomentum: cancelMomentum
        };
    }

    function dispose(stageEl) {
        if (!stageEl || !stageEl._luckyOrbit) return;
        var h = stageEl._luckyOrbit;
        h.cancelMomentum();
        stageEl.removeEventListener('pointerdown', h.onDown);
        window.removeEventListener('pointermove', h.onMove);
        window.removeEventListener('pointerup', h.onUp);
        window.removeEventListener('pointercancel', h.onUp);
        stageEl.removeEventListener('touchstart', h.onDown);
        stageEl.removeEventListener('touchmove', h.onMove);
        stageEl.removeEventListener('touchend', h.onUp);
        stageEl.removeEventListener('click', h.onClickCapture, true);
        delete stageEl._luckyOrbit;
    }

    return { init: init, dispose: dispose };
})();
