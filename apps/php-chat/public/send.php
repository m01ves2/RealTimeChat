<?php

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    header('Allow: POST');
    exit;
}


session_start();
$author = $_SESSION['nickname'] ?? null;
$roomId = $_SESSION['room_id'] ?? null;
$visitorId = $_SESSION['visitor_id'] ?? null;
session_write_close(); // Release the session lock after reading its data.

if ($author === null || $roomId === null || $visitorId === null) {
    // Set the HTTP status line manually to demonstrate classic PHP response handling.
    header($_SERVER['SERVER_PROTOCOL'] . ' 403 Forbidden');
    header('Content-Type: text/plain; charset=UTF-8');
    echo "Join a room before sending messages.";
    exit;
}

$recipient = $_POST['recipient'] ?? null; // null when recipient was not selected

if ($recipient !== null && !is_string($recipient)) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    header('Content-Type: text/plain; charset=UTF-8');
    echo 'Recipient must be a string.';
    exit;
}

if ($recipient !== null) {
    $recipient = trim($recipient);

    if ($recipient === '') {
        $recipient = null;
    } elseif (mb_strlen($recipient, 'UTF-8') > 30) {
        header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
        header('Content-Type: text/plain; charset=UTF-8');
        echo 'Recipient must not exceed 30 characters.';
        exit;
    }
}

$messageText = $_POST['message'] ?? null;
if (!is_string($messageText)) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    header('Content-Type: text/plain; charset=UTF-8');
    echo 'Message must be a string.';
    exit;
}

$messageText = trim($messageText);
if ($messageText === '') {
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    header('Content-Type: text/plain; charset=UTF-8');
    echo 'Message is required.';
    exit;
}

// Count UTF-8 characters rather than bytes to match VARCHAR(500).
if (mb_strlen($messageText, 'UTF-8') > 500) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    header('Content-Type: text/plain; charset=UTF-8');
    echo 'Message must not exceed 500 characters.';
    exit;
}



$pdo = require __DIR__ . '/../src/database.php';

// Check whether the session still belongs to an active room visitor.
$activeStatement = $pdo->prepare("  SELECT 1
                                    FROM room_visitors
                                    WHERE id = :visitor_id
                                    AND room_id = :room_id
                                    AND nickname = :nickname
                                    AND last_seen >= CURRENT_TIMESTAMP - INTERVAL '30 seconds';");
$activeStatement->execute(['visitor_id' => $visitorId, 'room_id' => $roomId, 'nickname' => $author,]);
$activeVisitor = $activeStatement->fetchColumn();
if ($activeVisitor === false) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 403 Forbidden');
    header('Content-Type: text/plain; charset=UTF-8');
    echo 'Your room session has expired. Return to the lobby.';
    exit;
}


// Bind user input as parameters instead of interpolating it into SQL.
$statement = $pdo->prepare('    INSERT INTO messages(author, recipient, message_text, room_id) 
                                VALUES (:author, :recipient, :message_text, :room_id);');
$statement->execute(['author' => $author, 'message_text' => $messageText, 'room_id' => $roomId, 'recipient' => $recipient]);

// Without the redirect, the browser would remain on the response returned by send.php.
// Apply Post/Redirect/Get so refreshing the page does not submit the message again.
header('Location: /chat.php', true, 303);

exit; // header() does not stop script execution, so terminate it explicitly.