(function () {
    "use strict";

    const SESSION_HISTORY_KEY = "krypton_session_history_v1";

    const ICON_SUN = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" aria-hidden="true"><circle cx="12" cy="12" r="5"/><path d="M12 1v2m0 18v2M4.22 4.22l1.42 1.42m12.72 12.72 1.42 1.42M1 12h2m18 0h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>`;
    const ICON_MOON = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>`;
    const ICON_EYE_OPEN = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>`;
    const ICON_EYE_OFF = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>`;

    function formatBytes(bytes) {
        if (!bytes || bytes === 0) return "0 B";
        const k = 1024;
        const sizes = ["B", "KB", "MB", "GB"];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
    }

    function escapeHtml(str) {
        if (!str) return "";
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // =========================================================================
    // Histórico Efêmero da Sessão (sessionStorage)
    // =========================================================================
    function addSessionHistoryEntry(operation, fileName) {
        try {
            const raw = sessionStorage.getItem(SESSION_HISTORY_KEY);
            const list = raw ? JSON.parse(raw) : [];
            list.unshift({
                timestamp: new Date().toLocaleTimeString("pt-BR"),
                operation,
                fileName,
                algorithm: "AES-256-GCM / PBKDF2-SHA512"
            });
            sessionStorage.setItem(SESSION_HISTORY_KEY, JSON.stringify(list.slice(0, 25)));
        } catch (_) { }
    }

    function renderSessionHistoryTable() {
        const tbody = document.getElementById("sessionHistoryBody");
        const thead = document.getElementById("sessionHistoryThead");
        const countBadge = document.getElementById("sessionHistoryCountBadge");
        const clearBtn = document.getElementById("clearSessionHistoryBtn");
        if (!tbody) return;

        let list = [];
        try {
            const raw = sessionStorage.getItem(SESSION_HISTORY_KEY);
            list = raw ? JSON.parse(raw) : [];
        } catch (_) { }

        if (countBadge) {
            countBadge.textContent = list.length === 1 ? "1 registro" : `${list.length} registros`;
        }

        if (clearBtn) {
            if (list.length > 0) {
                clearBtn.classList.remove("d-none");
            } else {
                clearBtn.classList.add("d-none");
            }
        }

        if (!list.length) {
            if (thead) thead.classList.add("d-none");
            tbody.innerHTML = `
                <tr>
                    <td colspan="6" class="border-0 p-0">
                        <div class="activity-empty-box">
                            <div class="activity-empty-emblem" aria-hidden="true">
                                <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
                                    <rect x="4" y="11" width="16" height="10" rx="2" />
                                    <path d="M8 11V7a4 4 0 0 1 8 0v4" />
                                </svg>
                            </div>
                            <div class="fw-semibold mb-1">Nenhuma operação registrada nesta aba</div>
                            <p class="small text-body-secondary mb-3 mx-auto">
                                Os arquivos selados ou restaurados durante a sessão aparecerão listados aqui.
                            </p>
                            <div class="d-inline-flex flex-wrap justify-content-center gap-2">
                                <a href="/Encryption" class="btn btn-sm btn-outline-secondary touch-target-btn px-3">Criptografar arquivos</a>
                                <a href="/Decryption" class="btn btn-sm btn-outline-secondary touch-target-btn px-3">Descriptografar arquivo</a>
                            </div>
                        </div>
                    </td>
                </tr>`;
            return;
        }

        if (thead) thead.classList.remove("d-none");

        const lockSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.2" aria-hidden="true"><rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/></svg>`;
        const unlockSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.2" aria-hidden="true"><rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 7.4-2"/></svg>`;
        const fileSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" class="text-body-secondary flex-shrink-0" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>`;

        tbody.innerHTML = list.map((item, idx) => {
            const isEnc = item.operation === "Criptografia";
            const pillClass = isEnc ? "activity-op-encrypt" : "activity-op-decrypt";
            const opIcon = isEnc ? lockSvg : unlockSvg;
            const opLabel = isEnc ? "Selagem .krypt" : "Restauração";

            return `
                <tr>
                    <td class="ps-4">
                        <span class="activity-op-pill ${pillClass}">
                            ${opIcon}
                            <span>${opLabel}</span>
                        </span>
                    </td>
                    <td>
                        <div class="activity-file-chip" title="${escapeHtml(item.fileName)}">
                            ${fileSvg}
                            <span class="font-monospace fw-semibold text-truncate">${escapeHtml(item.fileName)}</span>
                        </div>
                    </td>
                    <td>
                        <div class="d-flex flex-wrap gap-1">
                            <span class="activity-engine-tag font-monospace">AES-256-GCM</span>
                            <span class="activity-engine-tag font-monospace">PBKDF2 600k</span>
                        </div>
                    </td>
                    <td>
                        <span class="activity-status-pill">
                            <span class="led-indicator" aria-hidden="true"></span>
                            <span>Concluído</span>
                        </span>
                    </td>
                    <td class="font-monospace text-body-secondary text-nowrap tabular-nums">
                        ${escapeHtml(item.timestamp)}
                    </td>
                    <td class="pe-4 text-end">
                        <button type="button" class="btn-remove-activity" data-remove-idx="${idx}" title="Remover registro" aria-label="Remover registro de ${escapeHtml(item.fileName)}">
                            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" aria-hidden="true">
                                <line x1="18" y1="6" x2="6" y2="18" />
                                <line x1="6" y1="6" x2="18" y2="18" />
                            </svg>
                        </button>
                    </td>
                </tr>
            `;
        }).join("");

        tbody.querySelectorAll(".btn-remove-activity").forEach(btn => {
            btn.addEventListener("click", function () {
                const removeIdx = parseInt(btn.getAttribute("data-remove-idx"), 10);
                list.splice(removeIdx, 1);
                sessionStorage.setItem(SESSION_HISTORY_KEY, JSON.stringify(list));
                renderSessionHistoryTable();
            });
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initThemeToggle();
        initPasswordVisibilityToggles();
        renderSessionHistoryTable();

        const clearHistoryBtn = document.getElementById("clearSessionHistoryBtn");
        if (clearHistoryBtn) {
            clearHistoryBtn.addEventListener("click", function () {
                sessionStorage.removeItem(SESSION_HISTORY_KEY);
                renderSessionHistoryTable();
            });
        }

        initEncryptionPage();
        initDecryptionPage();
    });

    function initThemeToggle() {
        const btn = document.getElementById("themeToggleBtn");
        const icon = document.getElementById("themeToggleIcon");
        const label = document.getElementById("themeToggleLabel");
        if (!btn) return;

        function updateUi(theme) {
            if (icon) icon.innerHTML = theme === "dark" ? ICON_MOON : ICON_SUN;
            if (label) label.textContent = theme === "dark" ? "Escuro" : "Claro";
        }

        const current = document.documentElement.getAttribute("data-bs-theme") || "dark";
        updateUi(current);

        btn.addEventListener("click", function () {
            const active = document.documentElement.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
            document.documentElement.setAttribute("data-bs-theme", active);
            localStorage.setItem("krypton-theme", active);
            updateUi(active);
        });
    }

    function initPasswordVisibilityToggles() {
        document.querySelectorAll(".toggle-password-btn").forEach(btn => {
            btn.addEventListener("click", function () {
                const targetId = btn.getAttribute("data-target");
                const input = document.getElementById(targetId);
                if (!input) return;
                input.type = input.type === "password" ? "text" : "password";
                btn.innerHTML = input.type === "password" ? ICON_EYE_OPEN : ICON_EYE_OFF;
            });
        });
    }

    function wireKeyboardDropzone(dropzoneEl, fileInputEl) {
        if (!dropzoneEl || !fileInputEl) return;
        dropzoneEl.addEventListener("keydown", function (e) {
            if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                fileInputEl.click();
            }
        });
    }

    // =========================================================================
    // Criptografia (/Encryption)
    // =========================================================================
    function initEncryptionPage() {
        const form = document.getElementById("encryptForm");
        if (!form) return;

        const dropzone = document.getElementById("encryptDropzone");
        const fileInput = document.getElementById("encryptFileInput");
        const browseBtn = document.getElementById("browseEncryptFilesBtn");
        const filesContainer = document.getElementById("selectedFilesContainer");
        const filesList = document.getElementById("selectedFilesList");
        const filesCounter = document.getElementById("selectedFilesCounter");
        const clearFilesBtn = document.getElementById("clearSelectedFilesBtn");
        const batchPanel = document.getElementById("batchOptionsPanel");

        const pwdInput = document.getElementById("encryptPassword");
        const confirmInput = document.getElementById("confirmEncryptPassword");
        const matchFeedback = document.getElementById("passwordMatchFeedback");
        const strengthText = document.getElementById("passwordStrengthText");
        const strengthBar = document.getElementById("passwordStrengthBar");

        const alertContainer = document.getElementById("encryptAlertContainer");
        const progressCard = document.getElementById("encryptProgressCard");
        const submitBtn = document.getElementById("submitEncryptBtn");

        let selectedFiles = [];
        wireKeyboardDropzone(dropzone, fileInput);

        function syncInputFiles() {
            const dt = new DataTransfer();
            selectedFiles.forEach(f => dt.items.add(f));
            fileInput.files = dt.files;

            const totalBytes = selectedFiles.reduce((acc, f) => acc + f.size, 0);
            filesCounter.textContent = selectedFiles.length === 0
                ? "0 arquivos"
                : `${selectedFiles.length} arquivo(s) • ${formatBytes(totalBytes)}`;

            if (selectedFiles.length === 0) {
                filesContainer.classList.add("d-none");
                batchPanel.classList.add("d-none");
                filesList.innerHTML = "";
                return;
            }

            filesContainer.classList.remove("d-none");
            if (selectedFiles.length > 1) {
                batchPanel.classList.remove("d-none");
            } else {
                batchPanel.classList.add("d-none");
            }

            filesList.innerHTML = selectedFiles.map((f, idx) => `
                <li class="list-group-item bg-transparent d-flex justify-content-between align-items-center py-2 px-3 small">
                    <div class="text-truncate me-2 d-flex align-items-center gap-2">
                        <svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" class="text-body-secondary flex-shrink-0" aria-hidden="true">
                            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                            <polyline points="14 2 14 8 20 8"/>
                        </svg>
                        <strong class="text-truncate">${escapeHtml(f.name)}</strong>
                        <span class="text-body-secondary font-monospace tabular-nums flex-shrink-0">(${formatBytes(f.size)})</span>
                    </div>
                    <button type="button" class="btn btn-sm btn-outline-danger py-1 px-2 remove-file-btn" data-index="${idx}" aria-label="Remover ${escapeHtml(f.name)}">
                        Remover
                    </button>
                </li>
            `).join("");

            filesList.querySelectorAll(".remove-file-btn").forEach(btn => {
                btn.addEventListener("click", function () {
                    const idx = parseInt(btn.getAttribute("data-index"), 10);
                    selectedFiles.splice(idx, 1);
                    syncInputFiles();
                });
            });
        }

        function addFiles(fileList) {
            for (const f of fileList) {
                if (selectedFiles.length < 50) {
                    selectedFiles.push(f);
                }
            }
            syncInputFiles();
        }

        dropzone.addEventListener("click", function (e) {
            if (e.target === browseBtn) return;
            fileInput.click();
        });

        browseBtn.addEventListener("click", function (e) {
            e.stopPropagation();
            fileInput.click();
        });

        fileInput.addEventListener("change", function () {
            if (fileInput.files && fileInput.files.length > 0) {
                selectedFiles = Array.from(fileInput.files);
                syncInputFiles();
            }
        });

        ["dragenter", "dragover"].forEach(evt => {
            dropzone.addEventListener(evt, function (e) {
                e.preventDefault();
                e.stopPropagation();
                dropzone.classList.add("drag-over");
            });
        });

        ["dragleave", "drop"].forEach(evt => {
            dropzone.addEventListener(evt, function (e) {
                e.preventDefault();
                e.stopPropagation();
                dropzone.classList.remove("drag-over");
            });
        });

        dropzone.addEventListener("drop", function (e) {
            if (e.dataTransfer && e.dataTransfer.files.length > 0) {
                addFiles(e.dataTransfer.files);
            }
        });

        clearFilesBtn.addEventListener("click", function () {
            selectedFiles = [];
            syncInputFiles();
        });

        function updatePasswordIndicators() {
            const val = pwdInput.value || "";
            if (strengthBar && strengthText) {
                if (!val.length) {
                    strengthBar.style.width = "0%";
                    strengthBar.className = "progress-bar bg-secondary";
                    strengthText.textContent = "Mínimo de 8 caracteres";
                    strengthText.className = "text-body-secondary tabular-nums";
                } else if (val.length < 8) {
                    strengthBar.style.width = "30%";
                    strengthBar.className = "progress-bar bg-danger";
                    strengthText.textContent = `Faltam ${8 - val.length} caractere(s)`;
                    strengthText.className = "text-danger fw-semibold tabular-nums";
                } else if (val.length < 14) {
                    strengthBar.style.width = "68%";
                    strengthBar.className = "progress-bar bg-warning";
                    strengthText.textContent = `${val.length} caracteres (válida)`;
                    strengthText.className = "text-warning fw-semibold tabular-nums";
                } else {
                    strengthBar.style.width = "100%";
                    strengthBar.className = "progress-bar bg-success";
                    strengthText.textContent = `${val.length} caracteres (forte)`;
                    strengthText.className = "text-success fw-semibold tabular-nums";
                }
            }

            if (!confirmInput.value && !val) {
                matchFeedback.textContent = "";
                return;
            }
            if (confirmInput.value && val === confirmInput.value) {
                matchFeedback.textContent = "As senhas coincidem.";
                matchFeedback.className = "form-text small text-success fw-semibold";
            } else if (confirmInput.value) {
                matchFeedback.textContent = "A confirmação está diferente da senha principal.";
                matchFeedback.className = "form-text small text-danger fw-semibold";
            } else {
                matchFeedback.textContent = "";
            }
        }

        pwdInput.addEventListener("input", updatePasswordIndicators);
        confirmInput.addEventListener("input", updatePasswordIndicators);

        form.addEventListener("submit", async function (e) {
            e.preventDefault();
            alertContainer.innerHTML = "";

            if (selectedFiles.length === 0) {
                showAlert(alertContainer, "danger", "Selecione pelo menos um arquivo na área de entrada antes de criptografar.");
                dropzone.focus();
                return;
            }

            if (!pwdInput.value || pwdInput.value.length < 8) {
                showAlert(alertContainer, "danger", "Digite uma senha com pelo menos 8 caracteres.");
                pwdInput.focus();
                return;
            }

            if (pwdInput.value !== confirmInput.value) {
                showAlert(alertContainer, "danger", "A confirmação de senha deve ser idêntica à senha principal.");
                confirmInput.focus();
                return;
            }

            const formData = new FormData(form);
            formData.delete("Files");
            selectedFiles.forEach(f => formData.append("Files", f));

            submitBtn.disabled = true;
            progressCard.classList.remove("d-none");

            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: formData
                });

                if (!response.ok) {
                    let errMsg = "Não foi possível criptografar o arquivo.";
                    try {
                        const errJson = await response.json();
                        if (errJson && errJson.error) errMsg = errJson.error;
                    } catch (_) { }
                    showAlert(alertContainer, "danger", errMsg);
                    return;
                }

                const blob = await response.blob();
                const encodedFileName = response.headers.get("X-Krypton-OutputFileName");
                const downloadName = encodedFileName
                    ? decodeURIComponent(encodedFileName)
                    : `${selectedFiles[0].name}.krypt`;

                triggerBlobDownload(blob, downloadName);
                addSessionHistoryEntry("Criptografia", downloadName);

                showAlert(
                    alertContainer,
                    "success",
                    `<strong>Arquivo criptografado e baixado.</strong><br/>` +
                    `Arquivo gerado: <code>${escapeHtml(downloadName)}</code>`
                );
            } catch (err) {
                showAlert(alertContainer, "danger", "Falha de comunicação com o servidor: " + err.message);
            } finally {
                submitBtn.disabled = false;
                progressCard.classList.add("d-none");
            }
        });
    }

    // =========================================================================
    // Descriptografia (/Decryption)
    // =========================================================================
    function initDecryptionPage() {
        const form = document.getElementById("decryptForm");
        if (!form) return;

        const dropzone = document.getElementById("decryptDropzone");
        const fileInput = document.getElementById("decryptFileInput");
        const browseBtn = document.getElementById("browseDecryptFileBtn");
        const titleEl = document.getElementById("decryptDropzoneTitle");
        const subtitleEl = document.getElementById("decryptDropzoneSubtitle");
        const pwdInput = document.getElementById("decryptPassword");

        const alertContainer = document.getElementById("decryptAlertContainer");
        const progressCard = document.getElementById("decryptProgressCard");
        const submitBtn = document.getElementById("submitDecryptBtn");

        wireKeyboardDropzone(dropzone, fileInput);

        function updateSelectedFileDisplay(file) {
            if (!file) return;
            titleEl.innerHTML = `<strong>${escapeHtml(file.name)}</strong>`;
            subtitleEl.textContent = `Tamanho: ${formatBytes(file.size)} — pronto para validação e abertura`;
        }

        dropzone.addEventListener("click", function (e) {
            if (e.target === browseBtn) return;
            fileInput.click();
        });

        browseBtn.addEventListener("click", function (e) {
            e.stopPropagation();
            fileInput.click();
        });

        fileInput.addEventListener("change", function () {
            if (fileInput.files && fileInput.files.length > 0) {
                updateSelectedFileDisplay(fileInput.files[0]);
            }
        });

        ["dragenter", "dragover"].forEach(evt => {
            dropzone.addEventListener(evt, function (e) {
                e.preventDefault();
                e.stopPropagation();
                dropzone.classList.add("drag-over");
            });
        });

        ["dragleave", "drop"].forEach(evt => {
            dropzone.addEventListener(evt, function (e) {
                e.preventDefault();
                e.stopPropagation();
                dropzone.classList.remove("drag-over");
            });
        });

        dropzone.addEventListener("drop", function (e) {
            if (e.dataTransfer && e.dataTransfer.files.length > 0) {
                const dt = new DataTransfer();
                dt.items.add(e.dataTransfer.files[0]);
                fileInput.files = dt.files;
                updateSelectedFileDisplay(fileInput.files[0]);
            }
        });

        form.addEventListener("submit", async function (e) {
            e.preventDefault();
            alertContainer.innerHTML = "";

            if (!fileInput.files || fileInput.files.length === 0) {
                showAlert(alertContainer, "danger", "Escolha um arquivo .krypt na área de entrada antes de descriptografar.");
                dropzone.focus();
                return;
            }

            if (!pwdInput.value) {
                showAlert(alertContainer, "danger", "Digite a senha utilizada quando o arquivo foi criptografado.");
                pwdInput.focus();
                return;
            }

            const formData = new FormData(form);
            submitBtn.disabled = true;
            progressCard.classList.remove("d-none");

            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: formData
                });

                if (!response.ok) {
                    let errMsg = "Não foi possível descriptografar o arquivo.";
                    try {
                        const errJson = await response.json();
                        if (errJson && errJson.error) errMsg = errJson.error;
                    } catch (_) { }
                    showAlert(alertContainer, "danger", errMsg);
                    return;
                }

                const blob = await response.blob();
                const encodedFileName = response.headers.get("X-Krypton-OutputFileName");
                const restoredName = encodedFileName
                    ? decodeURIComponent(encodedFileName)
                    : "arquivo-restaurado.bin";

                triggerBlobDownload(blob, restoredName);
                addSessionHistoryEntry("Descriptografia", restoredName);

                showAlert(
                    alertContainer,
                    "success",
                    `<strong>Autenticidade GCM confirmada e arquivo original restaurado.</strong><br/>` +
                    `Arquivo baixado: <code>${escapeHtml(restoredName)}</code>`
                );
            } catch (err) {
                showAlert(alertContainer, "danger", "Falha de comunicação ao descriptografar: " + err.message);
            } finally {
                submitBtn.disabled = false;
                progressCard.classList.add("d-none");
            }
        });
    }

    function showAlert(container, type, htmlMessage) {
        if (!container) return;
        container.innerHTML = `
            <div class="alert alert-${type} alert-dismissible fade show rounded-3 small mb-3" role="alert">
                ${htmlMessage}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Fechar alerta"></button>
            </div>`;
    }

    function triggerBlobDownload(blob, fileName) {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.style.display = "none";
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        setTimeout(() => {
            window.URL.revokeObjectURL(url);
            a.remove();
        }, 1000);
    }
})();
