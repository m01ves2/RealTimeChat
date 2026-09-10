<?php
$pdo = require __DIR__ . '/../src/database.php';
$statement = $pdo->query('  SELECT author, message_text, created_at
                            FROM messages
                            WHERE room_id = 1
                            ORDER BY created_at;');

$messages = $statement->fetchAll(PDO::FETCH_ASSOC);
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
                    <!-- применим экранирования для защиты от Stored XSS — сохранённого вредоносного JavaScript, например, <script>alert('Hacked')</script> -->
                    <?= htmlspecialchars($message['created_at'], ENT_QUOTES, 'UTF-8') ?>:
                </span>
                <span>
                    <?= htmlspecialchars($message['author'], ENT_QUOTES, 'UTF-8') ?> @      
                </span>
                <span>
                    <?= htmlspecialchars($message['message_text'], ENT_QUOTES, 'UTF-8') ?>
                </span>
            </div>
        <?php endforeach; ?>
    <?php else: ?>
        No messages yet.
    <?php endif; ?>
</body>
</html>