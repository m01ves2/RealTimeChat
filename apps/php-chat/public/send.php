<?php

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    header('Allow: POST');
    exit;
}


session_start();
$author = $_SESSION['nickname'] ?? null;
$roomId = $_SESSION['room_id'] ?? null;
session_write_close(); // free blocked session

if ($author === null || $roomId === null) {
    // Set the HTTP status line manually to demonstrate classic PHP response handling.
    header($_SERVER['SERVER_PROTOCOL'] . ' 403 Forbidden');
    echo "Join a room before sending messages.";
    exit;
}

$recipient = $_POST['recipient'] ?? null;  // 'null' if no key 'recipient' in $_POST
if ($recipient !== null && !is_string($recipient) ){
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    echo "Recipient must be a string.";
    exit;
}
if ($recipient !== null) {
    $recipient = trim($recipient);

    if ($recipient === '') {
        $recipient = null;
    }
}

$messageText = trim($_POST['message'] ?? '');
if ($messageText === '') {
    // Set the HTTP status line manually to demonstrate classic PHP response handling.
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    echo "Message is required.";

    exit;
}

$pdo = require __DIR__ . '/../src/database.php';
// Bind user input as parameters instead of interpolating it into SQL.
$statement = $pdo->prepare('    INSERT INTO messages(author, recipient, message_text, room_id) 
                                VALUES (:author, :recipient, :message_text, :room_id);');
$statement->execute(['author' => $author, 'message_text' => $messageText, 'room_id' => $roomId, 'recipient' => $recipient]);

// Without the redirect, the browser would remain on the response returned by send.php.
// Apply Post/Redirect/Get so refreshing the page does not submit the message again.
header('Location: /chat.php', true, 303);

exit; // header() does not stop script execution, so terminate it explicitly.