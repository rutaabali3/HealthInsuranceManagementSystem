(() => {
    const body = document.body;
    const sidebar = document.querySelector(".admin-sidebar");
    const toggle = document.querySelector(".admin-sidebar-toggle");
    const storageKey = "healthinsure-admin-sidebar-collapsed";

    if (!sidebar || !toggle) {
        return;
    }

    const setCollapsed = (isCollapsed) => {
        body.classList.toggle("admin-sidebar-collapsed", isCollapsed);
        toggle.setAttribute("aria-pressed", isCollapsed.toString());
        toggle.setAttribute("aria-label", isCollapsed ? "Expand sidebar" : "Collapse sidebar");
        localStorage.setItem(storageKey, isCollapsed ? "true" : "false");
    };

    setCollapsed(localStorage.getItem(storageKey) === "true");

    toggle.addEventListener("click", () => {
        setCollapsed(!body.classList.contains("admin-sidebar-collapsed"));
    });
})();
