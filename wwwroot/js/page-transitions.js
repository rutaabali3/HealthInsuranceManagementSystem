(() => {
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (prefersReducedMotion) {
        return;
    }

    const body = document.body;
    const transitionMs = 500;
    const launchMs = 2000;
    const loaderStartedKey = "healthinsure-loader-started-at";
    const storedStartedAt = Number(sessionStorage.getItem(loaderStartedKey));
    const launchStartedAt = Number.isFinite(storedStartedAt) && storedStartedAt > 0
        ? storedStartedAt
        : Date.now();

    const showLoader = () => {
        body.classList.add("loader-active", "loader-lock");
    };

    const hideLoader = () => {
        body.classList.remove("loader-active", "loader-lock");
    };

    const hideAfterMinimumLaunchTime = () => {
        const elapsed = Date.now() - launchStartedAt;
        const remaining = Math.max(launchMs - elapsed, 0);

        window.setTimeout(() => {
            sessionStorage.removeItem(loaderStartedKey);
            hideLoader();
        }, remaining);
    };

    showLoader();
    hideAfterMinimumLaunchTime();

    window.addEventListener("pageshow", () => {
        body.classList.remove("is-page-leaving");
        hideAfterMinimumLaunchTime();
    });

    document.addEventListener("click", (event) => {
        const anchor = event.target.closest("a[href]");

        if (!anchor || event.defaultPrevented || event.button !== 0) {
            return;
        }

        if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
            return;
        }

        if (anchor.target && anchor.target !== "_self") {
            return;
        }

        if (anchor.hasAttribute("download") || anchor.dataset.noTransition === "true") {
            return;
        }

        const href = anchor.getAttribute("href");

        if (!href || href.startsWith("#")) {
            return;
        }

        const nextUrl = new URL(anchor.href, window.location.href);
        const currentUrl = new URL(window.location.href);
        const sameDocument = nextUrl.origin === currentUrl.origin
            && nextUrl.pathname === currentUrl.pathname
            && nextUrl.search === currentUrl.search
            && nextUrl.hash;

        if (nextUrl.origin !== currentUrl.origin || sameDocument) {
            return;
        }

        event.preventDefault();
        body.classList.add("is-page-leaving");
        sessionStorage.setItem(loaderStartedKey, Date.now().toString());
        showLoader();

        window.setTimeout(() => {
            window.location.href = nextUrl.href;
        }, transitionMs);
    });

    document.addEventListener("submit", (event) => {
        window.setTimeout(() => {
            if (event.defaultPrevented || event.target?.dataset?.noTransition === "true") {
                return;
            }

            body.classList.add("is-page-leaving");
            sessionStorage.setItem(loaderStartedKey, Date.now().toString());
            showLoader();
        }, 0);
    });
})();
