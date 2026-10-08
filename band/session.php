<?php
include "include/dbcon.php";
include "include/lib.php";

$AuthId = addslashes($_COOKIE['AuthId']);
$AuthSession = addslashes($_COOKIE['AuthSession']);

$session_type = ($_REQUEST["type"] == 'band') ? "band_session" : "fb_session";


$stmt = $connect->prepare("SELECT $session_type, date FROM user WHERE id=?");
$stmt->execute([$AuthId]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);

$timenow = date("Y-m-d");
$timetarget = $row['date'];
$str_now = strtotime($timenow);
$str_target = strtotime($timetarget);

if ($AuthId && $AuthSession) {
	if ($row[$session_type] != $AuthSession) {
		Logout();
	}
}
if ($str_now > $str_target) {
	Logout();
}

$stmt = $connect->prepare("SELECT $session_type FROM user WHERE id=?");
$stmt->execute([$AuthId]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);
echo $row[$session_type];
$connect = null;
?>