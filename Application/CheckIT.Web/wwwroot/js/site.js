// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
  const root = document.documentElement;
  const key = 'checkit-theme';

  function applyTheme(theme) {
    if (!theme || theme === 'light') {
      root.removeAttribute('data-theme');
      return;
    }
    root.setAttribute('data-theme', theme);
  }

  function initTheme() {
    const stored = localStorage.getItem(key);
    if (stored) {
      applyTheme(stored);
      return;
    }

    // Light by default per requirements.
    applyTheme('light');
  }

  function initToggle() {
    const btn = document.querySelector('[data-theme-toggle]');
    if (!btn) return;

    const isDark = root.getAttribute('data-theme') === 'dark';
    btn.setAttribute('aria-pressed', String(isDark));

    btn.addEventListener('click', function () {
      const currentDark = root.getAttribute('data-theme') === 'dark';
      const next = currentDark ? 'light' : 'dark';
      applyTheme(next);
      localStorage.setItem(key, next);
      btn.setAttribute('aria-pressed', String(next === 'dark'));
    });
  }

  function initReveal() {
    const els = document.querySelectorAll('.reveal');
    if (!els.length) return;

    const io = new IntersectionObserver(
      (entries) => {
        for (const e of entries) {
          if (e.isIntersecting) {
            e.target.classList.add('in');
            io.unobserve(e.target);
          }
        }
      },
      { threshold: 0.12 }
    );

    els.forEach((el) => io.observe(el));
  }

  function initToasts() {
    const host = document.querySelector('[data-toast-host]');
    if (!host) return;

    const alerts = document.querySelectorAll('[data-server-alert]');
    alerts.forEach((a) => {
      const kind = a.getAttribute('data-kind') || 'success';
      const card = document.createElement('div');
      card.className = `toast-card ${kind}`;
      card.textContent = a.textContent || '';
      host.appendChild(card);
      a.remove();

      setTimeout(() => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(-6px)';
        card.style.transition = 'opacity 260ms ease, transform 260ms ease';
        setTimeout(() => card.remove(), 320);
      }, 4200);
    });
  }

  function showOverlay(kind) {
    if (kind === 'simple') {
      const simple = document.querySelector('[data-simple-overlay]');
      if (!simple) return;
      simple.classList.add('show');
      simple.setAttribute('aria-hidden', 'false');
      return;
    }

    const overlay = document.querySelector('[data-progress-overlay]');
    if (!overlay) return;
    overlay.classList.add('show');
    overlay.setAttribute('aria-hidden', 'false');
  }

  function hideAllOverlays() {
    const a = document.querySelector('[data-progress-overlay]');
    if (a) {
      a.classList.remove('show');
      a.setAttribute('aria-hidden', 'true');
    }
    const b = document.querySelector('[data-simple-overlay]');
    if (b) {
      b.classList.remove('show');
      b.setAttribute('aria-hidden', 'true');
    }
  }

  function initOverlay() {
    // mascot overlay - only used on analysis pages (layout renders it conditionally)
    const mascotOverlay = document.querySelector('[data-progress-overlay]');
    if (mascotOverlay) {
      const analysisForms = document.querySelectorAll('form[data-show-overlay]');
      analysisForms.forEach((form) => {
        form.addEventListener('submit', () => {
          showOverlay('mascot');

          setTimeout(() => {
            const badge = mascotOverlay.querySelector('[data-overlay-badge]');
            if (badge && mascotOverlay.classList.contains('show')) badge.textContent = 'ще трохи…';
          }, 9500);
        });
      });
    }

    // simple overlay hook (optional)
    const formsSimple = document.querySelectorAll('form[data-show-simple-overlay]');
    formsSimple.forEach((form) => {
      form.addEventListener('submit', () => showOverlay('simple'));
    });

    window.addEventListener('pageshow', () => hideAllOverlays());
  }

  function isInternalLink(a) {
    try {
      const url = new URL(a.href, window.location.origin);
      return url.origin === window.location.origin;
    } catch {
      return false;
    }
  }

  function initPageTransitions() {
    const page = document.querySelector('[data-page]');
    if (!page) return;

    document.addEventListener('click', (e) => {
      const a = e.target && e.target.closest ? e.target.closest('a') : null;
      if (!a) return;

      if (a.hasAttribute('download')) return;
      if (a.target && a.target !== '_self') return;
      if (!a.href) return;
      if (!isInternalLink(a)) return;

      if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;

      e.preventDefault();

      const dir = a.getAttribute('data-transition') || 'up';
      page.classList.add('is-exiting', `dir-${dir}`);

      setTimeout(() => {
        window.location.href = a.href;
      }, 220);
    });
  }

  initTheme();
  initToggle();
  initReveal();
  initToasts();
  initOverlay();
  initPageTransitions();
})();
