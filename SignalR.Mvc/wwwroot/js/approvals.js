(() => {
    const body = document.body;
    const token = body.dataset.accessToken;
    const hubUrl = body.dataset.hubUrl;
    const page = body.dataset.page;
    if (!token || !hubUrl || !page || typeof signalR === "undefined") {
        return;
    }

    const status = document.getElementById("live-status");
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
            accessTokenFactory: () => token,
            withCredentials: false
        })
        .withAutomaticReconnect()
        .build();

    connection.on("ApprovalRequested", (item) => {
        if (page === "inbox") {
            appendInboxRow(item);
        }
    });

    connection.on("ApprovalDecided", (item) => {
        if (page === "mine") {
            updateMineRow(item);
        }

        if (page === "inbox") {
            removeRow(item.id);
        }
    });

    connection.onreconnecting(() => setLive("Reconnecting", "text-warning"));
    connection.onreconnected(() => setLive("Live", "text-success"));
    connection.onclose(() => setLive("Offline", "text-danger"));

    connection.start()
        .then(() => setLive("Live", "text-success"))
        .catch(() => setLive("Offline", "text-danger"));

    function setLive(text, className) {
        if (!status) {
            return;
        }

        status.textContent = text;
        status.className = className;
    }

    function rowsBody() {
        return document.getElementById("request-rows");
    }

    function clearEmptyState() {
        document.getElementById("empty-state")?.remove();
    }

    function ensureEmptyState(message, columns) {
        const bodyElement = rowsBody();
        if (!bodyElement || bodyElement.querySelector("tr")) {
            return;
        }

        const row = document.createElement("tr");
        row.id = "empty-state";
        const cell = document.createElement("td");
        cell.colSpan = columns;
        cell.className = "text-muted";
        cell.textContent = message;
        row.appendChild(cell);
        bodyElement.appendChild(row);
    }

    function appendInboxRow(item) {
        const bodyElement = rowsBody();
        if (!bodyElement || bodyElement.querySelector(`[data-request-id="${item.id}"]`)) {
            return;
        }

        clearEmptyState();
        const row = document.createElement("tr");
        row.dataset.requestId = item.id;
        row.append(
            cell(item.title),
            cell(item.requesterName),
            cell(item.details),
            cell(formatDate(item.createdAt)),
            decisionCell(item.id)
        );
        bodyElement.prepend(row);
    }

    function updateMineRow(item) {
        const row = document.querySelector(`[data-request-id="${item.id}"]`);
        if (!row) {
            return;
        }

        const badge = row.querySelector(".status");
        if (badge) {
            badge.textContent = item.status;
            badge.className = `badge status ${badgeClass(item.status)}`;
        }

        const note = row.querySelector(".note");
        if (note) {
            note.textContent = item.decisionNote || "—";
        }
    }

    function removeRow(id) {
        document.querySelector(`[data-request-id="${id}"]`)?.remove();
        ensureEmptyState("No pending requests.", 5);
    }

    function cell(value) {
        const element = document.createElement("td");
        element.textContent = value ?? "";
        return element;
    }

    function decisionCell(id) {
        const element = document.createElement("td");
        const form = document.createElement("form");
        form.method = "post";
        form.action = body.dataset.decideUrl || "";
        form.className = "d-flex gap-2";
        form.append(
            hidden("Id", id),
            hidden("__RequestVerificationToken", csrfToken()),
            noteInput(),
            submitButton("Approve", "true", "btn btn-sm btn-success"),
            submitButton("Reject", "false", "btn btn-sm btn-outline-danger")
        );
        element.appendChild(form);
        return element;
    }

    function hidden(name, value) {
        const input = document.createElement("input");
        input.type = "hidden";
        input.name = name;
        input.value = value;
        return input;
    }

    function noteInput() {
        const input = document.createElement("input");
        input.className = "form-control form-control-sm";
        input.name = "Note";
        input.maxLength = 1000;
        input.placeholder = "Note";
        return input;
    }

    function submitButton(label, approved, className) {
        const button = document.createElement("button");
        button.type = "submit";
        button.className = className;
        button.name = "Approved";
        button.value = approved;
        button.textContent = label;
        return button;
    }

    function csrfToken() {
        return document.querySelector('meta[name="csrf-token"]')?.content || "";
    }

    function formatDate(value) {
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? "" : date.toLocaleString();
    }

    function badgeClass(status) {
        if (status === "Approved") {
            return "text-bg-success";
        }

        if (status === "Rejected") {
            return "text-bg-danger";
        }

        return "text-bg-warning";
    }
})();
