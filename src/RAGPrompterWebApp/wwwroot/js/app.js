window.dropdownClickHandler = {
    initialize: function (dropdownElement) {
        document.addEventListener('click', function (e) {
            if (!dropdownElement.contains(e.target)) {
                DotNet.invokeMethodAsync('RAGPrompterWebApp', 'CloseDropdown');
            }
        });
    }
};

window.clipboardCopy = {
    copyText: function (text) {
        navigator.clipboard.writeText(text).then(function () {
            console.log("Copied to clipboard");
        })
            .catch(function (error) {
                console.error("Failed to copy: ", error);
            });
    }
};

window.addEventListener = (event, dotnetHelper) => {
    document.addEventListener(event, (e) => {
        dotnetHelper.invokeMethodAsync('HandleGlobalKeyPress', e.key);
    });
};

window.fileManager = {
    async getFiles(input) {
        const files = Array.from(input.files);
        return files.map(f => ({
            name: f.name,
            path: f.webkitRelativePath || f.name
        }));
    }
};