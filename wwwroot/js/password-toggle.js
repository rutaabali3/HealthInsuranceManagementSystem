(function () {
    document.querySelectorAll("[data-password-toggle]").forEach((toggle) => {
        const selector = toggle.getAttribute("data-password-toggle");
        const input = selector ? document.querySelector(selector) : null;
        const icon = toggle.querySelector(".bi");

        if (!input || !icon) return;

        toggle.addEventListener("click", () => {
            const shouldShow = input.type === "password";
            input.type = shouldShow ? "text" : "password";
            toggle.setAttribute("aria-pressed", shouldShow.toString());
            toggle.setAttribute("aria-label", shouldShow ? "Hide password" : "Show password");
            toggle.setAttribute("title", shouldShow ? "Hide password" : "Show password");
            icon.className = shouldShow ? "bi bi-eye-slash" : "bi bi-eye";
        });
    });
})();
