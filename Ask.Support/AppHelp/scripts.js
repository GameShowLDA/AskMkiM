document.addEventListener('DOMContentLoaded', () => {
    const $ = (selector, root = document) => root.querySelector(selector);
    const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
    const textOf = el => el?.textContent.trim() || '';
    const normalizeWhitespace = text => String(text ?? '').replace(/\s+/g, ' ').trim();
    const normalize = (text, caseSensitive = false) => {
        const normalized = normalizeWhitespace(text);
        return caseSensitive ? normalized : normalized.toLocaleLowerCase('ru-RU');
    };

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
    const searchOptions = $$('input[name="search-option"]');
    const caseSensitiveOption = $('#search-case-sensitive');
    const wholeWordsOption = $('#search-whole-words');
    const resizer = $('#resizer');
    const primaryArea = $('.primary-area');
    const secondaryArea = $('.secondary-area');
    const contentFrame = $('#content-frame');
    const mainContent = $('.main-content');
    const themeToggle = $('#theme-toggle');
    const sidebarToggle = $('#sidebar-toggle');

    const FALLBACK_ID = 'GeneralInformation';
    const THEME_STORAGE_KEY = 'mki-help-theme';
    const THEME_COOKIE = 'mki_help_theme';
    const SIDEBAR_STORAGE_KEY = 'mki-help-sidebar-collapsed';
    const BOOKMARKS_STORAGE_KEY = 'mki-help-bookmarks-v1';
    const LEGACY_BOOKMARKS_STORAGE_KEY = 'mki-bookmarks';
    const BOOKMARKS_COOKIE = 'mki_help_bookmarks';
    const BOOKMARKS_PARAM = 'bookmarks';
    const SEARCH_PLACEHOLDERS = {
        title: 'Введите название раздела…',
        content: 'Введите текст для поиска по содержимому…'
    };

    const pages = $$('.tree-item[data-src]', navTree).map(el => ({
        id: el.id,
        el,
        src: el.dataset.src,
        title: textOf(el),
        titleSearch: normalizeWhitespace(`${el.id} ${textOf(el)}`),
        isPage: el.classList.contains('page')
    }));
    const pageMap = new Map(pages.map(page => [page.id, page]));
    const pageOrder = pages.map(page => page.id);

    let currentPage = null;
    let activeItem = null;
    let isResizing = false;
    let searchToken = 0;
    let searchTimer = null;
    const bookmarks = new Set(readBookmarks());
    const contentIndex = new Map();
    let contentIndexPromise = null;
    let searchFrame = null;
    let pendingSelectionQuery = '';
    let pendingSelectionOptions = null;
    let currentTheme = 'light';

    const getSearchMode = () => $('input[name="search-mode"]:checked')?.value || 'title';
    const getSearchOptions = () => ({
        caseSensitive: Boolean(caseSensitiveOption?.checked),
        wholeWords: Boolean(wholeWordsOption?.checked)
    });
    const getTheme = () => currentTheme;
    const setClearVisible = visible => { clearBtn.style.display = visible ? 'block' : 'none'; };
    const setBookmarkVisible = visible => { bookmarkBtn.style.visibility = visible ? 'visible' : 'hidden'; };
    const bookmarkIds = () => [...bookmarks].filter(id => pageMap.get(id)?.isPage);
    const getBranch = folder => folder?.nextElementSibling?.tagName === 'UL' ? folder.nextElementSibling : null;

    function readStoredJson(key) {
        try {
            return JSON.parse(localStorage.getItem(key) || '[]');
        } catch {
            return [];
        }
    }

    function writeStoredJson(key, value) {
        try {
            localStorage.setItem(key, JSON.stringify(value));
        } catch {
            // Остальные хранилища всё равно сохранят закладки.
        }
    }

    function readStoredTheme() {
        let storedTheme = '';

        try {
            storedTheme = localStorage.getItem(THEME_STORAGE_KEY) || '';
        } catch {
            storedTheme = '';
        }

        if (storedTheme === 'dark' || storedTheme === 'light') return storedTheme;

        const cookieTheme = getCookie(THEME_COOKIE);
        return cookieTheme === 'dark' ? 'dark' : 'light';
    }

    function writeStoredTheme(theme) {
        try {
            localStorage.setItem(THEME_STORAGE_KEY, theme);
        } catch {
            // Cookie ниже останется запасным вариантом хранения.
        }

        setCookie(THEME_COOKIE, theme);
    }

    function applyThemeToFrame() {
        const doc = contentFrame.contentDocument;
        if (!doc?.body) return;

        doc.body.classList.toggle('dark-theme', getTheme() === 'dark');
    }

    function updateThemeButton(theme) {
        if (!themeToggle) return;

        const isDark = theme === 'dark';
        $('.theme-toggle-icon', themeToggle).textContent = isDark ? '☀️' : '🌙';
        $('.theme-toggle-text', themeToggle).textContent = isDark ? 'Светлая тема' : 'Темная тема';
        themeToggle.setAttribute('aria-label', isDark ? 'Включить светлую тему' : 'Включить темную тему');
        themeToggle.setAttribute('aria-pressed', String(isDark));
    }

    function applyTheme(theme = getTheme()) {
        currentTheme = theme;
        document.body.classList.toggle('dark-theme', theme === 'dark');
        updateThemeButton(theme);
        applyThemeToFrame();
    }

    function toggleTheme() {
        const theme = getTheme() === 'dark' ? 'light' : 'dark';
        writeStoredTheme(theme);
        applyTheme(theme);
    }

    function readSidebarCollapsed() {
        try {
            return localStorage.getItem(SIDEBAR_STORAGE_KEY) === 'true';
        } catch {
            return false;
        }
    }

    function writeSidebarCollapsed(collapsed) {
        try {
            localStorage.setItem(SIDEBAR_STORAGE_KEY, String(collapsed));
        } catch {
            // При недоступном localStorage состояние всё равно работает до закрытия страницы.
        }
    }

    function updateSidebarToggle(collapsed) {
        if (!sidebarToggle) return;

        sidebarToggle.setAttribute('aria-expanded', String(!collapsed));
        sidebarToggle.setAttribute(
            'aria-label',
            collapsed ? 'Показать панель навигации' : 'Свернуть панель навигации'
        );
        sidebarToggle.title = collapsed ? 'Показать меню' : 'Свернуть меню';
        $('.sidebar-toggle-icon', sidebarToggle).textContent = collapsed ? '▶' : '◀';
        $('.sidebar-toggle-text', sidebarToggle).textContent = collapsed ? 'Показать меню' : 'Свернуть меню';
    }

    function applySidebarState(collapsed) {
        mainContent.classList.toggle('sidebar-collapsed', collapsed);
        updateSidebarToggle(collapsed);
        requestAnimationFrame(fitFrame);
    }

    function toggleSidebar() {
        const collapsed = !mainContent.classList.contains('sidebar-collapsed');
        writeSidebarCollapsed(collapsed);
        applySidebarState(collapsed);
    }

    function encodeBookmarks(ids) {
        return ids.map(encodeURIComponent).join(',');
    }

    function decodeBookmarks(value) {
        if (!value) return [];

        return value.split(',')
            .map(id => {
                try {
                    return decodeURIComponent(id);
                } catch {
                    return id;
                }
            })
            .filter(Boolean);
    }

    function getCookie(name) {
        try {
            return document.cookie
                .split('; ')
                .find(row => row.startsWith(`${name}=`))
                ?.slice(name.length + 1) || '';
        } catch {
            return '';
        }
    }

    function setCookie(name, value) {
        try {
            const maxAge = 60 * 60 * 24 * 365 * 10;
            document.cookie = `${name}=${value};path=/;max-age=${maxAge};SameSite=Lax`;
        } catch {
            // В file:// cookie может быть недоступен, но URL и localStorage останутся.
        }
    }

    function readBookmarks() {
        const params = new URLSearchParams(location.search);
        const ids = [
            ...readStoredJson(BOOKMARKS_STORAGE_KEY),
            ...readStoredJson(LEGACY_BOOKMARKS_STORAGE_KEY),
            ...decodeBookmarks(getCookie(BOOKMARKS_COOKIE)),
            ...decodeBookmarks(params.get(BOOKMARKS_PARAM))
        ];

        return [...new Set(ids)].filter(id => pageMap.get(id)?.isPage);
    }

    function updateAddress(id = currentPage, replace = false) {
        const params = new URLSearchParams(location.search);
        const encodedBookmarks = encodeBookmarks(bookmarkIds());

        params.delete('cmd');
        if (id) params.set('section', id);
        encodedBookmarks ? params.set(BOOKMARKS_PARAM, encodedBookmarks) : params.delete(BOOKMARKS_PARAM);

        const query = params.toString();
        const url = `${location.pathname}${query ? `?${query}` : ''}${location.hash}`;
        try {
            history[replace ? 'replaceState' : 'pushState']({ id }, '', url);
        } catch {
            // Некоторые встроенные браузеры запрещают менять адрес, это не должно ломать закладки.
        }
    }

    function saveBookmarks() {
        const ids = bookmarkIds();

        bookmarks.clear();
        ids.forEach(id => bookmarks.add(id));

        writeStoredJson(BOOKMARKS_STORAGE_KEY, ids);
        writeStoredJson(LEGACY_BOOKMARKS_STORAGE_KEY, ids);
        setCookie(BOOKMARKS_COOKIE, encodeBookmarks(ids));
        updateAddress(currentPage, true);
    }

    function showTab(name) {
        tabs.forEach(tab => tab.classList.toggle('active', tab.dataset.tab === name));
        panes.forEach(pane => pane.classList.toggle('active', pane.id === `${name}-content`));

        if (name !== 'search') {
            clearTimeout(searchTimer);
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

    function setFolderOpen(folder, isOpen) {
        if (!folder?.classList.contains('folder')) return;

        const branch = getBranch(folder);
        const open = Boolean(branch && isOpen);
        const toggle = $('.toggle', folder);

        folder.classList.toggle('is-open', open);
        folder.setAttribute('aria-expanded', branch ? String(open) : 'false');
        if (toggle) toggle.textContent = branch ? (open ? '▼' : '▶') : '';
        if (branch) branch.style.display = open ? 'block' : 'none';
    }

    function syncFolderToggles() {
        $$('.tree-item.folder', navTree).forEach(folder => {
            setFolderOpen(folder, getBranch(folder)?.style.display === 'block');
        });
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
            if (node.tagName === 'UL') setFolderOpen(node.previousElementSibling, true);
        }
    }

    function loadPage(id, pushHistory = true, selectionQuery = '', selectionOptions = null) {
        const page = pageMap.get(id);
        if (!page) return;

        pendingSelectionQuery = selectionQuery.trim();
        pendingSelectionOptions = pendingSelectionQuery ? { ...selectionOptions } : null;

        currentPage = id;
        expandPathTo(page.el);

        if (page.src) {
            contentFrame.src = page.src;
        }

        updateActiveTreeItem(page.el);
        updateNavButtons();
        buildBreadcrumb(page.el);
        updateBookmarkIcon();

        if (pushHistory) updateAddress(id);
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
        return (doc.body?.textContent || doc.documentElement?.textContent || '')
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
        searchFrame.setAttribute('sandbox', 'allow-same-origin');
        searchFrame.tabIndex = -1;
        searchFrame.style.cssText = 'position:absolute;width:0;height:0;border:0;opacity:0;pointer-events:none;';
        document.body.append(searchFrame);
        return searchFrame;
    }

    function loadContentTextByFrame(src) {
        const frame = getSearchFrame();

        return new Promise(resolve => {
            const finish = text => {
                clearTimeout(timeout);
                frame.onload = null;
                frame.onerror = null;
                resolve(text);
            };
            const timeout = setTimeout(() => finish(null), 5000);
            frame.onload = () => {
                try {
                    finish(frame.contentDocument ? stripDocToText(frame.contentDocument) : null);
                } catch {
                    finish(null);
                }
            };
            frame.onerror = () => finish(null);
            frame.src = src;
        });
    }

    async function ensureContentIndex() {
        if (contentIndexPromise) return contentIndexPromise;

        const missingPages = pages.filter(page => !contentIndex.has(page.id));
        if (!missingPages.length) return;

        contentIndexPromise = (async () => {
            try {
                // file:// не везде разрешает fetch; только для него нужен запасной iframe.
                if (location.protocol === 'file:') {
                    for (const page of missingPages) {
                        const text = await loadContentTextByFrame(page.src);
                        if (text !== null) contentIndex.set(page.id, text);
                    }
                    return;
                }

                let next = 0;
                const worker = async () => {
                    while (next < missingPages.length) {
                        const page = missingPages[next++];
                        try {
                            // Сразу сохраняем текст и освобождаем DOM каждой статьи.
                            contentIndex.set(page.id, await loadContentTextByFetch(page.src));
                        } catch {
                            // Повторим только эту статью при следующем поиске.
                        }
                    }
                };
                await Promise.all(Array.from({ length: Math.min(4, missingPages.length) }, worker));
            } finally {
                searchFrame?.remove();
                searchFrame = null;
            }
        })().finally(() => {
            contentIndexPromise = null;
        });

        return contentIndexPromise;
    }

    function isWordCharacter(character) {
        return Boolean(character && /[\p{L}\p{N}_]/u.test(character));
    }

    function findMatchIndex(text, query, options = {}) {
        const source = normalizeWhitespace(text);
        const pattern = normalizeWhitespace(query);
        if (!source || !pattern) return -1;

        const searchableSource = options.caseSensitive
            ? source
            : source.toLocaleLowerCase('ru-RU');
        const searchablePattern = options.caseSensitive
            ? pattern
            : pattern.toLocaleLowerCase('ru-RU');
        const requireLeadingBoundary = options.wholeWords && isWordCharacter(pattern[0]);
        const requireTrailingBoundary = options.wholeWords && isWordCharacter(pattern.at(-1));

        for (let fromIndex = 0; fromIndex <= searchableSource.length - searchablePattern.length;) {
            const index = searchableSource.indexOf(searchablePattern, fromIndex);
            if (index < 0) return -1;

            const before = index > 0 ? source[index - 1] : '';
            const after = source[index + pattern.length] || '';
            const leadingBoundaryMatches = !requireLeadingBoundary || !isWordCharacter(before);
            const trailingBoundaryMatches = !requireTrailingBoundary || !isWordCharacter(after);

            if (leadingBoundaryMatches && trailingBoundaryMatches) return index;
            fromIndex = index + 1;
        }

        return -1;
    }

    function matchesSearch(text, query, options) {
        return findMatchIndex(text, query, options) >= 0;
    }

    function buildSnippet(text, query, options) {
        if (!text) return '';

        const index = findMatchIndex(text, query, options);
        if (index < 0) return '';

        const start = Math.max(0, index - 55);
        const end = Math.min(text.length, index + query.length + 85);
        return `${start ? '…' : ''}${text.slice(start, end).trim()}${end < text.length ? '…' : ''}`;
    }

    function renderSearchResults(items, mode, query, options) {
        if (!query) {
            searchResults.innerHTML = '';
            return;
        }

        if (!items.length) {
            searchResults.innerHTML = '<em>Ничего не найдено</em>';
            return;
        }

        const summary = `<div class="search-summary">Найдено: ${items.length}</div>`;
        searchResults.innerHTML = summary + items.map(page => {
            const snippet = mode === 'content'
                ? buildSnippet(contentIndex.get(page.id), query, options)
                : '';
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
        const query = normalizeWhitespace(searchBox.value);
        const mode = getSearchMode();
        const options = getSearchOptions();

        setClearVisible(Boolean(query));
        if (!query) {
            searchResults.innerHTML = '';
            return;
        }

        if (mode === 'title') {
            renderSearchResults(
                pages.filter(page => matchesSearch(page.titleSearch, query, options)),
                mode,
                query,
                options
            );
            return;
        }

        searchResults.innerHTML = '<em>Подготавливается поиск по содержимому…</em>';
        await ensureContentIndex();
        if (token !== searchToken) return;

        renderSearchResults(
            pages.filter(page => matchesSearch(contentIndex.get(page.id), query, options)),
            mode,
            query,
            options
        );
    }

    function findTextRange(doc, query, options = {}) {
        if (!doc?.body) return null;
        const pattern = normalize(query || '', options.caseSensitive);
        if (!pattern) return null;

        // KMP walks text nodes until the first match. Only query-sized buffers are
        // retained, instead of allocating a mapping object for every page character.
        const prefix = new Uint32Array(pattern.length);
        for (let i = 1, j = 0; i < pattern.length; i++) {
            while (j && pattern[i] !== pattern[j]) j = prefix[j - 1];
            if (pattern[i] === pattern[j]) j++;
            prefix[i] = j;
        }
        const capacity = pattern.length + 1;
        const nodes = new Array(capacity);
        const offsets = new Uint32Array(capacity);
        const characters = new Array(capacity);
        const walker = doc.createTreeWalker(doc.body, NodeFilter.SHOW_TEXT, {
            acceptNode: node => node.parentElement?.closest('script,style,noscript')
                ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT
        });
        let count = 0;
        let matched = 0;
        let whitespace = true;
        let pendingRange = null;
        let node;
        while ((node = walker.nextNode())) {
            const text = node.nodeValue;
            for (let offset = 0; offset < text.length; offset++) {
                const isSpace = /\s/.test(text[offset]);
                if (isSpace && whitespace) continue;
                whitespace = isSpace;
                const chars = isSpace
                    ? ' '
                    : options.caseSensitive
                        ? text[offset]
                        : text[offset].toLocaleLowerCase('ru-RU');
                for (let i = 0; i < chars.length; i++) {
                    const ch = chars[i];

                    if (pendingRange) {
                        if (!isWordCharacter(ch)) return pendingRange;
                        pendingRange = null;
                    }

                    const slot = count++ % capacity;
                    nodes[slot] = node;
                    offsets[slot] = offset;
                    characters[slot] = ch;
                    while (matched && ch !== pattern[matched]) matched = prefix[matched - 1];
                    if (ch === pattern[matched]) matched++;
                    if (matched === pattern.length) {
                        const startIndex = count - pattern.length;
                        const start = startIndex % capacity;
                        const before = startIndex > 0
                            ? characters[(startIndex - 1) % capacity]
                            : '';
                        const leadingBoundaryMatches = !options.wholeWords ||
                            !isWordCharacter(pattern[0]) ||
                            !isWordCharacter(before);
                        const range = doc.createRange();
                        range.setStart(nodes[start], offsets[start]);
                        range.setEnd(node, offset + 1);

                        if (leadingBoundaryMatches) {
                            if (!options.wholeWords || !isWordCharacter(pattern.at(-1))) return range;
                            pendingRange = range;
                        }

                        matched = prefix[matched - 1];
                    }
                }
            }
        }
        return pendingRange;
    }

    function scrollFrameSelectionIntoView(range) {
        if (!range) return;

        const rangeRect = range.getBoundingClientRect();
        const frameRect = contentFrame.getBoundingClientRect();
        const scrollContainer = getScrollableParent(contentFrame);

        const targetTopInViewport = frameRect.top + rangeRect.top;

        if (scrollContainer === document.scrollingElement || scrollContainer === document.documentElement) {
            window.scrollTo({
                top: Math.max(0, window.scrollY + targetTopInViewport - window.innerHeight / 3),
                behavior: 'smooth'
            });

            return;
        }

        const containerRect = scrollContainer.getBoundingClientRect();
        const targetTopInContainer =
            scrollContainer.scrollTop +
            targetTopInViewport -
            containerRect.top;

        scrollContainer.scrollTo({
            top: Math.max(0, targetTopInContainer - scrollContainer.clientHeight / 3),
            behavior: 'smooth'
        });
    }

    function getScrollableParent(element) {
        for (let parent = element.parentElement; parent; parent = parent.parentElement) {
            const style = getComputedStyle(parent);
            const overflowY = style.overflowY;

            const canScroll =
                (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') &&
                parent.scrollHeight > parent.clientHeight;

            if (canScroll) {
                return parent;
            }
        }

        return document.scrollingElement || document.documentElement;
    }

    function selectTextInContentFrame(query, options) {
        const doc = contentFrame.contentDocument;
        const win = contentFrame.contentWindow;

        if (!doc || !win) return;

        const selection = win.getSelection();
        selection?.removeAllRanges();

        if (!query) return;

        const range = findTextRange(doc, query, options);
        if (!range) return;

        selection.addRange(range);
        scrollFrameSelectionIntoView(range);
    }

    contentFrame.addEventListener('load', () => {
        applyThemeToFrame();
        fitFrame();

        const doc = contentFrame.contentDocument;
        if (!doc) return;

        doc.addEventListener('toggle', () => fitFrame(), true);

        requestAnimationFrame(() => {
            fitFrame();
            selectTextInContentFrame(pendingSelectionQuery, pendingSelectionOptions);
        });

        $$('img', doc).forEach(img => {
            if (!img.complete) {
                img.addEventListener('load', () => {
                    fitFrame();
                    selectTextInContentFrame(pendingSelectionQuery, pendingSelectionOptions);
                });
            }
        });

        doc.fonts?.ready.then(() => {
            fitFrame();
            selectTextInContentFrame(pendingSelectionQuery, pendingSelectionOptions);
        });
    });

    tabs.forEach(tab => tab.addEventListener('click', () => showTab(tab.dataset.tab)));

    navTree.addEventListener('click', event => {
        const item = event.target.closest('.tree-item');
        if (!item) return;

        const isToggleClick = event.target.classList.contains('toggle');
        const hasPage = Boolean(item.dataset.src);

        if (item.classList.contains('folder')) {
            setFolderOpen(item, !item.classList.contains('is-open'));

            if (hasPage && !isToggleClick) {
                setBookmarkVisible(false);
                loadPage(item.id);
            }
            return;
        }

        if (hasPage) loadPage(item.id);
    });

    searchBox.addEventListener('input', () => {
        clearTimeout(searchTimer);
        searchToken++;
        setClearVisible(Boolean(searchBox.value));
        searchTimer = setTimeout(runSearch, 150);
    });
    searchModes.forEach(mode => mode.addEventListener('change', () => {
        clearTimeout(searchTimer);
        updateSearchPlaceholder();
        runSearch();
    }));
    searchOptions.forEach(option => option.addEventListener('change', () => {
        clearTimeout(searchTimer);
        runSearch();
    }));

    clearBtn.addEventListener('click', () => {
        clearTimeout(searchTimer);
        searchToken++;
        searchBox.value = '';
        searchResults.innerHTML = '';
        setClearVisible(false);
        searchBox.focus();
    });

    searchResults.addEventListener('click', event => {
        const item = event.target.closest('.result-item');
        if (!item) return;

        const isContentSearch = getSearchMode() === 'content';
        const queryForSelection = isContentSearch
            ? searchBox.value.trim()
            : '';
        const optionsForSelection = isContentSearch ? getSearchOptions() : null;

        showTab('menu');
        loadPage(item.dataset.id, true, queryForSelection, optionsForSelection);
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

    themeToggle?.addEventListener('click', toggleTheme);
    sidebarToggle?.addEventListener('click', toggleSidebar);

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
        readBookmarks().forEach(id => bookmarks.add(id));
        renderBookmarksTab();
        loadPage(event.state?.id || pageOrder[0], false);
    });

    $$('ul', navTree).forEach(branch => { branch.style.display = 'none'; });
    syncFolderToggles();
    renderBookmarksTab();
    updateSearchPlaceholder();
    applyTheme(readStoredTheme());
    applySidebarState(readSidebarCollapsed());

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
    saveBookmarks();
});
