<?php
$sqlite_path = __DIR__ . '/../data/newsoft.sqlite';

//1. DB 연결
try {
	$connect = new PDO('sqlite:' . $sqlite_path);
	$connect->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
	$connect->exec('PRAGMA foreign_keys = ON');
} catch (PDOException $e) {
	echo '[연결실패] : ' . $e->getMessage();
	exit;
}
?>
