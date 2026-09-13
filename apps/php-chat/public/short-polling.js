const pollingIntervalMs = 5000;

async function loadMessages() {
    const response = await fetch('/messages.php');

    if (!response.ok) {
        throw new Error(`HTTP error: ${response.status}`);
    }

    const html = await response.text();
    const messagesContainer = document.querySelector('#messages');

    messagesContainer.innerHTML = html;

    // We use setTimeout(), not setInterval() to avoid request (messages.php) interseption.
     setTimeout(loadMessages, pollingIntervalMs);
}

loadMessages();