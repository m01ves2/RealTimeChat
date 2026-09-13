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
                                LIMIT 100;');
$statement->execute(['room_id' => $roomId]);

$messages = $statement->fetchAll(PDO::FETCH_ASSOC);
// The query selects the newest 10 rows efficiently; reverse them for chronological display.
$messages = array_reverse($messages);

header('Content-Type: text/html; charset=UTF-8');
?>

<div class="messages-layout">
    <div class="messages-panel">
        <div>Refreshed at: <?= date('H:i:s') ?></div>
        <h2>Messages: </h2>

        <?php if (count($messages) > 0): ?>
            <?php foreach ($messages as $message): ?>
                <?php
                $authorUrl = '/chat.php?' . http_build_query(['recipient' => $message['author'],]);

                $messageClass = '';

                if ($message['recipient'] !== null && strcasecmp($message['recipient'], $nickname) === 0) {
                    $messageClass = 'message-to-me';
                } elseif (strcasecmp($message['author'], $nickname) === 0) {
                    $messageClass = 'message-own';
                }

                ?>

                <div class="message <?= $messageClass ?>">
                    <span style="color: grey">
                        <!-- // Escape persisted values to prevent stored XSS - stored Javascript malcode, for example: <script>alert('Hacked')</script> -->
                        <?= htmlspecialchars($message['created_at'], ENT_QUOTES, 'UTF-8') ?>:
                    </span>
                    <span>
                        <a href="<?= htmlspecialchars($authorUrl, ENT_QUOTES, 'UTF-8') ?>"
                            data-recipient="<?= htmlspecialchars($message['author'], ENT_QUOTES, 'UTF-8') ?>">
                            <?= htmlspecialchars($message['author'], ENT_QUOTES, 'UTF-8') ?>
                        </a>

                        <?php if ($message['recipient'] !== null): ?>
                            @ <?= htmlspecialchars($message['recipient'], ENT_QUOTES, 'UTF-8') ?>:
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
    </div>

    <div class="visitors-panel">
        <h2>Online Visitors: </h2>
        <ul>
            <?php foreach ($visitors as $visitor): ?>
                <li>
                    <?php $recipientUrl = '/chat.php?' . http_build_query(['recipient' => $visitor]); ?>

                    <a href="<?= htmlspecialchars($recipientUrl, ENT_QUOTES, 'UTF-8') ?>"
                        data-recipient="<?= htmlspecialchars($visitor, ENT_QUOTES, 'UTF-8') ?>">
                        <?= htmlspecialchars($visitor, ENT_QUOTES, 'UTF-8') ?>
                    </a>
                </li>
            <?php endforeach; ?>
        </ul>
    </div>
</div>