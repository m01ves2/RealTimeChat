const pollingIntervalMs = 5000;
let pollingTimerId = null;

async function loadMessages() {

    // abort ticking timer 
    if (pollingTimerId !== null) {

        // Cancel the pending poll when loadMessages() is called manually.
        // If the timer has already fired, clearTimeout() safely does nothing.
        clearTimeout(pollingTimerId);
        pollingTimerId = null;
    }

    const response = await fetch('/messages.php');

    if (!response.ok) {
        throw new Error(`HTTP error: ${response.status}`);
    }

    const html = await response.text();
    const messagesContainer = document.querySelector('#messages');

    messagesContainer.innerHTML = html;

    // We use setTimeout(), not setInterval() to avoid request (messages.php) interseption.
    pollingTimerId = setTimeout(loadMessages, pollingIntervalMs);
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

        //immediately loadMessages.  pollingTimerId != null here
        try {
            await loadMessages();
        } catch (error) {
            // if message was sent, but GET /messages.php was broken... 
            console.error('The message was sent, but messages could not be refreshed.', error);
        }
    } catch (error) {
        console.error(error);
        alert('The message could not be sent.');
    }
});