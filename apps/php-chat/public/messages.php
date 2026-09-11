<?php

session_start();
$nickname = $_SESSION['nickname'] ?? null;
$roomId = $_SESSION['room_id'] ?? null;
$visitorId = $_SESSION['visitor_id'] ?? null;
// Release the session lock because this request does not modify session data.
session_write_close();

if ($nickname === null || $roomId === null || $visitorId === null) {
    // Set the HTTP status line manually to demonstrate classic PHP response handling.
    header($_SERVER['SERVER_PROTOCOL'] . ' 403 Forbidden');
    header('Content-Type: text/plain; charset=UTF-8');
    echo "Join a room to view messages.";
    exit;
}

$pdo = require __DIR__ . '/../src/database.php';

// Refresh the visitor's online presence.
$heartbeatStatement = $pdo->prepare('   UPDATE room_visitors
                                        SET last_seen = CURRENT_TIMESTAMP
                                        WHERE id = :visitor_id
                                        AND room_id = :room_id
                                        RETURNING id;');
$heartbeatStatement->execute(['room_id' => $roomId, 'visitor_id' => $visitorId]);
$updatedVisitorId = $heartbeatStatement->fetchColumn();
if ($updatedVisitorId === false) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 403 Forbidden');
    header('Content-Type: text/plain; charset=UTF-8');
    echo "Your room session has expired. Return to the lobby.";
    exit;
}


// Get all visitors of the room.
$visitorsStatement = $pdo->prepare("    SELECT nickname
                                        FROM room_visitors
                                        WHERE room_id = :room_id
                                        AND last_seen >= CURRENT_TIMESTAMP - INTERVAL '30 seconds'
                                        ORDER BY LOWER(nickname), nickname;");
$visitorsStatement->execute(['room_id' => $roomId]);
$visitors = $visitorsStatement->fetchAll(PDO::FETCH_COLUMN);

// Get the latest 10 messages.
$statement = $pdo->prepare('    SELECT id, author, recipient, message_text, created_at
                                FROM messages
                                WHERE room_id = :room_id
                                ORDER BY created_at DESC, id DESC
                                LIMIT 10;');
$statement->execute(['room_id' => $roomId]);

$messages = $statement->fetchAll(PDO::FETCH_ASSOC);
// The query selects the newest 10 rows efficiently; reverse them for chronological display.
$messages = array_reverse($messages);
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="refresh" content="5">
    <title>Chat Messages</title>
</head>
<body>
    <div>
        <span>Refreshed at: </span>
        <?= date('H:i:s') ?>
    </div>
    <h2>Messages: </h2>

    <?php if (count($messages) > 0): ?>
        <?php foreach ($messages as $message): ?>
            <div>
                <span style="color: grey">
                    <!-- // Escape persisted values to prevent stored XSS - stored Javascript malcode, for example: <script>alert('Hacked')</script> -->
                    <?= htmlspecialchars($message['created_at'], ENT_QUOTES, 'UTF-8') ?>:
                </span>
                <span>
                    <?php if ($message['recipient'] !== null): ?>
                        <?= htmlspecialchars($message['author'], ENT_QUOTES, 'UTF-8') ?> → <?= htmlspecialchars($message['recipient'], ENT_QUOTES, 'UTF-8') ?>:
                    <?php else: ?>
                        <?= htmlspecialchars($message['author'], ENT_QUOTES, 'UTF-8') ?>:
                    <?php endif; ?>
                </span>
                <span>
                    <?= htmlspecialchars($message['message_text'], ENT_QUOTES, 'UTF-8') ?>
                </span>
            </div>
        <?php endforeach; ?>
    <?php else: ?>
        No messages yet.
    <?php endif; ?>
    
    <h2>Online Visitors: </h2>
    <ul>
        <?php foreach ($visitors as $visitor): ?>
        <li>
            <?= htmlspecialchars($visitor, ENT_QUOTES, 'UTF-8') ?>:
        </li>
        <?php endforeach; ?>
    </ul>
</body>
</html>