(function () {
  function ready(fn) {
    if (document.readyState !== "loading") fn();
    else document.addEventListener("DOMContentLoaded", fn);
  }

  function isDesktop() {
    return window.matchMedia("(min-width: 769px)").matches;
  }

  function applyRailLabels() {
    document.querySelectorAll(".sidebar .nav-link").forEach(function (link) {
      var text = link.querySelector(".nav-text");
      if (text) link.setAttribute("data-rail-label", text.textContent.trim());
    });
  }

  function initTheme() {
    try {
      var stored = localStorage.getItem("theme");
      var prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
      var theme = stored || (prefersDark ? "dark" : "light");
      document.documentElement.setAttribute("data-theme", theme);
    } catch (e) { /* ignore */ }
  }

  function bindThemeToggle() {
    document.addEventListener("click", function (e) {
      var btn = e.target.closest(".theme-toggle");
      if (!btn) return;
      var current = document.documentElement.getAttribute("data-theme") || "light";
      var next = current === "dark" ? "light" : "dark";
      document.documentElement.setAttribute("data-theme", next);
      try { localStorage.setItem("theme", next); } catch (err) { /* ignore */ }
      btn.setAttribute("aria-pressed", next === "dark" ? "true" : "false");
    });
  }

  function bindSidebarToggle() {
    document.addEventListener("click", function (e) {
      var toggle = e.target.closest(".sidebar-toggle");
      var backdrop = e.target.closest(".sidebar-backdrop");
      if (!toggle && !backdrop) return;

      if (!isDesktop()) {
        document.body.classList.toggle("sidebar-open");
        var sidebar = document.querySelector(".sidebar");
        if (sidebar) sidebar.classList.toggle("open", document.body.classList.contains("sidebar-open"));
        var bd = document.querySelector(".sidebar-backdrop");
        if (bd) bd.hidden = !document.body.classList.contains("sidebar-open");
        return;
      }

      document.body.classList.toggle("sidebar-rail");
      try {
        localStorage.setItem("gentelella:sidebar-rail", document.body.classList.contains("sidebar-rail") ? "1" : "0");
      } catch (err) { /* ignore */ }
    });

    try {
      if (localStorage.getItem("gentelella:sidebar-rail") === "1" && isDesktop()) {
        document.body.classList.add("sidebar-rail");
      }
    } catch (err) { /* ignore */ }
  }

  function bindNavSubmenus() {
    document.addEventListener("click", function (e) {
      var btn = e.target.closest(".sidebar .nav-toggle");
      if (!btn) return;
      e.preventDefault();
      var tree = btn.closest(".nav-tree");
      if (!tree) return;
      var willOpen = !tree.classList.contains("open");
      document.querySelectorAll(".sidebar .nav-tree.open").forEach(function (t) {
        if (t !== tree) {
          t.classList.remove("open");
          var b = t.querySelector(".nav-toggle");
          if (b) b.setAttribute("aria-expanded", "false");
        }
      });
      tree.classList.toggle("open", willOpen);
      btn.setAttribute("aria-expanded", willOpen ? "true" : "false");
    });
  }

  ready(function () {
    document.body.classList.add("admin-gentelella");
    initTheme();
    applyRailLabels();
    bindThemeToggle();
    bindSidebarToggle();
    bindNavSubmenus();
  });

  // Blazor enhanced navigation: re-apply body class after renders
  if (window.Blazor) {
    document.addEventListener("enhancedload", function () {
      document.body.classList.add("admin-gentelella");
      applyRailLabels();
    });
  }
})();
