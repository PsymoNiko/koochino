window.supportChat = {
    ensureGuest: function () {
        try {
            var t = localStorage.getItem("koch_support_guest");
            if (!t) {
                t = (crypto.randomUUID && crypto.randomUUID()) || ("g-" + Date.now() + "-" + Math.random().toString(16).slice(2));
                localStorage.setItem("koch_support_guest", t);
            }
            return t;
        } catch (e) {
            return "g-session";
        }
    },
    requestNotifyPermission: async function () {
        try {
            if (!("Notification" in window)) return "unsupported";
            if (Notification.permission === "granted") return "granted";
            if (Notification.permission === "denied") return "denied";
            return await Notification.requestPermission();
        } catch (e) {
            return "error";
        }
    },
    showNotify: function (title, body) {
        try {
            if (!("Notification" in window) || Notification.permission !== "granted") return false;
            var n = new Notification(title || "پشتیبانی", {
                body: body || "",
                icon: "/favicon.ico",
                tag: "support-ops"
            });
            setTimeout(function () { try { n.close(); } catch (e) { } }, 6000);
            return true;
        } catch (e) {
            return false;
        }
    },
    playPing: function () {
        try {
            var Ctx = window.AudioContext || window.webkitAudioContext;
            if (!Ctx) return;
            var ctx = new Ctx();
            var o = ctx.createOscillator();
            var g = ctx.createGain();
            o.type = "sine";
            o.frequency.value = 880;
            g.gain.value = 0.04;
            o.connect(g);
            g.connect(ctx.destination);
            o.start();
            setTimeout(function () {
                try { o.stop(); ctx.close(); } catch (e) { }
            }, 160);
        } catch (e) { }
    }
};
