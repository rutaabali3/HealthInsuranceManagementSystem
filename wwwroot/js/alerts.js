(function () {
    "use strict";

    const theme = getComputedStyle(document.documentElement);
    const colors = {
        primary: theme.getPropertyValue("--md-primary").trim() || "#006a6a",
        onPrimary: theme.getPropertyValue("--md-on-primary").trim() || "#ffffff",
        onSurface: theme.getPropertyValue("--md-on-surface").trim() || "#171d1d",
        surface: theme.getPropertyValue("--md-surface-container-lowest").trim() || "#ffffff",
        success: theme.getPropertyValue("--md-success").trim() || "#146c2e",
        error: theme.getPropertyValue("--md-error").trim() || "#ba1a1a",
        warning: "#f3cb54"
    };

    const hasTable = () => Boolean(document.querySelector(".table"));
    const isTableAction = (element) => Boolean(element.closest(".table"));
    const getPosition = (element) => isTableAction(element) || hasTable() ? "top-end" : "center";

    const themedClasses = (type) => ({
        popup: `app-swal app-swal-${type}`,
        confirmButton: "app-swal-confirm-button",
        cancelButton: "app-swal-cancel-button"
    });

    function fireSweetAlert(options) {
        if (!window.Swal) {
            return;
        }

        return Swal.fire({
            background: colors.surface,
            color: colors.onSurface,
            confirmButtonColor: colors.primary,
            cancelButtonColor: colors.surface,
            buttonsStyling: false,
            showClass: {
                popup: "swal2-show app-swal-enter"
            },
            hideClass: {
                popup: "swal2-hide app-swal-exit"
            },
            ...options
        });
    }

    function showFlash(type, message) {
        if (!message) {
            return;
        }

        const isTablePage = hasTable();
        fireSweetAlert({
            icon: type,
            title: type === "success" ? "Success" : "Error",
            text: message,
            position: isTablePage ? "top-end" : "center",
            showConfirmButton: true,
            confirmButtonText: "OK",
            customClass: themedClasses(type)
        });
    }

    function showValidationSummary(root) {
        const scope = root || document;
        const summary = scope.querySelector(".app-validation-summary.validation-summary-errors");

        if (!summary) {
            return;
        }

        const message = Array.from(summary.querySelectorAll("li"))
            .map((item) => item.textContent.trim())
            .filter(Boolean)
            .join("\n") || summary.textContent.trim();

        if (!message || summary.dataset.swalShown === "true") {
            return;
        }

        summary.dataset.swalShown = "true";
        showFlash("error", message);
    }

    function attachConfirmations() {
        document.querySelectorAll("form[data-swal-confirm]").forEach((form) => {
            form.addEventListener("submit", (event) => {
                if (form.dataset.swalConfirmed === "true") {
                    return;
                }

                event.preventDefault();

                fireSweetAlert({
                    icon: "warning",
                    title: form.dataset.swalTitle || "Confirm action",
                    text: form.dataset.swalConfirm,
                    position: getPosition(form),
                    showCancelButton: true,
                    confirmButtonText: form.dataset.swalConfirmButton || "Confirm",
                    cancelButtonText: "Cancel",
                    customClass: themedClasses("confirm")
                }).then((result) => {
                    if (!result.isConfirmed) {
                        return;
                    }

                    form.dataset.swalConfirmed = "true";
                    form.submit();
                });
            });
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        const messages = window.healthAlertMessages || {};
        showFlash("success", messages.success);
        showFlash("error", messages.error);
        showValidationSummary();
        attachConfirmations();

        document.querySelectorAll("form").forEach((form) => {
            form.addEventListener("submit", () => {
                window.setTimeout(() => showValidationSummary(form), 0);
            });
        });
    });
})();
