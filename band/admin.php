<?php
include "include/dbcon.php";
include "include/lib.php";

$AuthId = addslashes($_COOKIE['AuthId']);
$AuthSession = addslashes($_COOKIE['AuthSession']);

$stmt = $connect->prepare("SELECT is_admin, band_session, date FROM user WHERE id=?");
$stmt->execute([$AuthId]);
$me = $stmt->fetch(PDO::FETCH_ASSOC);

$timenow = strtotime(date("Y-m-d"));
$isValidSession = $me
	&& $AuthId && $AuthSession
	&& $me['band_session'] === $AuthSession
	&& $timenow <= strtotime($me['date']);

if (!$isValidSession) {
	PageRedirect('./login.php', 0);
	exit;
}
if (!$me['is_admin']) {
	AlertBox('권한 없음', '', './login.php');
	exit;
}

if ($_SERVER['REQUEST_METHOD'] === 'POST' && $_POST['action'] === 'extend') {
	$targetId = $_POST['id'];
	$newDate = $_POST['new_date'];
	if (preg_match('/^\d{4}-\d{2}-\d{2}$/', $newDate)) {
		$stmt = $connect->prepare("UPDATE user SET date=?, updated_at=datetime('now') WHERE id=?");
		$stmt->execute([$newDate, $targetId]);
	}
	header('Location: ./admin.php');
	exit;
}

$users = $connect->query("SELECT id, date, is_admin FROM user ORDER BY id")->fetchAll(PDO::FETCH_ASSOC);
?>
<!DOCTYPE html>
<meta charset="utf-8" />
<title>유효기간 관리</title>
<style>
	table { border-collapse: collapse; }
	td, th { border: 1px solid #ccc; padding: 6px 10px; }
</style>
<h3>사용자 유효기간 관리</h3>
<table>
<tr><th>아이디</th><th>구분</th><th>만료일</th><th>연장</th></tr>
<?php foreach ($users as $u): ?>
<tr>
	<td><?= htmlspecialchars($u['id']) ?></td>
	<td><?= $u['is_admin'] ? '관리자' : '일반' ?></td>
	<td><?= htmlspecialchars($u['date']) ?></td>
	<td>
		<form method="post" action="admin.php" style="display:inline">
			<input type="hidden" name="action" value="extend" />
			<input type="hidden" name="id" value="<?= htmlspecialchars($u['id']) ?>" />
			<input type="date" name="new_date" value="<?= htmlspecialchars($u['date']) ?>" />
			<button type="submit">저장</button>
		</form>
		<?php foreach ([30, 90, 365] as $days): ?>
			<form method="post" action="admin.php" style="display:inline">
				<input type="hidden" name="action" value="extend" />
				<input type="hidden" name="id" value="<?= htmlspecialchars($u['id']) ?>" />
				<input type="hidden" name="new_date" value="<?= date('Y-m-d', strtotime($u['date'] . " +$days days")) ?>" />
				<button type="submit">+<?= $days ?>일</button>
			</form>
		<?php endforeach; ?>
	</td>
</tr>
<?php endforeach; ?>
</table>
<?php $connect = null; ?>
