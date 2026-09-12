<?php

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    header('Allow: POST');
    exit;
}


session_start();
$visitorId = $_SESSION['visitor_id'] ?? null;
$roomId = $_SESSION['room_id'] ?? null;

if ($visitorId !== null && $roomId !== null) {

    $pdo = require __DIR__ . '/../src/database.php';

    $statement = $pdo->prepare('    DELETE FROM room_visitors
                                    WHERE id = :visitor_id
                                    AND room_id = :room_id;');
    $statement->execute(['visitor_id' => $visitorId, 'room_id' => $roomId,]);

}

session_unset();
session_destroy();


// Destroy the server-side session and redirect the visitor to the lobby.
header('Location: /', true, 303);

exit; // header() does not stop script execution, so terminate it explicitly.