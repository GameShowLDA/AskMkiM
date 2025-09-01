document.addEventListener('DOMContentLoaded', () => {
    /* ───────── Refs */
    const tabs = document.querySelectorAll('.tab');
    const tabContents = document.querySelectorAll('.tab-content');
    const navTree = document.getElementById('nav-tree');
    const breadcrumb = document.getElementById('breadcrumb');
    const prevBtn = document.getElementById('prev-btn');
    const nextBtn = document.getElementById('next-btn');
    const bookmarkBtn = document.getElementById('bookmark-btn');
    const bookmarksList = document.getElementById('bookmarks-list');
    const searchBox = document.querySelector('.search-box');
    const searchResults = document.getElementById('search-results');
    const clearBtn = document.getElementById('clear-btn');
    const resizer = document.getElementById('resizer');
    const primaryArea = document.querySelector('.primary-area');
    const secondaryArea = document.querySelector('.secondary-area');
    const contentFrame = document.getElementById('content-frame');

    /* === Страница, открывающаяся по-умолчанию, если cmd не найден === */
    const FALLBACK_ID = 'LanguageControlProgramsBasicConcepts';  // ← при необходимости замените

    /* ───────── Data from DOM */
    // «Страницей» считаем любой .tree-item, у которого есть data-src
    const pageItems = Array.from(navTree.querySelectorAll('.tree-item[data-src]'));
    const pageOrder = pageItems.map(el => el.id);               // порядок навигации

    /* ───────── State */
    let currentPage = null;
    let bookmarks = new Set(JSON.parse(localStorage.getItem('mki-bookmarks') || '[]'));

    /* ───────── Helpers */
    function updateActiveTreeItem(id) {
        document.querySelectorAll('.tree-item').forEach(el => el.classList.remove('active'));
        const item = navTree.querySelector(`.tree-item[id="${id}"]`);
        if (item) item.classList.add('active');
    }

    function updateNavButtons() {
        const idx = pageOrder.indexOf(currentPage);
        prevBtn.disabled = idx <= 0;
        nextBtn.disabled = idx === -1 || idx >= pageOrder.length - 1;
    }

    function updateBookmarkIcon() {
        const item = navTree.querySelector(`.tree-item[id="${currentPage}"]`);
        const isPage = !!item && item.classList.contains('page');

        if (!isPage) {                     // для папок — сразу скрываем
            bookmarkBtn.style.visibility = 'hidden';
            bookmarkBtn.classList.remove('active');
            return;
        }

        // для страниц — показываем и ставим активность
        bookmarkBtn.style.visibility = 'visible';
        bookmarks.has(currentPage)
            ? bookmarkBtn.classList.add('active')
            : bookmarkBtn.classList.remove('active');
    }

    function saveBookmarks() {
        localStorage.setItem('mki-bookmarks', JSON.stringify([...bookmarks]));
    }

    function renderBookmarksTab() {
        bookmarksList.innerHTML = [...bookmarks].map(id => {
            const el = navTree.querySelector(`.tree-item[id="${id}"]`);
            if (!el || !el.classList.contains('page') || !el.dataset.src) return '';
            return `<div class="bookmark-item" data-id="${id}">${el.innerText.trim()}</div>`;
        }).join('');
    }

    function buildBreadcrumb(el) {
        const parts = [];
        let node = el;
        while (node) {
            if (node.classList && node.classList.contains('tree-item')) {
                parts.unshift(node.innerText.trim());
            }
            const ul = node.parentElement?.parentElement;
            node = ul ? ul.previousElementSibling : null;
        }
        breadcrumb.innerHTML = parts.map(t => `<span>${t}</span>`).join(' → ');
    }

    /* ───────── Разворачиваем все родительские папки, чтобы элемент был виден */
    function expandPathTo(el) {
        let node = el;
        while (node && node.id !== 'nav-tree') {
            if (node.tagName === 'UL') {
                node.style.display = 'block';
            } else if (node.classList?.contains('folder')) {
                const toggle = node.querySelector('.toggle');
                if (toggle) toggle.textContent = '▼';
            }
            node = node.parentElement;
        }
    }

    /* ───────── Загрузка страницы */
    async function loadPage(id, pushHistory = true) {
        currentPage = id;
        const item = navTree.querySelector(`.tree-item[id="${id}"]`);
        if (!item) return;

        expandPathTo(item);

        if (item.dataset.src) {
            contentFrame.src = item.dataset.src;
        }
        updateActiveTreeItem(id);
        updateNavButtons();
        buildBreadcrumb(item);
        updateBookmarkIcon();

        if (pushHistory) {
            history.pushState({ id }, '', `?section=${encodeURIComponent(id)}`);
        }
    }

    /* ───────── Автоподгонка iframe */
    function fitFrame() {
        const doc = contentFrame.contentDocument;
        if (!doc) return;

        contentFrame.style.height = '0px';
        const newHeight = Math.max(
            doc.documentElement.scrollHeight,
            doc.body?.scrollHeight || 0
        );
        contentFrame.style.height = newHeight + 'px';

        doc.documentElement.style.overflow = 'hidden';
        doc.body.style.overflow = 'hidden';
    }

    contentFrame.addEventListener('load', () => {
        fitFrame();

        const doc = contentFrame.contentDocument;
        if (!doc) return;

        // картинки
        doc.querySelectorAll('img').forEach(img => {
            if (!img.complete) img.addEventListener('load', fitFrame);
        });
        // шрифты
        if (doc.fonts) doc.fonts.ready.then(fitFrame);
    });

    /* ───────── Tabs */
    tabs.forEach(tab => tab.addEventListener('click', () => {
        tabs.forEach(t => t.classList.remove('active'));
        tabContents.forEach(c => c.classList.remove('active'));
        tab.classList.add('active');
        document.getElementById(`${tab.dataset.tab}-content`).classList.add('active');

        if (tab.dataset.tab !== 'search') {
            searchBox.value = '';
            searchResults.innerHTML = '';
            clearBtn.style.display = 'none';
        }
    }));

    /* ───────── Navigation tree (clicks) */
    navTree.addEventListener('click', e => {
        const ti = e.target.closest('.tree-item');
        if (!ti) return;

        const clickedToggle = e.target.classList.contains('toggle');
        const hasPage = !!ti.dataset.src;

        /* --- папка --- */
        if (ti.classList.contains('folder')) {
            const ul = ti.nextElementSibling;
            if (ul) {
                const opened = ul.style.display === 'block';
                ul.style.display = opened ? 'none' : 'block';
                ti.querySelector('.toggle').textContent = opened ? '▶' : '▼';
            }

            // если у папки есть собственная страница и клик НЕ по стрелке —
            // СНАЧАЛА прячем кнопку, ПОТОМ грузим страницу
            if (hasPage && !clickedToggle) {
                bookmarkBtn.style.visibility = 'hidden'; // ← требуемое «сначала скрылась»
                loadPage(ti.id);
            }
            return;
        }

        /* --- простая страница --- */
        if (hasPage) loadPage(ti.id);
    });

    /* ───────── Search */
    searchBox.addEventListener('input', () => {
        const q = searchBox.value.trim().toLowerCase();
        clearBtn.style.display = q ? 'block' : 'none';
        if (!q) { searchResults.innerHTML = ''; return; }

        const result = pageItems.filter(el =>
            (el.id + ' ' + el.innerText).toLowerCase().includes(q)
        );

        searchResults.innerHTML = result.length
            ? result.map(el =>
                `<div class="result-item" data-id="${el.id}">${el.innerText.trim()}</div>`
            ).join('')
            : '<em>Ничего не найдено</em>';
    });

    clearBtn.addEventListener('click', () => {
        searchBox.value = '';
        searchResults.innerHTML = '';
        clearBtn.style.display = 'none';
        searchBox.focus();
    });

    searchResults.addEventListener('click', e => {
        const el = e.target.closest('.result-item');
        if (!el) return;
        tabs[0].click();               // «Меню»
        loadPage(el.dataset.id);
    });

    /* ───────── Bookmarks */
    bookmarkBtn.addEventListener('click', () => {
        const item = navTree.querySelector(`.tree-item[id="${currentPage}"]`);
        if (!item || !item.classList.contains('page') || !item.dataset.src) return; // только страницы

        bookmarks.has(currentPage) ? bookmarks.delete(currentPage)
            : bookmarks.add(currentPage);

        saveBookmarks();
        updateBookmarkIcon();
        renderBookmarksTab();
    });

    bookmarksList.addEventListener('click', e => {
        const item = e.target.closest('.bookmark-item');
        if (!item) return;
        tabs[0].click();
        loadPage(item.dataset.id);
    });

    /* ───────── Prev/Next */
    prevBtn.addEventListener('click', () => {
        const idx = pageOrder.indexOf(currentPage);
        if (idx > 0) loadPage(pageOrder[idx - 1]);
    });
    nextBtn.addEventListener('click', () => {
        const idx = pageOrder.indexOf(currentPage);
        if (idx < pageOrder.length - 1) loadPage(pageOrder[idx + 1]);
    });

    /* ───────── Resizer */
    let isResizing = false;
    resizer.addEventListener('mousedown', e => {
        isResizing = true;
        document.body.style.cursor = 'col-resize';
        e.preventDefault();
    });
    document.addEventListener('mousemove', e => {
        if (!isResizing) return;
        const total = document.querySelector('.main-content').offsetWidth;
        let left = Math.max(e.clientX, total / 3);
        left = Math.min(left, total / 2);
        primaryArea.style.width = left + 'px';
        secondaryArea.style.flex = 'none';
        secondaryArea.style.width = (total - left - resizer.offsetWidth) + 'px';
    });
    document.addEventListener('mouseup', () => {
        isResizing = false;
        document.body.style.cursor = 'default';
    });

    /* ───────── History (Back/Forward) */
    window.addEventListener('popstate', e => {
        const id = e.state?.id || pageOrder[0];
        loadPage(id, false);
    });

    /* ───────── Initialisation */
    navTree.querySelectorAll('ul').forEach(ul => ul.style.display = 'none');
    navTree.querySelectorAll('.folder .toggle').forEach(t => t.textContent = '▶');
    renderBookmarksTab();

    /* --- Стартовая страница --- */
    const urlParams = new URLSearchParams(location.search);
    const cmd = urlParams.get('cmd');      // ?cmd=...
    const section = urlParams.get('section');  // ?section=...

    let startId = pageOrder[0];

    if (cmd) {
        const decoded = decodeURIComponent(cmd).toLowerCase();
        const found = pageItems.find(el => el.id.toLowerCase() === decoded);
        startId = found ? found.id : FALLBACK_ID;
    } else if (section) {
        startId = section;
    }

    loadPage(startId, false);я
});