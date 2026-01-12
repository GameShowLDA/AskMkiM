(function () {
    document.addEventListener('click', (e) => {
        const link = e.target.closest('a[data-nav]');
        if (!link) return;

        e.preventDefault();

        const id = link.getAttribute('data-nav');
        if (!id) return;

        // Отправляем родителю команду переключить “вкладку/страницу” по id
        window.parent.postMessage({ type: 'mki:navigate', id }, '*');
    });
})();