// TradeFlow Backup & Restore Center - Client Side Utilities
(function () {
    window.tfBackup = {
        uploadArchive: function (fileInputId, dotNetHelper) {
            var input = document.getElementById(fileInputId);
            if (!input || !input.files || input.files.length === 0) {
                dotNetHelper.invokeMethodAsync('OnValidationFailed', 'Không tìm thấy tệp để tải lên.');
                return;
            }

            var file = input.files[0];
            var formData = new FormData();
            formData.append('file', file, file.name);

            var xhr = new XMLHttpRequest();
            xhr.open('POST', '/api/admin/backup/upload-archive', true);

            xhr.upload.onprogress = function (e) {
                if (e.lengthComputable) {
                    var percent = Math.round((e.loaded / e.total) * 100);
                    dotNetHelper.invokeMethodAsync('OnUploadProgress', percent);
                }
            };

            xhr.onload = function () {
                if (xhr.status >= 200 && xhr.status < 300) {
                    try {
                        dotNetHelper.invokeMethodAsync('OnValidationSuccess', xhr.responseText);
                    } catch (ex) {
                        dotNetHelper.invokeMethodAsync('OnValidationFailed', 'Lỗi xử lý phản hồi máy chủ: ' + ex.message);
                    }
                } else {
                    var errMsg = 'Tải tệp thất bại (' + xhr.status + ').';
                    try {
                        var errObj = JSON.parse(xhr.responseText);
                        if (errObj && errObj.message) errMsg = errObj.message;
                    } catch (e) { }
                    dotNetHelper.invokeMethodAsync('OnValidationFailed', errMsg);
                }
            };

            xhr.onerror = function () {
                dotNetHelper.invokeMethodAsync('OnValidationFailed', 'Lỗi kết nối mạng trong quá trình tải tệp lên máy chủ.');
            };

            xhr.send(formData);
        },

        uploadFolder: function (folderInputId, dotNetHelper) {
            var input = document.getElementById(folderInputId);
            if (!input || !input.files || input.files.length === 0) {
                dotNetHelper.invokeMethodAsync('OnValidationFailed', 'Không có tệp nào trong thư mục đã chọn.');
                return;
            }

            var formData = new FormData();
            for (var i = 0; i < input.files.length; i++) {
                var f = input.files[i];
                var relPath = f.webkitRelativePath || f.name;
                formData.append('files', f, f.name);
                formData.append(f.name, relPath);
            }

            var xhr = new XMLHttpRequest();
            xhr.open('POST', '/api/admin/backup/upload-folder', true);

            xhr.upload.onprogress = function (e) {
                if (e.lengthComputable) {
                    var percent = Math.round((e.loaded / e.total) * 100);
                    dotNetHelper.invokeMethodAsync('OnUploadProgress', percent);
                }
            };

            xhr.onload = function () {
                if (xhr.status >= 200 && xhr.status < 300) {
                    try {
                        dotNetHelper.invokeMethodAsync('OnValidationSuccess', xhr.responseText);
                    } catch (ex) {
                        dotNetHelper.invokeMethodAsync('OnValidationFailed', 'Lỗi xử lý phản hồi máy chủ: ' + ex.message);
                    }
                } else {
                    var errMsg = 'Tải thư mục thất bại (' + xhr.status + ').';
                    try {
                        var errObj = JSON.parse(xhr.responseText);
                        if (errObj && errObj.message) errMsg = errObj.message;
                    } catch (e) { }
                    dotNetHelper.invokeMethodAsync('OnValidationFailed', errMsg);
                }
            };

            xhr.onerror = function () {
                dotNetHelper.invokeMethodAsync('OnValidationFailed', 'Lỗi mạng khi tải thư mục lên máy chủ.');
            };

            xhr.send(formData);
        },

        executeRestore: async function (planJson, dotNetHelper) {
            try {
                var response = await fetch('/api/admin/backup/execute-restore', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: planJson
                });

                var text = await response.text();
                if (response.ok) {
                    dotNetHelper.invokeMethodAsync('OnRestoreSuccess', text);
                } else {
                    var errMsg = 'Khôi phục dữ liệu không thành công (' + response.status + ').';
                    try {
                        var errObj = JSON.parse(text);
                        if (errObj && errObj.message) errMsg = errObj.message;
                    } catch (e) { }
                    dotNetHelper.invokeMethodAsync('OnRestoreFailed', errMsg);
                }
            } catch (ex) {
                dotNetHelper.invokeMethodAsync('OnRestoreFailed', 'Lỗi mạng khi gửi yêu cầu khôi phục: ' + ex.message);
            }
        },

        cleanupStaging: function (stagingPath) {
            if (!stagingPath) return;
            try {
                fetch('/api/admin/backup/cleanup-staging', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ stagingPath: stagingPath }),
                    keepalive: true
                });
            } catch (e) { }
        },

        triggerDownload: function (url) {
            var a = document.createElement('a');
            a.href = url;
            a.download = '';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        }
    };
})();
