(function () {
    function navigateTo(id) {
        const navTree = document.getElementById('nav-tree');
        if (!navTree) return;

        // CSS.escape нужен для безопасного querySelector по id
        const safeId = (window.CSS && CSS.escape) ? CSS.escape(id) : id.replace(/"/g, '\\"');
        const item = navTree.querySelector(`.tree-item[id="${safeId}"]`);
        if (!item || !item.dataset || !item.dataset.src) return;

        // Используем существующую логику клика в app.js
        item.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    }

    // Публичный API
    window.mkiHelp = window.mkiHelp || {};
    window.mkiHelp.navigateTo = navigateTo;

    // Прием запросов из iframe
    window.addEventListener('message', (e) => {
        const d = e.data;
        if (!d || d.type !== 'mki:navigate') return;
        if (typeof d.id !== 'string' || !d.id) return;

        navigateTo(d.id);
    });
})();