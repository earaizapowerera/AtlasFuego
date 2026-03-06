// Inject custom menu items into sidebar after dynamic menu loads
(function() {
    const orig = window.renderMenu;
    if (typeof orig === 'function') {
        window.renderMenu = function(menuItems) {
            menuItems.push({
                name: 'Email Agradecimiento',
                icon: 'fas fa-heart',
                routePath: '/Agradecimiento',
                isModule: false,
                objectId: 'email-agradecimiento',
                permission: 1,
                children: []
            });
            menuItems.push({
                name: 'App Check-in',
                icon: 'fas fa-mobile-alt',
                routePath: '/App',
                isModule: false,
                objectId: 'app-checkin-download',
                permission: 1,
                children: []
            });
            orig(menuItems);
        };
    }
})();
