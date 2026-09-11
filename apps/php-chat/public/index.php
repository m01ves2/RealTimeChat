<?php

$pdo = require __DIR__ . '/../src/database.php';
$statement = $pdo->query('  SELECT id, title
                            FROM rooms
                            ORDER BY id;');

$rooms = $statement->fetchAll(PDO::FETCH_ASSOC);

?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Chat Lobby</title>
</head>
<body>
    <h1>Chat Lobby</h1>
    <form method="post" action="join.php">
        <div>
            <label for="nickname">Nickname: </label>
            <input type="text" name="nickname" id="nickname" maxlength="30" required />
        </div>

        <div>
            <label for="room_id">Room: </label>
            <select id="room_id" name="room_id" required>
                <option value="" disabled selected hidden>Choose a room</option>
                <?php foreach ($rooms as $room): ?>
                    <option value="<?= (int)$room['id'] ?>"><?= htmlspecialchars($room['title'], ENT_QUOTES, 'UTF-8') ?></option>
                <?php endforeach; ?>
            </select>
        </div>
        <input type="submit" value="Join Room">
    </form>

</body>
</html>