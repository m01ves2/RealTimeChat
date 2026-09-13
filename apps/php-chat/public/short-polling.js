async function loadMessages() {
    const response = await fetch('/messages.php');

    if (!response.ok) {
        throw new Error(`HTTP error: ${response.status}`);
    }

    const html = await response.text();
    const messagesContainer = document.querySelector('#messages');

    messagesContainer.innerHTML = html;
}

loadMessages();