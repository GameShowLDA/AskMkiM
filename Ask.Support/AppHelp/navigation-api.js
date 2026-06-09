(function () {
    const escapeId = id => window.CSS?.escape ? CSS.escape(id) : id.replace(/["\\]/g, '\\$&');
    const navigateTo = id => document
        .getElementById('nav-tree')
        ?.querySelector(`.tree-item#${escapeId(id)}`)
        ?.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    window.mkiHelp = { ...(window.mkiHelp || {}), navigateTo };
    window.addEventListener('message', ({ data }) => {
        if (data?.type === 'mki:navigate' && typeof data.id === 'string' && data.id) navigateTo(data.id);
    });
})();
