<?php
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    header('Allow: POST');
    exit;
}



$author = trim($_POST['author'] ?? '');
$messageText = trim($_POST['message'] ?? '');
$roomId = 1;

if ($author === '' || $messageText === '') {
    // Set the HTTP status line manually to demonstrate classic PHP response handling.
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    echo "Author and message are required.";

    //modern PHP8.x response handling
    // http_response_code(400);
    // echo 'Author and message are required.';

    exit;
}

$pdo = require __DIR__ . '/../src/database.php';
// Bind user input as parameters instead of interpolating it into SQL.
$statement = $pdo->prepare('INSERT INTO messages(author, message_text, room_id) VALUES (:author, :message_text, :room_id);');
$statement->execute(['author' => $author, 'message_text' => $messageText, 'room_id' => $roomId]);

// Without the redirect, the browser would remain on the response returned by send.php.
header('Location: /', true, 303); // Apply Post/Redirect/Get so refreshing the page does not submit the message again.

exit; // header() does not stop script execution, so terminate it explicitly.