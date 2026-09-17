let lastMessageId = null;
const messagesContainer = document.querySelector('#messages');

async function loadMessages() {

    let url = '/messages.php';

      // The first request has no "after" parameter and returns immediately.
    // Subsequent requests wait for messages newer than lastMessageId.
    if (lastMessageId !== null) {
        url += `?after=${lastMessageId}`;
    }

    const response = await fetch(url);

    if (!response.ok) {
        throw new Error(`HTTP error: ${response.status}`);
    }

    const html = await response.text();




    // restore message scrolling after call loadMessages() and reloading messages panel
    const previousMessagesPanel = messagesContainer.querySelector('.messages-panel');
    let previousScrollTop = null;
    let wasAtBottom = true;

    // Save the current scrolling position.
    if (previousMessagesPanel !== null) {
        previousScrollTop = previousMessagesPanel.scrollTop;

        const distanceFromBottom = 
        previousMessagesPanel.scrollHeight // full size of content inside panel, including invisible part
        - previousMessagesPanel.scrollTop  // distance from top of panel
        - previousMessagesPanel.clientHeight; // height of visible part. e.g.: 2000 - 1200 - 300 = 500px - distance from bottom visible part to the end of all content

    wasAtBottom = distanceFromBottom < 20;
    }

    // Replace the messages and visitors panels with the new server response.
    messagesContainer.innerHTML = html;

    // Read the id of the newest message returned by the server.
    const messagesLayout = messagesContainer.querySelector('.messages-layout');

    if (messagesLayout !== null) {
        lastMessageId = Number(messagesLayout.dataset.lastMessageId);
    }

    // restore message scrolling from previous state
    const currentMessagesPanel = messagesContainer.querySelector('.messages-panel');
if (currentMessagesPanel !== null) {
    if (wasAtBottom) {
        currentMessagesPanel.scrollTop = currentMessagesPanel.scrollHeight;
    } else 
        if (previousScrollTop !== null) {
            currentMessagesPanel.scrollTop = previousScrollTop;
        }
}



    // Unlike short polling, there is no delay here.
    // The next request starts immediately after the previous one finishes.
    await loadMessages();
}

// Start the polling chain.
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

         // No manual loadMessages() call here.
        //
        // A long polling GET request is already waiting in messages.php.
        // The newly inserted message will cause that request to finish.
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