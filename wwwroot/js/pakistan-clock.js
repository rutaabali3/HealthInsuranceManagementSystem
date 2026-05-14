(() => {
    const targets = document.querySelectorAll("[data-pk-time]");

    if (!targets.length) {
        return;
    }

    const formatter = new Intl.DateTimeFormat("en-PK", {
        timeZone: "Asia/Karachi",
        hour: "numeric",
        minute: "2-digit",
        second: "2-digit",
        hour12: true
    });

    const updateClock = () => {
        const time = formatter.format(new Date()).toUpperCase();
        targets.forEach((target) => {
            target.textContent = time;
        });
    };

    updateClock();
    window.setInterval(updateClock, 1000);
})();
