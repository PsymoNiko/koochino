// تنظیم داینامیک متا تگ‌های سئو از پنل ادمین
window.setSeoMeta = function (opts) {
    if (!opts) return;
    var d = document;
    function setMeta(name, content) {
        if (!content) return;
        var el = d.querySelector('meta[name="' + name + '"]');
        if (!el) { el = d.createElement('meta'); el.setAttribute('name', name); d.head.appendChild(el); }
        el.setAttribute('content', content);
    }
    function setProp(property, content) {
        if (!content) return;
        var el = d.querySelector('meta[property="' + property + '"]');
        if (!el) { el = d.createElement('meta'); el.setAttribute('property', property); d.head.appendChild(el); }
        el.setAttribute('content', content);
    }
    setMeta('description', opts.description);
    setMeta('keywords', opts.keywords);
    setProp('og:title', opts.ogTitle);
    setProp('og:description', opts.ogDescription);
    setProp('og:image', opts.ogImage);
    setProp('og:url', opts.ogUrl);
    setProp('og:type', opts.ogType || 'website');
    setMeta('twitter:card', opts.twitterCard || 'summary_large_image');
    setMeta('twitter:title', opts.twitterTitle || opts.ogTitle);
    setMeta('twitter:description', opts.twitterDescription || opts.ogDescription);
    if (opts.canonical) {
        var link = d.querySelector('link[rel="canonical"]');
        if (!link) { link = d.createElement('link'); link.setAttribute('rel', 'canonical'); d.head.appendChild(link); }
        link.setAttribute('href', opts.canonical);
    }
    if (opts.robots) {
        var robotsEl = d.querySelector('meta[name="robots"]');
        if (!robotsEl) { robotsEl = d.createElement('meta'); robotsEl.setAttribute('name', 'robots'); d.head.appendChild(robotsEl); }
        robotsEl.setAttribute('content', opts.robots);
    }
};
