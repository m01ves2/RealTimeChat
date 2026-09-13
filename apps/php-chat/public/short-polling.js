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

// prevent page reloading for 'Submit' button
const messageForm = document.querySelector('.message-form');

messageForm.addEventListener('submit', async function (event) {
    event.preventDefault(); // do not open HTTP-response as new a new page

    const formData = new FormData(messageForm); // collect form data

    try {
        // Send POST request in background
        const response = await fetch(
            messageForm.action, {
            method: messageForm.method,
            headers: {
                // Tell send.php that this is a background form submission.
                // It should return 204 No Content instead of redirecting to chat.php.
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: formData
        });

        if (!response.ok) {
            const errorMessage = await response.text();
            alert(errorMessage || `HTTP error: ${response.status}`);
            return;
        }

        messageForm.querySelector('#message').value = '';
    } catch (error) {
        console.error(error);
        alert('The message could not be sent.');
    }
});