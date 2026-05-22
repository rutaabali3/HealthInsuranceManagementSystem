(function () {
    "use strict";

    const links = Array.from(document.querySelectorAll("[data-conversation-url]"));
    const avatar = document.querySelector("[data-chat-avatar]");
    const name = document.querySelector("[data-chat-name]");
    const email = document.querySelector("[data-chat-email]");
    const status = document.querySelector("[data-chat-status]");
    const messages = document.querySelector("[data-chat-messages]");
    const replyForm = document.querySelector("[data-reply-form]");

    if (!links.length || !avatar || !name || !email || !status || !messages || !replyForm) {
        return;
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function setActiveLink(activeLink) {
        links.forEach((link) => link.classList.toggle("active", link === activeLink));
    }

    function renderConversation(data, activeLink) {
        const initial = data.name && data.name.trim() ? data.name.trim().charAt(0).toUpperCase() : "?";

        avatar.textContent = initial;
        name.textContent = data.name || "Visitor";
        email.textContent = data.email || "";
        status.textContent = data.status || "New";
        status.className = `status-pill ${data.status === "New" ? "status-waiting" : "status-approved"}`;
        replyForm.action = data.replyUrl || activeLink.dataset.replyUrl || replyForm.action;

        messages.innerHTML = data.messages.map((message) => {
            const isStaff = message.senderType === "Staff";
            const direction = isStaff ? "support-message-out" : "support-message-in";

            return `
                <article class="support-message ${direction}">
                    <div>
                        <p>${escapeHtml(message.message)}</p>
                        <small>${escapeHtml(message.sentAt)}</small>
                    </div>
                </article>
            `;
        }).join("");

        messages.scrollTop = messages.scrollHeight;
        setActiveLink(activeLink);
        window.history.replaceState(null, "", activeLink.href);
    }

    links.forEach((link) => {
        link.addEventListener("click", async (event) => {
            event.preventDefault();

            try {
                const response = await fetch(link.dataset.conversationUrl, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });

                if (!response.ok) {
                    window.location.href = link.href;
                    return;
                }

                renderConversation(await response.json(), link);
            } catch {
                window.location.href = link.href;
            }
        });
    });
})();
