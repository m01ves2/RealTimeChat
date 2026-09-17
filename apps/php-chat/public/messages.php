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


// The first request does not contain "after".
// Subsequent long polling requests send the id of the last message
// already known to the browser.

// takes param from query string and tries to make int || null
// equals: $afterMessageId = $_GET['after'] ?? null; + make integer validation:
$afterMessageId = filter_input(INPUT_GET, 'after', FILTER_VALIDATE_INT); 

if ($afterMessageId === false || ($afterMessageId !== null && $afterMessageId < 0)) {
    header($_SERVER['SERVER_PROTOCOL'] . ' 400 Bad Request');
    header('Content-Type: text/plain; charset=UTF-8');
    echo 'Invalid message id.';
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

// The first request has no "after" parameter and returns immediately.
//
// Subsequent requests are long polling requests:
// wait until a newer message appears or until the timeout expires.
if ($afterMessageId !== null) {
    $pollTimeoutSeconds = 20;
    $pollIntervalMicroseconds = 500000; // 0.5 second
    $startedAt = microtime(true);

    $newMessageStatement = $pdo->prepare('  SELECT id
                                            FROM messages
                                            WHERE room_id = :room_id
                                            AND id > :after_message_id
                                            ORDER BY id
                                            LIMIT 1; ');


    while (true) {
        $newMessageStatement->execute([
            'room_id' => $roomId,
            'after_message_id' => $afterMessageId,
        ]);

        $newMessageId = $newMessageStatement->fetchColumn();

        if ($newMessageId !== false) {
            break;
        }

        if (microtime(true) - $startedAt >= $pollTimeoutSeconds) {
            break;
        }

        usleep($pollIntervalMicroseconds);
    }
}


// Get all visitors of the room.
$visitorsStatement = $pdo->prepare("    SELECT nickname
                                        FROM room_visitors
                                        WHERE room_id = :room_id
                                        AND last_seen >= CURRENT_TIMESTAMP - INTERVAL '30 seconds'
                                        ORDER BY LOWER(nickname), nickname;");
$visitorsStatement->execute(['room_id' => $roomId]);
$visitors = $visitorsStatement->fetchAll(PDO::FETCH_COLUMN);

// Get the latest 100 messages.
$statement = $pdo->prepare('    SELECT id, author, recipient, message_text, created_at
                                FROM messages
                                WHERE room_id = :room_id
                                ORDER BY created_at DESC, id DESC
                                LIMIT 100;');
$statement->execute(['room_id' => $roomId]);

$messages = $statement->fetchAll(PDO::FETCH_ASSOC);
// The query selects the newest 100 rows efficiently; reverse them for chronological display.
$messages = array_reverse($messages);

// Tell JavaScript which message is currently the newest one.
$lastMessageId = 0;

if (count($messages) > 0) {
    $lastMessage = $messages[count($messages) - 1];
    $lastMessageId = (int)$lastMessage['id'];
}


header('Content-Type: text/html; charset=UTF-8');
?>

<div  class="messages-layout"
    data-last-message-id="<?= $lastMessageId ?>">
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