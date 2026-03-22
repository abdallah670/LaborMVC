/**
 * Theme Switcher Logic
 * Handles switching between Light and Dark modes and persisting the state.
 */
document.addEventListener("DOMContentLoaded", function () {
    const themeToggle = document.getElementById("theme-toggle");
    const themeIcon = document.getElementById("theme-icon");
    const currentTheme = localStorage.getItem("theme") || "light";

    // Set initial theme
    if (currentTheme === "dark") {
        document.documentElement.setAttribute("data-theme", "dark");
        if (themeIcon) {
            themeIcon.classList.replace("bi-moon-stars-fill", "bi-sun-fill");
        }
    }

    if (themeToggle) {
        themeToggle.addEventListener("click", function () {
            let theme = document.documentElement.getAttribute("data-theme");
            
            if (theme === "dark") {
                document.documentElement.setAttribute("data-theme", "light");
                localStorage.setItem("theme", "light");
                if (themeIcon) {
                    themeIcon.classList.replace("bi-sun-fill", "bi-moon-stars-fill");
                }
            } else {
                document.documentElement.setAttribute("data-theme", "dark");
                localStorage.setItem("theme", "dark");
                if (themeIcon) {
                    themeIcon.classList.replace("bi-moon-stars-fill", "bi-sun-fill");
                }
            }
        });
    }
});
