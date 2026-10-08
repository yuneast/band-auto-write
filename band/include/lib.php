<?php
function AlertBox($StrMsg,$StrAction,$StrRefresh='') {
	print "<script language='javascript'>\n";
	print "alert ('$StrMsg');\n";
	if ($StrAction == 'close') {
		print "self.close();\n";
	}
	elseif ($StrAction == 'reload') {
		print "location.reload();\n";
	}
	elseif ($StrAction == 'back') {
		print "history.back();\n";
	}
	print "</script>";
	if ($StrRefresh) {
		print "<meta http-equiv='Refresh' content='0;URL=$StrRefresh'>";
	}
}

function PageRedirect($StrPage,$IntSec) {
	print ("<meta http-equiv=\"Refresh\" content=\"$IntSec;URL=$StrPage\">");
}

function Logout() {
	SetCookie('AuthId','',0,'/');
	SetCookie('AuthSession','',0,'/');
}
?>