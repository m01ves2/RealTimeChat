<?php

session_start();

// Session values are missing when chat.php is opened directly.
$nickname = $_SESSION['nickname'] ?? null;
$roomId = $_SESSION['room_id'] ?? null;

if ($nickname === null || $roomId === null) {
    // Redirect visitors who have not joined a room back to the lobby.
    header('Location: /', true, 302);
    exit;
}

$pdo = require __DIR__ . '/../src/database.php';
$statement = $pdo->prepare('    SELECT title
                                FROM rooms
                                WHERE id = :room_id;');
$statement->execute(['room_id' => $roomId]);
$roomTitle = $statement->fetchColumn();

if ($roomTitle === false) {
    //room doesn't exist anymore => clean session and redirect to lobby

    // Remove all variables from the current session.
    session_unset();

    // Delete the session data stored on the server.
    session_destroy();
    header('Location: /', true, 302);
    exit;
}


$recipient = $_GET['recipient'] ?? null;
if ($recipient !== null) {
    if (!is_string($recipient)) {
        header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
        echo 'Invalid recipient.';
        exit;
    }

    $recipient = trim($recipient);

    if ($recipient === '') {
        $recipient = null;
    }
}

?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Classic PHP Chat</title>
</head>
<body>
    <h1>Room: <?= htmlspecialchars($roomTitle, ENT_QUOTES, 'UTF-8') ?></h1>
    <div>Room ID: <?= (int)$roomId ?></div>
    <div>Signed in as: <?= htmlspecialchars($nickname, ENT_QUOTES, 'UTF-8') ?></div>
    <iframe src="messages.php"
            title="New messages"
            width="800"
            height="600">
    </iframe>

    <form method="post" action="send.php">
        <?php if ($recipient !== null): ?>
            <div>
                To: <strong> <?= htmlspecialchars($recipient, ENT_QUOTES, 'UTF-8') ?> </strong>
                <a href="/chat.php">Clear</a>
            </div>
        <?php endif; ?>

        <input type="hidden" name="recipient" value="<?= htmlspecialchars($recipient ?? '', ENT_QUOTES, 'UTF-8') ?>"
>
        <div>
            <label for="message">Message: </label>
            <textarea rows="5" cols="80" name="message" id="message" required></textarea>
        </div>
        <input type="submit" value="Send">
    </form>

    <form method="post" action="exit.php">
        <button type="submit">Exit</button>
    </form>
</body>
</html>