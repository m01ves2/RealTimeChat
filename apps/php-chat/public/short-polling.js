const pollingIntervalMs = 5000;
let pollingTimerId = null;
const messagesContainer = document.querySelector('#messages');

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

    
    // restore message scrolling after call loadMessages() and reloading messages panel
    const previousMessagesPanel = messagesContainer.querySelector('.messages-panel');
    let previousScrollTop = null;

     // save previous scrolling
    if (previousMessagesPanel !== null) {
        previousScrollTop = previousMessagesPanel.scrollTop;
    }

    // fill message panel by new messages 
    messagesContainer.innerHTML = html;

    // restore message scrolling from previous state
    const currentMessagesPanel = messagesContainer.querySelector('.messages-panel');
    if (currentMessagesPanel !== null && previousScrollTop !== null) {
        currentMessagesPanel.scrollTop = previousScrollTop;
    }

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


//choose recipient without page reloading
const recipientInput = document.querySelector('#recipient');
const recipientName = document.querySelector('#recipient-name');
const clearRecipientLink = document.querySelector('#clear-recipient');

messagesContainer.addEventListener('click', function (event) {
    const recipientLink = event.target.closest('[data-recipient]');

    if (recipientLink === null) {
        return;
    }

    event.preventDefault();

    const recipient = recipientLink.dataset.recipient;

    recipientInput.value = recipient;
    recipientName.textContent = recipient;
    clearRecipientLink.hidden = false;
});

clearRecipientLink.addEventListener('click', function (event) {
    event.preventDefault();

    recipientInput.value = '';
    recipientName.textContent = 'Everyone';
    clearRecipientLink.hidden = true;
});