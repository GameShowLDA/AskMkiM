document.addEventListener('DOMContentLoaded', () => {
    const $ = (selector, root = document) => root.querySelector(selector);
    const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
    const textOf = el => el?.innerText.trim() || '';
    const normalize = text => text.replace(/\s+/g, ' ').trim().toLowerCase();

    const tabs = $$('.tab');
    const panes = $$('.tab-content');
    const navTree = $('#nav-tree');
    const breadcrumb = $('#breadcrumb');
    const prevBtn = $('#prev-btn');
    const nextBtn = $('#next-btn');
    const bookmarkBtn = $('#bookmark-btn');
    const bookmarksList = $('#bookmarks-list');
    const searchBox = $('.search-box');
    const searchResults = $('#search-results');
    const clearBtn = $('#clear-btn');
    const searchModes = $$('input[name="search-mode"]');
    const resizer = $('#resizer');
    const primaryArea = $('.primary-area');
    const secondaryArea = $('.secondary-area');
    const contentFrame = $('#content-frame');
    const mainContent = $('.main-content');

    const FALLBACK_ID = 'GeneralInformation';
    const SEARCH_PLACEHOLDERS = {
        title: 'Введите название раздела…',
        content: 'Введите текст для поиска по содержимому…'
    };

    const pages = $$('.tree-item[data-src]', navTree).map(el => ({
        id: el.id,
        el,
        src: el.dataset.src,
        title: textOf(el),
        titleSearch: normalize(`${el.id} ${textOf(el)}`),
        isPage: el.classList.contains('page')
    }));
    const pageMap = new Map(pages.map(page => [page.id, page]));
    const pageOrder = pages.map(page => page.id);

    let currentPage = null;
    let activeItem = null;
    let isResizing = false;
    let searchToken = 0;
    const bookmarks = new Set(JSON.parse(localStorage.getItem('mki-bookmarks') || '[]'));
    const contentIndex = new Map();
    let contentIndexPromise = null;
    let searchFrame = null;

    const getSearchMode = () => $('input[name="search-mode"]:checked')?.value || 'title';
    const setClearVisible = visible => { clearBtn.style.display = visible ? 'block' : 'none'; };
    const setBookmarkVisible = visible => { bookmarkBtn.style.visibility = visible ? 'visible' : 'hidden'; };
    const saveBookmarks = () => localStorage.setItem('mki-bookmarks', JSON.stringify([...bookmarks]));

    function showTab(name) {
        tabs.forEach(tab => tab.classList.toggle('active', tab.dataset.tab === name));
        panes.forEach(pane => pane.classList.toggle('active', pane.id === `${name}-content`));

        if (name !== 'search') {
            searchToken++;
            searchBox.value = '';
            searchResults.innerHTML = '';
            setClearVisible(false);
        }
    }

    function updateActiveTreeItem(item) {
        activeItem?.classList.remove('active');
        activeItem = item || null;
        activeItem?.classList.add('active');
    }

    function updateNavButtons() {
        const index = pageOrder.indexOf(currentPage);
        prevBtn.disabled = index <= 0;
        nextBtn.disabled = index < 0 || index >= pageOrder.length - 1;
    }

    function updateBookmarkIcon() {
        const page = pageMap.get(currentPage);
        if (!page?.isPage) {
            setBookmarkVisible(false);
            bookmarkBtn.classList.remove('active');
            return;
        }

        setBookmarkVisible(true);
        bookmarkBtn.classList.toggle('active', bookmarks.has(currentPage));
    }

    function renderBookmarksTab() {
        bookmarksList.innerHTML = [...bookmarks]
            .map(id => pageMap.get(id))
            .filter(page => page?.isPage && page.src)
            .map(page => `<div class="bookmark-item" data-id="${page.id}">${page.title}</div>`)
            .join('');
    }

    function buildBreadcrumb(item) {
        const parts = [];

        for (let node = item; node; ) {
            if (node.classList?.contains('tree-item')) parts.unshift(textOf(node));
            const branch = node.parentElement?.parentElement;
            node = branch ? branch.previousElementSibling : null;
        }

        breadcrumb.innerHTML = parts.map(part => `<span>${part}</span>`).join(' → ');
    }

    function expandPathTo(item) {
        for (let node = item; node && node.id !== 'nav-tree'; node = node.parentElement) {
            if (node.tagName === 'UL') node.style.display = 'block';
            if (node.classList?.contains('folder')) $('.toggle', node).textContent = '▼';
        }
    }

    function loadPage(id, pushHistory = true) {
        const page = pageMap.get(id);
        if (!page) return;

        currentPage = id;
        expandPathTo(page.el);
        if (page.src) contentFrame.src = page.src;

        updateActiveTreeItem(page.el);
        updateNavButtons();
        buildBreadcrumb(page.el);
        updateBookmarkIcon();

        if (pushHistory) history.pushState({ id }, '', `?section=${encodeURIComponent(id)}`);
    }

    function fitFrame() {
        const doc = contentFrame.contentDocument;
        if (!doc) return;

        contentFrame.style.height = '0px';
        contentFrame.style.height = `${Math.max(doc.documentElement.scrollHeight, doc.body?.scrollHeight || 0)}px`;
        doc.documentElement.style.overflow = 'hidden';
        if (doc.body) doc.body.style.overflow = 'hidden';
    }

    function updateSearchPlaceholder() {
        searchBox.placeholder = SEARCH_PLACEHOLDERS[getSearchMode()];
    }

    function stripDocToText(doc) {
        if (!doc) return '';
        doc.querySelectorAll('script,style,noscript').forEach(node => node.remove());
        return (doc.body?.innerText || doc.body?.textContent || doc.documentElement?.textContent || '')
            .replace(/\s+/g, ' ')
            .trim();
    }

    async function loadContentTextByFetch(src) {
        const response = await fetch(src);
        if (!response.ok) throw new Error(response.statusText || src);

        const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
        return stripDocToText(doc);
    }

    function getSearchFrame() {
        if (searchFrame) return searchFrame;

        searchFrame = document.createElement('iframe');
        searchFrame.setAttribute('aria-hidden', 'true');
        searchFrame.tabIndex = -1;
        searchFrame.style.cssText = 'position:absolute;width:0;height:0;border:0;opacity:0;pointer-events:none;';
        document.body.append(searchFrame);
        return searchFrame;
    }

    function loadContentTextByFrame(src) {
        const frame = getSearchFrame();

        return new Promise(resolve => {
            const finish = () => {
                frame.onload = null;
                frame.onerror = null;
                resolve(stripDocToText(frame.contentDocument));
            };

            frame.onload = finish;
            frame.onerror = () => {
                frame.onload = null;
                frame.onerror = null;
                resolve('');
            };
            frame.src = src;
        });
    }

    async function ensureContentIndex() {
        if (contentIndexPromise) return contentIndexPromise;

        const missingPages = pages.filter(page => !contentIndex.has(page.id));
        if (!missingPages.length) return;

        contentIndexPromise = (async () => {
            try {
                await loadContentTextByFetch(missingPages[0].src);

                const loaded = await Promise.all(missingPages.map(async page => ([
                    page.id,
                    await loadContentTextByFetch(page.src)
                ])));

                loaded.forEach(([id, text]) => {
                    contentIndex.set(id, { text, search: normalize(text) });
                });
                return;
            } catch {
                for (const page of missingPages) {
                    const text = await loadContentTextByFrame(page.src);
                    contentIndex.set(page.id, { text, search: normalize(text) });
                }
            }
        })().finally(() => {
            contentIndexPromise = null;
        });

        return contentIndexPromise;
    }

    function buildSnippet(text, query) {
        if (!text) return '';

        const searchText = text.toLowerCase();
        const index = searchText.indexOf(query);
        if (index < 0) return '';

        const start = Math.max(0, index - 55);
        const end = Math.min(text.length, index + query.length + 85);
        return `${start ? '…' : ''}${text.slice(start, end).trim()}${end < text.length ? '…' : ''}`;
    }

    function renderSearchResults(items, mode, query) {
        if (!query) {
            searchResults.innerHTML = '';
            return;
        }

        if (!items.length) {
            searchResults.innerHTML = '<em>Ничего не найдено</em>';
            return;
        }

        searchResults.innerHTML = items.map(page => {
            const snippet = mode === 'content' ? buildSnippet(contentIndex.get(page.id)?.text, query) : '';
            return `
                <div class="result-item" data-id="${page.id}">
                    <div class="result-title">${page.title}</div>
                    ${snippet ? `<div class="result-snippet">${snippet}</div>` : ''}
                </div>
            `;
        }).join('');
    }

    async function runSearch() {
        const token = ++searchToken;
        const query = normalize(searchBox.value);
        const mode = getSearchMode();

        setClearVisible(Boolean(query));
        if (!query) {
            searchResults.innerHTML = '';
            return;
        }

        if (mode === 'title') {
            renderSearchResults(pages.filter(page => page.titleSearch.includes(query)), mode, query);
            return;
        }

        searchResults.innerHTML = '<em>Подготавливается поиск по содержимому…</em>';
        await ensureContentIndex();
        if (token !== searchToken) return;

        renderSearchResults(
            pages.filter(page => contentIndex.get(page.id)?.search.includes(query)),
            mode,
            query
        );
    }

    contentFrame.addEventListener('load', () => {
        fitFrame();

        const doc = contentFrame.contentDocument;
        if (!doc) return;

        $$('img', doc).forEach(img => {
            if (!img.complete) img.addEventListener('load', fitFrame);
        });
        doc.fonts?.ready.then(fitFrame);
    });

    tabs.forEach(tab => tab.addEventListener('click', () => showTab(tab.dataset.tab)));

    navTree.addEventListener('click', event => {
        const item = event.target.closest('.tree-item');
        if (!item) return;

        const isToggleClick = event.target.classList.contains('toggle');
        const hasPage = Boolean(item.dataset.src);

        if (item.classList.contains('folder')) {
            const branch = item.nextElementSibling;
            if (branch) {
                const isOpen = branch.style.display === 'block';
                branch.style.display = isOpen ? 'none' : 'block';
                $('.toggle', item).textContent = isOpen ? '▶' : '▼';
            }

            if (hasPage && !isToggleClick) {
                setBookmarkVisible(false);
                loadPage(item.id);
            }
            return;
        }

        if (hasPage) loadPage(item.id);
    });

    searchBox.addEventListener('input', runSearch);
    searchModes.forEach(mode => mode.addEventListener('change', () => {
        updateSearchPlaceholder();
        runSearch();
    }));

    clearBtn.addEventListener('click', () => {
        searchToken++;
        searchBox.value = '';
        searchResults.innerHTML = '';
        setClearVisible(false);
        searchBox.focus();
    });

    searchResults.addEventListener('click', event => {
        const item = event.target.closest('.result-item');
        if (!item) return;

        showTab('menu');
        loadPage(item.dataset.id);
    });

    bookmarkBtn.addEventListener('click', () => {
        const page = pageMap.get(currentPage);
        if (!page?.isPage || !page.src) return;

        bookmarks.has(currentPage) ? bookmarks.delete(currentPage) : bookmarks.add(currentPage);
        saveBookmarks();
        updateBookmarkIcon();
        renderBookmarksTab();
    });

    bookmarksList.addEventListener('click', event => {
        const item = event.target.closest('.bookmark-item');
        if (!item) return;

        showTab('menu');
        loadPage(item.dataset.id);
    });

    prevBtn.addEventListener('click', () => {
        const index = pageOrder.indexOf(currentPage);
        if (index > 0) loadPage(pageOrder[index - 1]);
    });

    nextBtn.addEventListener('click', () => {
        const index = pageOrder.indexOf(currentPage);
        if (index < pageOrder.length - 1) loadPage(pageOrder[index + 1]);
    });

    resizer.addEventListener('mousedown', event => {
        isResizing = true;
        document.body.style.cursor = 'col-resize';
        event.preventDefault();
    });

    document.addEventListener('mousemove', event => {
        if (!isResizing) return;

        const totalWidth = mainContent.offsetWidth;
        const leftWidth = Math.min(Math.max(event.clientX, totalWidth / 3), totalWidth / 2);

        primaryArea.style.width = `${leftWidth}px`;
        secondaryArea.style.flex = 'none';
        secondaryArea.style.width = `${totalWidth - leftWidth - resizer.offsetWidth}px`;
    });

    document.addEventListener('mouseup', () => {
        isResizing = false;
        document.body.style.cursor = 'default';
    });

    window.addEventListener('popstate', event => {
        loadPage(event.state?.id || pageOrder[0], false);
    });

    $$('ul', navTree).forEach(branch => { branch.style.display = 'none'; });
    $$('.folder .toggle', navTree).forEach(toggle => { toggle.textContent = '▶'; });
    renderBookmarksTab();
    updateSearchPlaceholder();

    const params = new URLSearchParams(location.search);
    const cmd = params.get('cmd');
    const section = params.get('section');
    let startId = pageOrder[0];

    if (cmd) {
        const decoded = decodeURIComponent(cmd).toLowerCase();
        startId = pages.find(page => page.id.toLowerCase() === decoded)?.id || FALLBACK_ID;
    } else if (section && pageMap.has(section)) {
        startId = section;
    }

    loadPage(startId, false);
});
