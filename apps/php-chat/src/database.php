<?php

$config = require __DIR__ . '/../config/database.local.php';

$dsn = $config['dsn'];
$username = $config['username'];
$password = $config['password'];

$pdo = new PDO($dsn, $username, $password);
return $pdo;