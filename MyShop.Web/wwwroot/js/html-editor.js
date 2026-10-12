window.htmlEditor = {
    init: function (el, dotNet) {
        if (!el || el._htmlEditorBound) return;
        el._htmlEditorBound = true;
        var timer = null;
        var sync = function () {
            try { dotNet.invokeMethodAsync("OnHtmlChanged", el.innerHTML || ""); } catch (e) { }
        };
        var syncDebounced = function () {
            if (timer) clearTimeout(timer);
            timer = setTimeout(sync, 400);
        };
        el.addEventListener("input", syncDebounced);
        el.addEventListener("blur", function () {
            if (timer) { clearTimeout(timer); timer = null; }
            sync();
        });
        el.addEventListener("paste", function (e) {
            var plain = e.clipboardData && e.clipboardData.getData("text/plain");
            if (!plain || plain.length < 40) return;
            e.preventDefault();
            var html = window.htmlEditor.plainToHtml(plain);
            document.execCommand("insertHTML", false, html);
            syncDebounced();
        });
    },
    plainToHtml: function (text) {
        var esc = function (s) {
            return (s || "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
        };
        var blocks = (text || "").replace(/\r\n/g, "\n").split(/\n+/);
        var out = [];
        for (var i = 0; i < blocks.length; i++) {
            var t = blocks[i].trim();
            if (!t) continue;
            if (t.length < 90 && (/^\d+[\-\.\s]/.test(t) || /^(مقدمه|تعاریف|پذیرش|قانون)/.test(t)))
                out.push("<h2>" + esc(t) + "</h2>");
            else
                out.push("<p>" + esc(t) + "</p>");
        }
        return out.join("") || ("<p>" + esc(text) + "</p>");
    },
    setHtml: function (el, html) {
        if (!el) return;
        if (el.innerHTML !== (html || ""))
            el.innerHTML = html || "";
    },
    getHtml: function (el) {
        return el ? (el.innerHTML || "") : "";
    },
    exec: function (cmd, value) {
        document.execCommand(cmd, false, value || null);
    },
    promptLink: function () {
        var url = window.prompt("آدرس لینک", "https://");
        if (url) document.execCommand("createLink", false, url);
    }
};
