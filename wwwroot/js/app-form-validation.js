(function () {
    "use strict";

    const strongPasswordPattern = /^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$/;
    const namePattern = /^[A-Za-z][A-Za-z\s'-]*$/;
    const usernamePattern = /^[A-Za-z0-9._-]+$/;
    const pakistanPhonePattern = /^(\+92|0)\d{10}$/;
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const websitePattern = /^https?:\/\/[^\s.]+\.[^\s]{2,}$/i;

    const requiredNames = new Set([
        "FirstName", "LastName", "Username", "Password", "Email", "Phone", "ContactNumber",
        "Department", "Designation", "Role", "DateOfBirth", "Gender", "CompanyName",
        "Address", "CompanyId", "PolicyName", "PolicyType", "PremiumAmount",
        "CoverageAmount", "DurationMonths", "PolicyId", "StartDate", "EndDate",
        "BillAmount", "CurrentPassword", "NewPassword", "ConfirmPassword",
        "ContactName", "ContactEmail", "ContactMessage"
    ]);

    const fieldLabel = (field) => {
        const form = field.closest("form");
        const label = form?.querySelector(`label[for="${CSS.escape(field.id)}"]`);
        return (label?.textContent || field.name || "This field").replace(/\s*\(.*?\)\s*/g, "").trim();
    };

    const todayString = () => {
        const today = new Date();
        today.setMinutes(today.getMinutes() - today.getTimezoneOffset());
        return today.toISOString().slice(0, 10);
    };

    const adultCutoffString = () => {
        const cutoff = new Date();
        cutoff.setFullYear(cutoff.getFullYear() - 18);
        cutoff.setMinutes(cutoff.getMinutes() - cutoff.getTimezoneOffset());
        return cutoff.toISOString().slice(0, 10);
    };

    function normalizePhone(value) {
        return value.replace(/[\s-]/g, "");
    }

    function fieldAnchor(field) {
        return field.closest(".app-field-group, .login-input-group, .input-group") || field;
    }

    function feedbackElement(field) {
        const anchor = fieldAnchor(field);
        let feedback = anchor.nextElementSibling;
        while (feedback && feedback.matches("[data-valmsg-for], .field-validation-valid, .field-validation-error, .text-danger")) {
            feedback = feedback.nextElementSibling;
        }

        if (!feedback || !feedback.classList.contains("app-field-feedback")) {
            feedback = document.createElement("div");
            feedback.className = "app-field-feedback";
            anchor.insertAdjacentElement("afterend", feedback);
        }

        return feedback;
    }

    function setFeedback(field, type, message) {
        const anchor = fieldAnchor(field);
        const feedback = feedbackElement(field);
        anchor.classList.remove("app-field-valid", "app-field-error", "app-field-warning");
        field.classList.remove("is-valid", "is-invalid");

        if (!type || !message) {
            feedback.hidden = true;
            feedback.textContent = "";
            return;
        }

        anchor.classList.add(`app-field-${type}`);
        if (type === "success") {
            field.classList.add("is-valid");
        }
        if (type === "error") {
            field.classList.add("is-invalid");
        }

        const icon = type === "success" ? "bi-check-circle-fill" : type === "warning" ? "bi-exclamation-triangle-fill" : "bi-exclamation-circle-fill";
        feedback.className = `app-field-feedback app-field-feedback-${type}`;
        feedback.innerHTML = `<i class="bi ${icon}" aria-hidden="true"></i><span>${message}</span>`;
        feedback.hidden = false;
    }

    function isRequired(field) {
        return field.required || field.dataset.valRequired || requiredNames.has(field.name);
    }

    function isPasswordStrengthField(field) {
        if (field.name === "NewPassword") {
            return true;
        }

        if (field.name !== "Password") {
            return false;
        }

        const page = document.body.dataset.page;
        return page === "admin-registeremployee" || page === "auth-resetpassword" || field.dataset.requireStrongPassword === "true";
    }

    function matchingPasswordField(form, field) {
        if (field.name !== "ConfirmPassword") {
            return null;
        }

        return form.querySelector('[name="NewPassword"]') || form.querySelector('[name="Password"]');
    }

    function checkField(form, field) {
        const name = field.name;
        const label = fieldLabel(field);
        const raw = field.value.trim();
        const value = name === "Phone" || name === "ContactNumber" ? normalizePhone(raw) : raw;

        if (field.type === "hidden" || field.disabled) {
            return { valid: true };
        }

        if (!raw) {
            if (isRequired(field)) {
                return { valid: false, type: "error", message: `${label} is required.` };
            }

            return { valid: true };
        }

        if ((name === "FirstName" || name === "LastName" || name === "ContactName") && raw.length < 3) {
            return { valid: false, type: "error", message: `${label} must be at least 3 characters.` };
        }

        if ((name === "FirstName" || name === "LastName" || name === "ContactName") && !namePattern.test(raw)) {
            return { valid: false, type: "error", message: `${label} cannot contain numbers or special characters.` };
        }

        if (name === "Username") {
            if (raw.length < 3) {
                return { valid: false, type: "error", message: "Username must be at least 3 characters." };
            }
            if (!usernamePattern.test(raw)) {
                return { valid: false, type: "error", message: "Username can use letters, numbers, dots, underscores, and hyphens only." };
            }
        }

        if ((field.type === "email" || name === "Email" || name === "ContactEmail") && !emailPattern.test(raw)) {
            return { valid: false, type: "error", message: "Enter a valid email address." };
        }

        if (name === "ContactMessage" && raw.length < 10) {
            return { valid: false, type: "error", message: "Message must be at least 10 characters." };
        }

        if ((name === "Phone" || name === "ContactNumber") && !pakistanPhonePattern.test(value)) {
            return { valid: false, type: "error", message: "Use a Pakistan number starting with +92 or 0, for example 03001234567." };
        }

        if (name === "Website" && raw && !websitePattern.test(raw)) {
            return { valid: false, type: "error", message: "Enter a valid website URL starting with http:// or https://." };
        }

        if (name === "DateOfBirth") {
            if (raw > adultCutoffString()) {
                return { valid: false, type: "error", message: "Employee must be at least 18 years old." };
            }
            if (raw > todayString()) {
                return { valid: false, type: "error", message: "Date of birth cannot be in the future." };
            }
        }

        if ((name === "FromDate" || name === "ToDate") && raw > todayString()) {
            return { valid: false, type: "error", message: `${label} cannot be in the future.` };
        }

        if (name === "ToDate") {
            const fromDate = form.querySelector('[name="FromDate"]');
            if (fromDate?.value && raw < fromDate.value) {
                return { valid: false, type: "error", message: "To date cannot be earlier than from date." };
            }
        }

        if (name === "StartDate" && raw < todayString()) {
            return { valid: false, type: "error", message: "Start date cannot be in the past." };
        }

        if (name === "EndDate") {
            const startDate = form.querySelector('[name="StartDate"]');
            if (startDate?.value && raw <= startDate.value) {
                return { valid: false, type: "error", message: "End date must be after the start date." };
            }
        }

        if ((field.type === "number" || ["PremiumAmount", "CoverageAmount", "BillAmount"].includes(name)) && Number(raw) <= 0) {
            return { valid: false, type: "error", message: `${label} must be greater than zero.` };
        }

        if (name === "DurationMonths") {
            const months = Number(raw);
            if (months < 1 || months > 60) {
                return { valid: false, type: "error", message: "Duration must be between 1 and 60 months." };
            }
        }

        if (isPasswordStrengthField(field)) {
            if (raw.length < 8) {
                return { valid: false, type: "warning", message: "Use at least 8 characters." };
            }
            if (!/[A-Za-z]/.test(raw) || !/\d/.test(raw) || !/[^A-Za-z\d]/.test(raw)) {
                return { valid: false, type: "warning", message: "Add a letter, a number, and a symbol for a strong password." };
            }
        }

        const matchField = matchingPasswordField(form, field);
        if (matchField && raw !== matchField.value) {
            return { valid: false, type: "error", message: "Passwords do not match." };
        }

        return { valid: true, type: "success", message: "All good." };
    }

    function prepareField(field) {
        if (!field.id && field.name) {
            field.id = `app-field-${field.name}-${Math.random().toString(36).slice(2)}`;
        }

        if (field.name === "DateOfBirth") {
            field.max = adultCutoffString();
        }
        if (field.name === "FromDate" || field.name === "ToDate") {
            field.max = todayString();
        }
        if (field.name === "StartDate") {
            field.min = todayString();
        }
        if (field.name === "Phone" || field.name === "ContactNumber") {
            field.inputMode = "tel";
            field.placeholder = field.placeholder || "03001234567";
        }
        if (isPasswordStrengthField(field)) {
            field.autocomplete = "new-password";
        }
    }

    function attachValidation(form) {
        if (form.dataset.validationMode === "native") {
            return;
        }

        const fields = Array.from(form.querySelectorAll("input, select, textarea"))
            .filter((field) => field.type !== "hidden" && !field.disabled);

        if (!fields.length) {
            return;
        }

        fields.forEach((field) => {
            prepareField(field);

            const update = () => {
                const result = checkField(form, field);
                if (result.valid && !field.value.trim()) {
                    setFeedback(field, null, "");
                    return;
                }
                setFeedback(field, result.type, result.message);
            };

            field.addEventListener("blur", update);
            field.addEventListener("input", update);
            field.addEventListener("change", update);

            if (isPasswordStrengthField(field)) {
                field.addEventListener("focus", () => {
                    if (!field.value) {
                        setFeedback(field, "warning", "Use 8+ characters with a letter, a number, and a symbol.");
                    }
                });
            }

            if (document.body.dataset.page === "auth-forgotpassword" && field.name === "Email") {
                field.addEventListener("focus", () => {
                    if (!field.value) {
                        setFeedback(field, "warning", "Use the email registered with your account.");
                    }
                });
            }
        });

        form.addEventListener("submit", (event) => {
            const invalid = fields
                .map((field) => ({ field, result: checkField(form, field) }))
                .filter((item) => !item.result.valid);

            fields.forEach((field) => {
                const result = checkField(form, field);
                if (result.valid && !field.value.trim()) {
                    setFeedback(field, null, "");
                } else {
                    setFeedback(field, result.type, result.message);
                }
            });

            if (invalid.length) {
                event.preventDefault();
                invalid[0].field.focus();
            } else if (form.dataset.staticForm === "true") {
                event.preventDefault();
            }
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll("form").forEach(attachValidation);
    });
})();
