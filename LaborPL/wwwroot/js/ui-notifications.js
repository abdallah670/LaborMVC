/**
 * Global UI Notifications & SweetAlert Interceptor
 * Handles TempData messages from the server and transforms them into SweetAlert toasts/modals.
 */
document.addEventListener("DOMContentLoaded", function () {
    const successMsg = document.getElementById("global-success-message");
    const errorMsg = document.getElementById("global-error-message");
    const warningMsg = document.getElementById("global-warning-message");
    const infoMsg = document.getElementById("global-info-message");

    if (successMsg && successMsg.getAttribute("data-message")) {
        Swal.fire({
            icon: 'success',
            title: 'Success',
            text: successMsg.getAttribute("data-message"),
            timer: 3000,
            showConfirmButton: false,
            toast: true,
            position: 'top-end'
        });
    }

    if (errorMsg && errorMsg.getAttribute("data-message")) {
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: errorMsg.getAttribute("data-message"),
            confirmButtonColor: 'var(--primary-color)'
        });
    }

    if (warningMsg && warningMsg.getAttribute("data-message")) {
        Swal.fire({
            icon: 'warning',
            title: 'Warning',
            text: warningMsg.getAttribute("data-message"),
            confirmButtonColor: 'var(--primary-color)'
        });
    }

    if (infoMsg && infoMsg.getAttribute("data-message")) {
        Swal.fire({
            icon: 'info',
            title: 'Information',
            text: infoMsg.getAttribute("data-message"),
            confirmButtonColor: 'var(--primary-color)'
        });
    }
});
