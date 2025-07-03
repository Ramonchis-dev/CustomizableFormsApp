// Global JavaScript functions
window.blazorHelpers = {
    focusElement: function (element) {
        if (element) {
            element.focus();
        }
    },

    scrollToBottom: function (element) {
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },

    setLocalStorage: function (key, value) {
        localStorage.setItem(key, value);
    },

    getLocalStorage: function (key) {
        return localStorage.getItem(key);
        return localStorage.getItem(key);
    }
};